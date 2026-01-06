# 🚀 MyDownloader Bot

**MyDownloader** is a Telegram bot built with **.NET 10** that allows users to download videos and photos from popular social networks (TikTok, Instagram, YouTube, etc.).

The bot accepts a link from the user, downloads the media using `yt-dlp`, and sends the file back to the chat.

## ✨ Key Features

* 📥 **Media Download** from popular platforms:
    * TikTok
    * Instagram (Reels, Posts)
    * YouTube (Shorts, Video)
    * Pinterest
    * Twitter / X
* 🎞 **Format Support**: Automatic detection of media type (Photo/Video/GIF).
* 📦 **Size Limit**: Supports files up to **50 MB** (Telegram API limit for bots).
* 🧹 **Auto-Cleanup**: Downloaded files are automatically deleted from the server after sending.
* 🍪 **Cookies Support**: Ability to use `cookies.txt` for accessing content requiring authorization.
* 💓 **Keep-Alive Server**: Built-in mini-HTTP server (port 8080) to maintain container activity (Health Check).

## 🛠 Tech Stack

* **Language**: C# (.NET 10)
* **Libraries**:
    * `Telegram.Bot` — Telegram API interaction.
    * `Serilog` — Logging (Console + Files).
* **External Tools**:
    * `yt-dlp` — Media downloading.
    * `ffmpeg` — Multimedia processing.
    * `Docker` — Containerization.

## 🚀 Docker Setup (Recommended)

The project is fully configured for Docker.

### 1. Clone the repository
```bash
git clone [https://github.com/oket23/MyDownloader.git](https://github.com/oket23/MyDownloader.git)
cd MyDownloader

```

### 2. Configure Token

You can pass the token via an environment variable at runtime or set it in `appsettings.json` (not recommended for public repos).

### 3. Build and Run

**Note:** You must run the build command from the root solution folder (where the `.sln` file is located).

```bash
# Build the image
docker build -f MyDownloader/Dockerfile -t mydownloader-bot .

# Run the container
docker run -d \
  --name my-bot \
  -e TELEGRAM_BOT_TOKEN="YOUR_TOKEN_HERE" \
  -p 8080:8080 \
  mydownloader-bot

```

> **Note:** If you need cookies for downloading (e.g., for Instagram), mount the `cookies.txt` file into the container:
> ```bash
> -v $(pwd)/cookies.txt:/app/cookies.txt
> 
> ```
> 
> 

## 💻 Local Development

### Prerequisites

1. **.NET SDK 10** installed.
2. System tools installed:
* `ffmpeg`
* `yt-dlp` (must be available in PATH or `/usr/local/bin/yt-dlp`).


3. A bot token obtained from [@BotFather](https://t.me/BotFather).

### Instructions

1. Navigate to the project directory:
```bash
cd MyDownloader

```


2. Edit `appsettings.json` or set the environment variable:
```json
{
  "TELEGRAM_BOT_TOKEN": "YOUR_TOKEN_HERE"
}

```


3. Run the bot:
```bash
dotnet run

```



## 📂 Project Structure

```text
MyDownloader.sln       # Solution file
MyDownloader/          # Main project folder
├── Dockerfile         # Docker instructions
├── Program.cs         # Entry point, logger and config initialization
├── Services/
│   └── TgService.cs   # Bot logic: message handling and downloading
└── appsettings.json   # Configuration (token)

```

## 📝 Logging

Logs are recorded in two locations:

1. **Console** (useful for Docker logs).
2. **Files** `logs/bot-YYYYMMDD.log` (retained for 30 days).

---

Created by **oket23**
