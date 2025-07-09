using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DiffLens.VisualStudio.Services;

namespace DiffLens.VisualStudio
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("DiffLens Application Starting...");
            
            try
            {
                // サービスの初期化
                await GitService.InitializeAsync();
                await DiffService.InitializeAsync();
                await ReviewService.InitializeAsync();
                
                logger.LogInformation("Testing services...");
                
                // ここでアプリケーションのメイン処理を実装
                Console.WriteLine("DiffLens for .NET Core is running!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during execution");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}
