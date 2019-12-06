using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Noggin.NetCoreAuth.Config;
using Noggin.SampleSite.Data;
using System;
using System.IO;

namespace Noggin.SampleSite
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
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
                    options.Cookie.Name = ".Font.Wtf.Session";
                });

            // Add framework services.
            services.AddMvc();
            services.AddDbContext<SampleSimpleDbContext>();

            //services.AddAuthorization();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.AccessDeniedPath = "/";
                        options.LoginPath = "/";
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

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
            // Static files (in wwwroot/.well-known/acme-challenge) for SSL Cert Auth check
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, @".well-known", "acme-challenge")),
                RequestPath = "/.well-known/acme-challenge",
                ServeUnknownFileTypes = true,
                DefaultContentType = "text/json"
            });

            app.UseSession();

            app.UseAuthentication();


            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapNogginNetAuthRoutes(app.ApplicationServices);
            });
        }
    }
}
