using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace WebDubRosh
{
    /// <summary>
    /// Аргументы события для обновления статуса сервера
    /// </summary>
    public class ServerStatusEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public string Message { get; set; }
        public string ExternalUrl { get; set; }
    }
    
    /// <summary>
    /// Для создания единого exe:
    /// dotnet publish -c Release -r win-x64 --self-contained true
    ///   -p:PublishSingleFile=true
    ///   -p:IncludeAllContentForSelfExtract=true
    ///   -p:EnableCompressionInSingleFile=true
    ///   -p:UseAppHost=true
    ///   -p:PublishTrimmed=false
    ///   -o publish
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Здесь меняем субдомен localtunnel (без .loca.lt).
        /// Если хотите другой - просто замените эту строку.
        /// Например, "warehousemanagementweb2" => warehousemanagementweb2.loca.lt
        /// </summary>
        private const string Subdomain = "webdubovayarosha";

        private IHost _webHost;
        private Process _ltProcess; // Процесс для localtunnel
        private List<string> _logMessages = new List<string>();
        private object _logLock = new object();
        private string _externalUrl;
        private bool _serverRunning;
        
        /// <summary>
        /// Событие изменения статуса сервера
        /// </summary>
        public event EventHandler<ServerStatusEventArgs> ServerStatusChanged;
        
        /// <summary>
        /// Возвращает последние логи и очищает коллекцию
        /// </summary>
        public List<string> GetLatestLogs()
        {
            lock (_logLock)
            {
                var logs = new List<string>(_logMessages);
                _logMessages.Clear();
                return logs;
            }
        }

        /// <summary>
        /// Добавляет сообщение в лог
        /// </summary>
        private void LogMessage(string message)
        {
            lock (_logLock)
            {
                _logMessages.Add(message);
            }
        }
        
        /// <summary>
        /// Отправляет событие об изменении статуса сервера
        /// </summary>
        private void RaiseServerStatusChanged(bool isRunning, string message, string externalUrl = null)
        {
            _serverRunning = isRunning;
            
            if (!string.IsNullOrEmpty(externalUrl))
            {
                _externalUrl = externalUrl;
            }
            
            ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs 
            { 
                IsRunning = isRunning,
                Message = message,
                ExternalUrl = externalUrl ?? _externalUrl
            });
            
            LogMessage(message);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Создаем и показываем окно
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            // 1) Запускаем локальный сервер
            await Task.Run(() => StartKestrelServer(e.Args));
            RaiseServerStatusChanged(true, "Локальный сервер запущен на http://localhost:8080");

            // 2) Проверяем/устанавливаем Node.js + localtunnel
            await CheckAndSetupNodeAndLocalTunnel();
            
            // 3) Запускаем localtunnel с повторными попытками (до 3 раз)
            var tunnelUrl = await StartLocalTunnelWithRetryAsync(3);
            if (!string.IsNullOrEmpty(tunnelUrl))
            {
                RaiseServerStatusChanged(true, $"Внешний туннель доступен: {tunnelUrl}", tunnelUrl);
                
                // Открываем браузер
                Process.Start(new ProcessStartInfo(tunnelUrl)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                RaiseServerStatusChanged(true, "Не удалось создать внешний туннель. Используем локальный URL.");
                Process.Start(new ProcessStartInfo("http://localhost:8080")
                {
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Запускает Kestrel-сервер на http://*:8080.
        /// </summary>
        private void StartKestrelServer(string[] args)
        {
            LogMessage("Запуск Kestrel сервера...");
            
            _webHost = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Слушаем на всех IP (0.0.0.0) на порту 8080
                    webBuilder.UseUrls("http://*:8080");

                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddCors(options =>
                        {
                            options.AddPolicy("MyCors", builder =>
                            {
                                builder.AllowAnyOrigin()
                                       .AllowAnyMethod()
                                       .AllowAnyHeader();
                            });
                        });
                    });

                    webBuilder.Configure(app =>
                    {
                        // Логирование входящих запросов
                        app.Use(async (context, next) =>
                        {
                            var logMessage = $"[{DateTime.Now}] {context.Connection.RemoteIpAddress} => {context.Request.Method} {context.Request.Path}";
                            LogMessage(logMessage);
                            await next.Invoke();
                        });

                        // Подключаем файлы
                        string projectDir = AppDomain.CurrentDomain.BaseDirectory;
                        // Navigate up from bin/Debug/net8.0-windows to the project directory
                        if (projectDir.Contains("bin\\Debug") || projectDir.Contains("bin\\Release"))
                        {
                            projectDir = Path.GetFullPath(Path.Combine(projectDir, "..", "..", ".."));
                        }
                        string siteFolder = Path.Combine(projectDir, "Web");
                        
                        // Verify that the directory exists
                        if (!Directory.Exists(siteFolder))
                        {
                            LogMessage($"Web directory not found at: {siteFolder}");
                            // Fall back to the app's directory if needed
                            siteFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web");
                        }
                        
                        var defaultFilesOptions = new DefaultFilesOptions
                        {
                            FileProvider = new PhysicalFileProvider(siteFolder)
                        };
                        defaultFilesOptions.DefaultFileNames.Clear();
                        defaultFilesOptions.DefaultFileNames.Add("Login.html");

                        app.UseDefaultFiles(defaultFilesOptions);
                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(siteFolder)
                        });
                        app.UseRouting();
                        app.UseCors("MyCors");
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                })
                .Build();

            _webHost.Start();
            LogMessage("Kestrel server started on http://*:8080");
        }

        /// <summary>
        /// Проверяет наличие Node.js, при необходимости устанавливает.
        /// Затем ставит localtunnel через npm install -g localtunnel.
        /// </summary>
        private async Task CheckAndSetupNodeAndLocalTunnel()
        {
            LogMessage("Проверка установки Node.js...");
            bool nodeInstalled = false;
            try
            {
                var psi = new ProcessStartInfo("node", "-v")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                        string output = await proc.StandardOutput.ReadToEndAsync();
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            nodeInstalled = true;
                            LogMessage("Node.js установлен: " + output.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Проверка установки Node.js не удалась (вероятно не установлен): " + ex.Message);
                nodeInstalled = false;
            }

            if (!nodeInstalled)
            {
                string installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Required packages", "node-v22.14.0-x64.msi");
                if (File.Exists(installerPath))
                {
                    LogMessage("Node.js не найден. Установка из: " + installerPath);
                    try
                    {
                        var installerPsi = new ProcessStartInfo("msiexec", $"/i \"{installerPath}\" /qn")
                        {
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        var installerProcess = Process.Start(installerPsi);
                        if (installerProcess != null)
                        {
                            installerProcess.WaitForExit();
                            LogMessage("Установка Node.js завершена.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Ошибка при установке Node.js: " + ex.Message);
                    }
                }
                else
                {
                    LogMessage("Установщик Node.js не найден по пути: " + installerPath);
                }
            }

            try
            {
                LogMessage("Установка localtunnel глобально через npm...");
                var npmInstallPsi = new ProcessStartInfo("npm", "install -g localtunnel")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var npmProcess = Process.Start(npmInstallPsi))
                {
                    if (npmProcess != null)
                    {
                        await npmProcess.WaitForExitAsync();
                        string npmOutput = await npmProcess.StandardOutput.ReadToEndAsync();
                        string npmError = await npmProcess.StandardError.ReadToEndAsync();
                        LogMessage(npmOutput);
                        if (!string.IsNullOrEmpty(npmError))
                        {
                            LogMessage("npm error: " + npmError);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Ошибка при установке localtunnel: " + ex.Message);
            }
        }

        /// <summary>
        /// Запускает localtunnel до maxAttempts раз, проверяя, что URL содержит Subdomain.
        /// </summary>
        private async Task<string> StartLocalTunnelWithRetryAsync(int maxAttempts)
        {
            LogMessage("Запуск localtunnel для создания внешнего туннеля...");
            
            // Ожидаемый полный адрес: Subdomain + ".loca.lt"
            string expectedUrlPart = Subdomain + ".loca.lt";
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                LogMessage($"Попытка {attempt} из {maxAttempts}...");
                string tunnelUrl = await StartLocalTunnelAsync();
                if (!string.IsNullOrEmpty(tunnelUrl) && tunnelUrl.Contains(expectedUrlPart))
                {
                    // Успех: вернули адрес, который содержит наш Subdomain
                    return tunnelUrl;
                }
                else
                {
                    LogMessage($"Попытка {attempt}: URL туннеля не соответствует '{expectedUrlPart}'.");
                    if (_ltProcess != null && !_ltProcess.HasExited)
                    {
                        try { _ltProcess.Kill(); } catch { }
                    }
                    await Task.Delay(3000); // Небольшая пауза перед повтором
                }
            }
            return null; // Если все попытки неудачны
        }

        /// <summary>
        /// Запускает localtunnel (cmd.exe /c lt --port 8080 --subdomain [Subdomain])
        /// и пытается прочитать строку "your url is: https://..."
        /// Возвращает сам URL, либо null при неудаче/таймауте (15 секунд).
        /// </summary>
        private async Task<string> StartLocalTunnelAsync()
        {
            try
            {
                LogMessage($"Запуск localtunnel с поддоменом '{Subdomain}'...");
                var ltPsi = new ProcessStartInfo("cmd.exe", $"/c lt --port 8080 --subdomain {Subdomain}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                _ltProcess = Process.Start(ltPsi);
                if (_ltProcess == null)
                {
                    LogMessage("Не удалось запустить процесс localtunnel.");
                    return null;
                }

                string tunnelUrl = null;

                // Читаем stderr в фоне
                _ = Task.Run(async () =>
                {
                    var err = await _ltProcess.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        LogMessage("localtunnel stderr: " + err);
                    }
                });

                // 15-секундный таймаут ожидания
                var timeout = TimeSpan.FromSeconds(15);
                var startTime = DateTime.UtcNow;

                while (!_ltProcess.HasExited)
                {
                    if (DateTime.UtcNow - startTime > timeout)
                    {
                        LogMessage("Тайм-аут ожидания URL от localtunnel.");
                        break;
                    }

                    var line = await _ltProcess.StandardOutput.ReadLineAsync();
                    if (line == null) break;

                    LogMessage("localtunnel: " + line);
                    if (line.Contains("your url is: "))
                    {
                        tunnelUrl = line.Replace("your url is: ", "").Trim();
                        break;
                    }
                }

                return tunnelUrl;
            }
            catch (Exception ex)
            {
                LogMessage("Ошибка запуска localtunnel: " + ex.Message);
                return null;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogMessage("Завершение работы приложения...");
            
            if (_ltProcess != null && !_ltProcess.HasExited)
            {
                try { 
                    _ltProcess.Kill();
                    LogMessage("Процесс localtunnel остановлен.");
                } 
                catch (Exception ex) { 
                    LogMessage($"Ошибка при остановке localtunnel: {ex.Message}");
                }
            }
            
            _webHost?.StopAsync().Wait();
            _webHost?.Dispose();
            LogMessage("Веб-сервер остановлен.");
            
            RaiseServerStatusChanged(false, "Сервер остановлен");
            
            base.OnExit(e);
        }
    }
}
