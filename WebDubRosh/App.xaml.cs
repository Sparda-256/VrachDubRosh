using System;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebDubRosh
{
    public partial class App : Application
    {
        private IHost _webHost;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _webHost = Host.CreateDefaultBuilder(e.Args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Слушаем на http://localhost:8080
                    webBuilder.UseUrls("http://localhost:8080");

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _webHost?.StopAsync().Wait();
            _webHost?.Dispose();
            base.OnExit(e);
        }
    }
}