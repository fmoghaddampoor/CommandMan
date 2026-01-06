using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommandMan.Shell
{
    public class ProgressHub : Hub
    {
        public async Task SendProgress(string fileName, int percentage)
        {
            await Clients.All.SendAsync("ReceiveProgress", fileName, percentage);
        }
    }

    public static class SignalRServer
    {
        private static IHost? _host;

        public static async Task StartAsync()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:5001");
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSignalR();
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowAll", builder =>
                            {
                                builder.WithOrigins("http://localhost:4200")
                                       .AllowAnyHeader()
                                       .AllowAnyMethod()
                                       .AllowCredentials();
                            });
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseCors("AllowAll");
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<ProgressHub>("/progress");
                        });
                    });
                })
                .Build();

            await _host.StartAsync();
        }

        public static IHubContext<ProgressHub>? HubContext => _host?.Services.GetService<IHubContext<ProgressHub>>();
    }
}
