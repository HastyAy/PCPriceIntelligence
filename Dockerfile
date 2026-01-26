# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY Domain/*.csproj ./Domain/
COPY web/*.csproj ./web/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/web
RUN dotnet publish -c Release -o /app/publish

# Install Playwright browsers in build stage
WORKDIR /app/publish
RUN pwsh playwright.ps1 install chromium

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install Playwright browser dependencies
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    libatspi2.0-0 \
    libxshmfence1 \
    libglu1-mesa \
    libwayland-client0 \
    xvfb \
    fonts-liberation \
    fonts-noto-color-emoji \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Copy Playwright browsers from build stage
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "web.dll"]