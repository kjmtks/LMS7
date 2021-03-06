using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.EntityFrameworkCore;
using ALMS.App.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using ALMS.App.Services;
using System.Net.Http;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;

namespace ALMS.App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options => {
                options.CheckConsentNeeded = context => !context.User.Identity.IsAuthenticated;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
                options.HttpOnly = HttpOnlyPolicy.Always;
            });

            var subdir = Environment.GetEnvironmentVariable("SUB_DIR") ?? "";
            services.ConfigureApplicationCookie(options => { options.Cookie.Path = subdir; });

            services
               .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options => { options.LoginPath = $"{subdir}/login"; options.LogoutPath = $"{subdir}/logout"; });
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(@"/data/DataProtectionKey"));

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddHostedService<QueuedHostedBackgroundJobService>();
            services.AddSingleton<IBackgroundTaskQueueSet, BackgroundTaskQueueSet>();


            services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DatabaseContext")));
            services.AddScoped<DatabaseService>();

            services.AddSingleton<NotifierService>();
            services.AddSingleton<OnlineStatusService>();
            
            services.AddScoped<ActivityService>();

            services.AddScoped<IFileUploadService, FileUploadService>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var subdir = Environment.GetEnvironmentVariable("SUB_DIR") ?? "";
            if (!string.IsNullOrWhiteSpace(subdir)) { app.UsePathBase(subdir); }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "../node_modules")),
                RequestPath = "/node_modules"
            });
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapFallbackToPage("/_Host");
            });

            app.UseCookiePolicy();
        }
    }
}
