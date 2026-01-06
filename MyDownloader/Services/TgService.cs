using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyDownloader.Services;

public class TgService
{
    private readonly TelegramBotClient _client;
    private readonly ILogger _logger;

    public TgService(string token, ILogger logger)
    {
        _logger = logger;
        _client = new TelegramBotClient(token);
    }

    public async Task StartAsync(CancellationToken ctsToken)
    {
        var me = await _client.GetMe();
        
        _logger.Information($"bot started: @{me.Username}");
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() 
        };

        _client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions
        );
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        var chatId = message.Chat.Id;
        var user = message.From;
        
        if (messageText.StartsWith("/start"))
        {
            _logger.Information("Message from {Username} ({UserId}): {Text}", user?.Username ?? user?.FirstName, user?.Id, messageText);
            
            await botClient.SendMessage(
                chatId,
                "🚀 **MyDownloader Bot**\n\n" +
                "Кидай посилання:\n" +
                "• TikTok\n" +
                "• Instagram Reels\n" +
                "• YouTube Shorts\n" +
                "• Pinterest\n" +
                "• Twitter/X\n\n" +
                "_Підтримую фото, відео, GIF_",
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Markdown
            );
            return;
        }
        
        string[] supportedServices = 
        {
            "instagram.com", "tiktok.com", "youtu", "pinterest.com", 
            "pin.it", "x.com", "twitter.com"
        };

        if (!supportedServices.Any(s => messageText.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.Debug("Ignored unsupported link/text from {Username}({UserId}): {Text}", user?.Username ?? user?.FirstName, user?.Id, messageText);
            
            return; 
        }
        
        var statusMsg = await botClient.SendMessage(
            chatId,
            "⏳ Завантажую контент...",
            cancellationToken: cancellationToken
        );

        string filePath = "";
        try
        {
            _logger.Information("Starting download for {Username}({UserId}): {Url}", user?.Username ?? user?.FirstName,user?.Id, messageText);
            filePath = await DownloadMediaAsync(messageText);

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                await using var stream = File.OpenRead(filePath);
                var fileInfo = new FileInfo(filePath);
                var fileSizeMb = fileInfo.Length / (1024.0 * 1024.0);
                
                _logger.Information("Downloaded file: {FileName} ({Size:F2} MB)", fileInfo.Name, fileSizeMb);
                
                if (fileInfo.Length > 49 * 1024 * 1024)
                {
                    _logger.Warning("File too big ({Size:F2} MB) for {UserId}", fileSizeMb, user?.Id);
                    
                    await botClient.EditMessageText(chatId, statusMsg.MessageId, "❌ Файл занадто великий (>50MB)", cancellationToken: cancellationToken);
                    return;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                if (extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif")
                {
                    await botClient.SendPhoto(
                        chatId,
                        InputFile.FromStream(stream),
                        caption: "📸 Готово!",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendVideo(
                        chatId,
                        InputFile.FromStream(stream),
                        caption: "🎥 Готово!",
                        supportsStreaming: true,
                        cancellationToken: cancellationToken
                    );
                }
                
                _logger.Information("Sent file to {Username}({UserId}) successfully",  user?.Username ?? user?.FirstName,user?.Id);
                await botClient.DeleteMessage(chatId, statusMsg.MessageId, cancellationToken);
            }
            else
            {
                _logger.Warning("Download returned empty path or file missing for {Url}", messageText);
                await botClient.EditMessageText(chatId, statusMsg.MessageId, "❌ Не вдалося завантажити. Перевір лінк.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing URL for {Username}({UserId}): {Url}", user?.Username ?? user?.FirstName,user?.Id, messageText);
            await botClient.EditMessageText(chatId, statusMsg.MessageId, "❌ Сталася помилка.", cancellationToken: cancellationToken);
        }
        finally
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                await Task.Delay(2000);
                try
                {
                    File.Delete(filePath);
                    Log.Debug("Cleaned up file: {FilePath}", filePath);
                }
                catch(Exception deleteEx)
                {
                    _logger.Error(deleteEx, "Failed to delete file: {FilePath}", filePath); 
                }
            }
        }
    }

    /*private async Task<string> DownloadMediaAsync(string url)
    {
        var guid = Guid.NewGuid().ToString("N")[..8];
        var outputTemplate = $"dl_{guid}.%(ext)s";
        
        var args = $"\"{url}\" -o \"{outputTemplate}\" --no-playlist --max-filesize 50M";
        
        string ytDlpPath = "yt-dlp"; 
        
        if (File.Exists("/usr/local/bin/yt-dlp")) 
        {
            ytDlpPath = "/usr/local/bin/yt-dlp";
        }
        
        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = args,
            RedirectStandardOutput = false, 
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return string.Empty;

            await process.WaitForExitAsync();
            
            var file = Directory.GetFiles(Directory.GetCurrentDirectory(), $"dl_{guid}.*").FirstOrDefault();
            return file ?? string.Empty;
        }
        catch (Exception e)
        {
            _logger.Error(e,"YT-DLP Execution Error");
            return string.Empty;
        }
    }*/
    
    private async Task<string> DownloadMediaAsync(string url)
    {
        var guid = Guid.NewGuid().ToString("N")[..8];
        var outputTemplate = $"dl_{guid}.%(ext)s";
        
        var args = $"\"{url}\" -o \"{outputTemplate}\" --no-playlist --max-filesize 50M";
        string cookiesPath = "cookies.txt"; 
        
        if (File.Exists(cookiesPath))
        {
            args += $" --cookies \"{cookiesPath}\"";
        }

        string ytDlpPath = "yt-dlp"; 
        if (File.Exists("/usr/local/bin/yt-dlp")) ytDlpPath = "/usr/local/bin/yt-dlp";

        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return string.Empty;

            await process.WaitForExitAsync();

            var file = Directory.GetFiles(Directory.GetCurrentDirectory(), $"dl_{guid}.*").FirstOrDefault();
            return file ?? string.Empty;
        }
        catch (Exception e)
        {
            _logger.Error(e,"YT-DLP Execution Error");
            return string.Empty;
        }
    }
    

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.Error(exception, "Telegram API Error");
        return Task.CompletedTask;
    }
}