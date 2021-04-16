using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace HSE.Contest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateHostBuilder(args).Build().Run();
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<User>>();
                    var rolesManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    RoleInitializer.InitializeAsync(userManager, rolesManager).Wait();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    //.ConfigureKestrel(serverOptions =>
                    //{
                    //    serverOptions.Limits.MaxConcurrentConnections = 100;
                    //    serverOptions.Limits.MaxConcurrentUpgradedConnections = 100;
                    //    serverOptions.Limits.MaxRequestBodySize = 100 * 1024;
                        
                    //    serverOptions.Limits.KeepAliveTimeout =
                    //        TimeSpan.FromMinutes(60);
                    //    serverOptions.Limits.RequestHeadersTimeout =
                    //        TimeSpan.FromMinutes(30);
                    //})
                    .UseStartup<Startup>();
                });
    }
}
