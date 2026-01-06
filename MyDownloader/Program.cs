using Microsoft.Extensions.Configuration;
using MyDownloader.Services;
using Serilog;

namespace MyDownloader;

class Program
{
    /*static async Task Main(string[] args)
    { 
        var configurator = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? configurator["TELEGRAM_BOT_TOKEN"] ?? throw new InvalidOperationException("API_TOKEN is not set");
        
        var tgService = new TgService(token);
        using var cts = new CancellationTokenSource();

        Console.WriteLine("Запускаю бота... Натисни Ctrl+C для зупинки");

        await tgService.StartAsync(cts.Token);
        
        await Task.Delay(-1, cts.Token);
    }*/
    
    static async Task Main(string[] args)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug() 
            .WriteTo.Console()   
            .WriteTo.File("logs/bot-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30) 
            .CreateLogger();
        
        var configurator = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        try
        {
            logger.Information("Bot starting...");
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? configurator["TELEGRAM_BOT_TOKEN"];
            
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.Fatal("TELEGRAM_BOT_TOKEN is not found!"); 
                return;
            }

            StartKeepAliveServer(logger);

            var tgService = new TgService(token,logger);
            using var cts = new CancellationTokenSource();
            
            await tgService.StartAsync(cts.Token);
            
            await Task.Delay(-1, cts.Token);
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Bot dropped with critical error!");
        }
        finally
        {
            await Log.CloseAndFlushAsync(); 
        }
    }

    private static void StartKeepAliveServer(ILogger logger)
    {
        _ = Task.Run(async () =>
        {
            logger.Information("HTTP Server listening port 8080");
        });
    }
}
