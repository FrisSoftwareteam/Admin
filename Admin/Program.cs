using FirstReg.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                if (!env.IsDevelopment())
                {
                    scope.ServiceProvider.GetRequiredService<AppDB>().Database.Migrate();
                }
                else
                {
                    try
                    {
                        scope.ServiceProvider.GetRequiredService<AppDB>().Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Skipping database migration during local development startup.");
                    }
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
