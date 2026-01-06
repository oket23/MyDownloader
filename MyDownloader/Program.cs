using System.Net;
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
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");
        
            try 
            {
                listener.Start();
                logger.Information("HTTP Server started on port 8080");

                while (true)
                {
                    var context = await listener.GetContextAsync();
                    
                    var response = context.Response;
                    string responseString = "Bot is alive!";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "HTTP Server crashed");
            }
        });
    }
}
