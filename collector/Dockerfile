FROM mcr.microsoft.com/playwright/dotnet:v1.64.1-noble

USER root
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends fluxbox novnc websockify x11-utils x11vnc \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["Collector.Playwright.csproj", "./"]
RUN dotnet restore "Collector.Playwright.csproj"

COPY . .
RUN dotnet publish "Collector.Playwright.csproj" -c Release -o /app/publish --no-restore

COPY entrypoint.sh /usr/local/bin/collector-entrypoint
RUN chmod +x /usr/local/bin/collector-entrypoint \
    && mkdir -p /home/pwuser/profile \
    && chown -R pwuser:pwuser /home/pwuser/profile /app/publish

ENV PLAYWRIGHT_USER_DATA_DIR=/home/pwuser/profile
USER pwuser
WORKDIR /home/pwuser
ENTRYPOINT ["/usr/local/bin/collector-entrypoint"]
