using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Noggin.NetCoreAuth.Config;
using Noggin.SampleSite;
using Noggin.SampleSite.Data;
using System;

var builder = WebApplication.CreateBuilder(args);

// Behind a reverse proxy (e.g. nginx) that terminates HTTPS, trust its X-Forwarded-* headers so the request
// scheme is https and callback URLs sent to login providers use https.
// The proxy's address isn't known in advance (e.g. a Docker network), so the default loopback-only lists are cleared.
// Only do this when the app can't be reached except through the proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Point Noggin Auth at your login handler and give it the app's configuration (reads the "NogginNetAuth" section)
builder.Services.AddNogginNetCoreAuth<SampleLoginHandler>(builder.Configuration);

// Noggin Auth requires session to persist details between the first and second call to login providers
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(25);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".Noggin.NetCoreAuth.Session";
});

// The sample site stores its users with EF Core and logs them in with a cookie
builder.Services.AddDbContext<SampleSimpleDbContext>();
builder.Services.AddScoped<ISimpleDbContext, SampleSimpleDbContext>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/";
        options.LoginPath = "/";
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Must run before anything that reads the request scheme, host or client IP
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Order here is important (authorization must be after authentication as it needs an authenticated user to check)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Register the Noggin Auth login and callback routes (templates can be changed in appsettings)
app.MapNogginNetAuthRoutes(app.Services);

app.Run();
