using System;
using System.IO;
using FirstReg;
using FirstReg.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FirstReg.OnlineAccess
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Access host...");
            Console.WriteLine("Before host build...");

            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                // Required for Azure App Service / IIS (ANCM). Do not hardcode UseUrls —
                // that fights the dynamic port Azure assigns and causes AddressInUseException.
                .UseIIS()
                .UseIISIntegration();

            // Local `dotnet run` only: keep a stable port when Azure/IIS is not hosting us.
            // Azure sets WEBSITE_INSTANCE_ID; ASPNETCORE_URLS / --urls also win when provided.
            var isAzure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
            var hasUrls = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
                || Array.Exists(args, a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase));
            if (!isAzure && !hasUrls)
                hostBuilder = hostBuilder.UseUrls("http://localhost:5092");

            var host = hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    Console.WriteLine("Config: SetBasePath");
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    Console.WriteLine("Config: appsettings.json");
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    Console.WriteLine("Config: env appsettings");
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    Console.WriteLine("Config: env vars");
                    config.AddEnvironmentVariables();
                    Console.WriteLine("Config: done");
                })
                .ConfigureServices((context, services) =>
                {
                    var startup = new Startup(context.Configuration, context.HostingEnvironment);
                    Console.WriteLine("Before ConfigureServices...");
                    startup.ConfigureServices(services);
                    Console.WriteLine("After ConfigureServices...");
                })
                .Configure(app =>
                {
                    var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                    var startup = new Startup(app.ApplicationServices.GetRequiredService<IConfiguration>(), env);
                    startup.Configure(app, env);
                })
                .Build();

            Console.WriteLine("After host build...");

            var skipMigration = string.Equals(
                Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (!skipMigration)
            {
                using (var scope = host.Services.CreateScope())
                {
                    try
                    {
                        Console.WriteLine("Running database migration...");
                        scope.ServiceProvider.GetRequiredService<AppDB>().Database.Migrate();
                        Console.WriteLine("Database migration completed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Database migration warning: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Skipping database migration because SKIP_DB_MIGRATION=true.");
            }

            using (var scope = host.Services.CreateScope())
            {
                try
                {
                    Console.WriteLine("Ensuring Hidden columns exist...");
                    scope.ServiceProvider.GetRequiredService<Service>().Data.EnsureSoftDeleteColumnsAsync().GetAwaiter().GetResult();
                    Console.WriteLine("Hidden columns ready.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hidden column check warning: {ex.Message}");
                }
            }

            Console.WriteLine("Starting web host...");
            host.Run();
        }
    }
}
