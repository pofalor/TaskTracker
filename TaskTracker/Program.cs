using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskTracker.Core.src.ConfigSectionModels;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Context;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Installers;

var builder = WebApplication.CreateBuilder(args);

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

var identityConfiguration = builder.Configuration
    .GetValue<IdentityConfiguration>(IdentityConfiguration.IdentitySectionInConfig);

if(identityConfiguration is null)
{
    //TODO: насрать в логи
    throw new InvalidOperationException("Identity configuration not correct.");
}

builder.Services
    .AddAuthorization()
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
        };
    });

builder.Services
    .AddAutoMapper(typeof(AutoMappingProfile))
    .AddCore();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();