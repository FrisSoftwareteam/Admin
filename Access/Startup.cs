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

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        IsDevelopment = env.IsDevelopment();
    }

    public IConfiguration Configuration { get; }
    private bool IsDevelopment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        Console.WriteLine("Startup: registering DbContext");
        services.AddDbContext<AppDB>(options =>
        {
            options.UseLazyLoadingProxies().UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
        });

        Console.WriteLine("Startup: registering MongoClient");
        services.AddSingleton<IMongoClient, MongoClient>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            var url = configuration["mongouri"] ?? configuration.GetConnectionString("mongouri");
            if (string.IsNullOrWhiteSpace(url))
                return new MongoClient();
            // Production json previously used "&wmajority" (invalid); Mongo requires "&w=majority".
            url = url.Replace("&wmajority", "&w=majority", StringComparison.OrdinalIgnoreCase)
                     .Replace("?wmajority", "?w=majority", StringComparison.OrdinalIgnoreCase);
            return new MongoClient(url);
        });

        Console.WriteLine("Startup: registering API helpers");
        services.AddSingleton(_ => new EStockApiUrl(Configuration.GetValue<string>(Common.APISettingName)));
        services.AddHttpClient<IApiClient, ApiClient>(c =>
        {
            c.BaseAddress = new Uri(Configuration.GetValue<string>(Common.APISettingName));
            c.Timeout = TimeSpan.FromSeconds(8);
        });

        Console.WriteLine("Startup: registering app services");
        services.AddScoped<Service>();
        services.AddSingleton<Mongo>();

        //services.AddSingleton<IHashids>();

        services.AddIdentity<User, Role>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;

            // Password settings.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
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

        var cookieSecurePolicy = IsDevelopment
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60 * 24);

            options.LoginPath = "/login";
            options.AccessDeniedPath = "/denied";
            options.SlidingExpiration = true;
        });

        services.AddAntiforgery(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
        });

        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.ValueLengthLimit = 20_000_000;
            options.MultipartBodyLengthLimit = 20_000_000;
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
        Console.WriteLine("Startup: ConfigureServices complete");
    }

    private static void SetQuestPdfLicense()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (!OperatingSystem.IsMacOS())
        {
            SetQuestPdfLicense();
        }

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

        app.Use((context, next) =>
        {
            var proto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (!string.IsNullOrEmpty(proto))
                context.Request.Scheme = proto;
            return next(context);
        });

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
