using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using MyDownloader.Services;
using Serilog;

namespace MyDownloader;

class Program
{
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
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        
        using var cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += (s, e) =>
        {
            logger.Information("Detected Ctrl+C. Stopping...");
            e.Cancel = true;
            cts.Cancel();
        };
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                logger.Information("Received SIGTERM. Stopping gracefully...");
                cts.Cancel();
            });
        }

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
            
            var tgService = new TgService(token, logger);
            
            await tgService.StartAsync(cts.Token);
            
            logger.Information("Bot is running. Press Ctrl+C to stop.");
            await Task.Delay(-1, cts.Token);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Bot dropped with critical error!");
        }
        finally
        {
            logger.Information("Bot stopped. Flushing logs...");
            await Log.CloseAndFlushAsync();
        }
    }
    
    private static void StartKeepAliveServer(ILogger logger)
    {
        _ = Task.Run(async () =>
        {
            using var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add("http://*:8080/");
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