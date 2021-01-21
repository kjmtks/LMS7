using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ALMS.App.Models;
using Microsoft.Extensions.DependencyInjection;
using ALMS.App.Services;

namespace ALMS.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            CreateDbIfNotExists(host);

            MountDirectories(host);

            host.Run();
        }

        private static void CreateDbIfNotExists(IHost host) {
            using(var scope = host.Services.CreateScope()) {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<DatabaseContext>();
                    var config = services.GetRequiredService<IConfiguration>();
                    DbInitializer.Initialize(context);
                    SeedData.Initialize(context, config);
                } catch (Exception e) {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(e, "An error occured creating the Database.");
                }
            }
        }

        private static void MountDirectories(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<DatabaseContext>();
                    var lectures = context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Include(x => x.Sandboxes).AsNoTracking();

                    foreach(var lecture in lectures)
                    {
                        foreach (var sandbox in lecture.Sandboxes)
                        {
                            sandbox.SetUsers();
                            sandbox.MountLectureDirectory();
                        }
                    }
                }
                catch (Exception e)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(e, "An error occured mounting directories.");
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    if (env == "Production")
                    {
                        webBuilder.UseStartup<Startup>().UseUrls("https://*:8080");
                    }
                    else
                    {
                        webBuilder.UseStartup<Startup>().UseUrls("http://*:8080");
                    }
                });
    }
}
