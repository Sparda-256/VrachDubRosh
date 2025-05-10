using System;
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
        private const string Subdomain = "webdubroshasssasssa";

        private IHost _webHost;
        private Process _ltProcess; // Процесс для localtunnel

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1) Запускаем локальный сервер
            StartKestrelServer(e.Args);

            // 2) Проверяем/устанавливаем Node.js + localtunnel
            await CheckAndSetupNodeAndLocalTunnel();

            // 3) Запускаем localtunnel с повторными попытками (до 3 раз)
            var tunnelUrl = await StartLocalTunnelWithRetryAsync(3);
            if (!string.IsNullOrEmpty(tunnelUrl))
            {
                Console.WriteLine($"LocalTunnel is ready: {tunnelUrl}");
                // Открываем браузер
                Process.Start(new ProcessStartInfo(tunnelUrl)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                Console.WriteLine("LocalTunnel URL not detected or not matching expected subdomain. Opening localhost:8080 as fallback.");
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
                            Console.WriteLine($"[{DateTime.Now}] {context.Connection.RemoteIpAddress} => {context.Request.Method} {context.Request.Path}");
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
                            Console.WriteLine($"Web directory not found at: {siteFolder}");
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
            Console.WriteLine("Kestrel server started on http://*:8080");
        }

        /// <summary>
        /// Проверяет наличие Node.js, при необходимости устанавливает.
        /// Затем ставит localtunnel через npm install -g localtunnel.
        /// </summary>
        private async Task CheckAndSetupNodeAndLocalTunnel()
        {
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
                            Console.WriteLine("Node.js is installed: " + output.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Node.js check failed (likely not installed): " + ex.Message);
                nodeInstalled = false;
            }

            if (!nodeInstalled)
            {
                string installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Required packages", "node-v22.14.0-x64.msi");
                if (File.Exists(installerPath))
                {
                    Console.WriteLine("Node.js not found. Installing from: " + installerPath);
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
                            Console.WriteLine("Node.js installation completed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error during Node.js installation: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Node.js installer not found at: " + installerPath);
                }
            }

            try
            {
                Console.WriteLine("Installing localtunnel globally via npm...");
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
                        Console.WriteLine(npmOutput);
                        if (!string.IsNullOrEmpty(npmError))
                        {
                            Console.WriteLine("npm error: " + npmError);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error installing localtunnel: " + ex.Message);
            }
        }

        /// <summary>
        /// Запускает localtunnel до maxAttempts раз, проверяя, что URL содержит Subdomain.
        /// </summary>
        private async Task<string> StartLocalTunnelWithRetryAsync(int maxAttempts)
        {
            // Ожидаемый полный адрес: Subdomain + ".loca.lt"
            string expectedUrlPart = Subdomain + ".loca.lt";
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Console.WriteLine($"Starting localtunnel attempt {attempt}...");
                string tunnelUrl = await StartLocalTunnelAsync();
                if (!string.IsNullOrEmpty(tunnelUrl) && tunnelUrl.Contains(expectedUrlPart))
                {
                    // Успех: вернули адрес, который содержит наш Subdomain
                    return tunnelUrl;
                }
                else
                {
                    Console.WriteLine($"Attempt {attempt}: Tunnel URL did not match '{expectedUrlPart}'.");
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
                Console.WriteLine($"Running localtunnel with subdomain '{Subdomain}'...");
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
                    Console.WriteLine("Failed to start localtunnel process.");
                    return null;
                }

                string tunnelUrl = null;

                // Читаем stderr в фоне
                _ = Task.Run(async () =>
                {
                    var err = await _ltProcess.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        Console.WriteLine("localtunnel stderr: " + err);
                    }
                });

                // 15-секундный таймаут ожидания
                var timeout = TimeSpan.FromSeconds(15);
                var startTime = DateTime.UtcNow;

                while (!_ltProcess.HasExited)
                {
                    if (DateTime.UtcNow - startTime > timeout)
                    {
                        Console.WriteLine("Timeout waiting for localtunnel URL.");
                        break;
                    }

                    var line = await _ltProcess.StandardOutput.ReadLineAsync();
                    if (line == null) break;

                    Console.WriteLine("localtunnel: " + line);
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
                Console.WriteLine("Error starting localtunnel: " + ex.Message);
                return null;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_ltProcess != null && !_ltProcess.HasExited)
            {
                try { _ltProcess.Kill(); } catch { }
            }
            _webHost?.StopAsync().Wait();
            _webHost?.Dispose();
            base.OnExit(e);
        }
    }
}
