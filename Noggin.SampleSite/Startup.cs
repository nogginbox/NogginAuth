﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Noggin.NetCoreAuth.Config;
using Noggin.SampleSite.Data;
using System;

namespace Noggin.SampleSite;

public class Startup
{
    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddUserSecrets<Startup>()
            .AddEnvironmentVariables();
        Configuration = builder.Build();
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddNogginNetCoreAuth<SampleLoginHandler>(Configuration);
        services.AddScoped<ISimpleDbContext, SampleSimpleDbContext>();

        services
            .AddMemoryCache()
            .AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(25);
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".Noggin.NetCoreAuth.Session";
            });

        // Add framework services.
        services.AddMvc();
        services.AddDbContext<SampleSimpleDbContext>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.AccessDeniedPath = "/";
                    options.LoginPath = "/";
                });

        services.AddAuthorization();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //if (env.IsDevelopment())
        //{
            app.UseDeveloperExceptionPage();
        //app.UseBrowserLink();
        /*}
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }*/

        // Static files (in wwwroot)
        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();

        // Order here is important (authorization must be after authentication as it needs an authenticated user to check)
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            endpoints.MapNogginNetAuthRoutes(app.ApplicationServices);
        });
    }
}
