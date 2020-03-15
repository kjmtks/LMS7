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
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddHostedService<QueuedHostedBackgroundJobService>();
            services.AddSingleton<IBackgroundTaskQueueSet, BackgroundTaskQueueSet>();


            services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DatabaseContext")));
            services.AddScoped<DatabaseService>();

            services.AddSingleton<NotifierService>();

            services.AddScoped<HttpClient>();
            services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
            services.AddScoped<ILocalStorageService, LocalStorageService>();
            services.AddScoped<IAuthService, DatabaseAuthService>();
            services.AddAuthorizationCore();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
