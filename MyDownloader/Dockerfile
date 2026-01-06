FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY MyDownloader/MyDownloader.csproj ./
RUN dotnet restore

COPY MyDownloader/ .

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        python3 \
        python3-minimal \
        ffmpeg \
        curl \
    && rm -rf /var/lib/apt/lists/*

RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp && \
    chmod a+rx /usr/local/bin/yt-dlp

ENTRYPOINT ["dotnet", "MyDownloader.dll"]