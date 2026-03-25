using Clear;
using FirstReg.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using QuestPDF.Infrastructure;
using System;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDB>(options => options.UseLazyLoadingProxies().UseSqlServer(
            Configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IMongoClient, MongoClient>(s =>
        {
            var url = s.GetRequiredService<IConfiguration>()["mongouri"];
            return string.IsNullOrEmpty(url) ? new() : new(url);
        });

        services.AddSingleton(new EStockApiUrl(Configuration.GetValue<string>(Common.APISettingName)));
        services.AddHttpClient<IApiClient, ApiClient>(c =>
        {
            c.BaseAddress = new Uri(Configuration.GetValue<string>(Common.APISettingName));
            c.Timeout = TimeSpan.FromMinutes(3);
        });

        services.AddScoped<Service>();
        services.AddSingleton<Mongo>();

        //services.AddSingleton<IHashids>();

        services.AddIdentity<User, Role>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;

            // Password settings.
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 0;

            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings.
            options.User.AllowedUserNameCharacters =
             "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";
            options.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<AppDB>().AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60 * 24);

            options.LoginPath = "/login";
            options.AccessDeniedPath = "/denied";
            options.SlidingExpiration = true;
        });

        services.AddAntiforgery(options =>
        {
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.UseIcgNetCoreUtilitiesSpreadsheet();
        services.AddDatabaseDeveloperPageExceptionFilter();

        //CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
                builder.SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        });

        services.AddControllersWithViews();
        services.AddRazorPages();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseForwardedHeaders();

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Remove("X-Frame-Options");
                return Task.CompletedTask;
            });
            await next();
        });

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();
        });
    }
}
