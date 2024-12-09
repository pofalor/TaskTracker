using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using System.Text;
using TaskTracker.Core.src.ConfigSectionModels;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Context;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Installers;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Init main");

const string AllowAllCorsPolicy = "AllowAll";
const string AllowOnlyFrontCors = "AllowFront";

try
{
    var builder = WebApplication.CreateBuilder(args);

    var currentDir = Directory.GetCurrentDirectory();
    var configPath = Path.Combine(currentDir, "config");
    var nlog_path = Path.Combine(configPath, "NLog.config");
    NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlog_path);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString(AppConfigurationConstants.DbConnectionName));
    });
    // Add services to the container.
    builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationIdentityDbContext>();
    builder.Services.AddScoped<AuthenticationService>();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString(AppConfigurationConstants.DbConnectionName));
    });

    var identityConfiguration = builder.Configuration.GetSection(IdentityConfiguration.IdentitySectionInConfig)
    .Get<IdentityConfiguration>();

    if (identityConfiguration is null)
    {
        var mes = "Identity configuration not correct.";
        logger.Error(mes);
        throw new InvalidOperationException(mes);
    }

    builder.Services.AddAuthorization();
    builder.Services
        // схема аутентификации - с помощью jwt-токенов
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        // подключение аутентификации с помощью jwt-токенов
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                //кто выдаёт токен
                ValidIssuer = identityConfiguration.TokenIssuer,
                ValidateAudience = true,
                //кому выдают токен
                ValidAudience = identityConfiguration.TokenAudience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identityConfiguration.TokenSecret)),
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
            };
            options.UseSecurityTokenValidators = true;
        });

    builder.Services
        .AddAutoMapper(typeof(AutoMappingProfile))
        .AddCore();

    builder.Host.UseNLog();

    builder.Services
        .AddCors(options =>
        {
            options.AddPolicy(AllowAllCorsPolicy, builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
            options.AddPolicy(AllowOnlyFrontCors, builder =>
            {
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithOrigins(
                    identityConfiguration.TokenAudience
                    )
                    .AllowCredentials();
            });
        });


    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    var corsPolicy = app.Environment.IsDevelopment() ? AllowAllCorsPolicy : AllowOnlyFrontCors;
    app.UseCors(corsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseStatusCodePages();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    await app.RunAsync();
}
catch(Exception ex)
{
    // NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}