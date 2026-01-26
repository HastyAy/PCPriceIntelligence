# PC Price Intelligence

A web-based PC configuration and compatibility platform built with Blazor Server.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (or use Docker)
- [Google Gemini API Key](https://makersuite.google.com/app/apikey)

## Quick Start

### Option 1: Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/pc-price-intelligence.git
   cd pc-price-intelligence
   ```

2. **Configure the application**
   
   Edit `appsettings.json` or set environment variables:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=pcbuilder;Username=postgres;Password=yourpassword"
     },
     "Gemini": {
       "ApiKey": "YOUR_GEMINI_API_KEY"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run --project web
   ```

5. **Access the app**
   - URL: `https://localhost:5001`
   - Admin: `admin@pcbuilder.com` / `Admin123!`

---

### Option 2: Docker (Recommended)

1. **Clone and navigate**
   ```bash
   git clone https://github.com/yourusername/pc-price-intelligence.git
   cd pc-price-intelligence
   ```

2. **Set your Gemini API key**
   ```bash
   # Linux/Mac
   export GEMINI_API_KEY=your_api_key_here
   
   # Windows PowerShell
   $env:GEMINI_API_KEY="your_api_key_here"
   ```

3. **Run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

4. **Access the app**
   - URL: `http://localhost:8080`
   - Admin: `admin@pcbuilder.com` / `Admin123!`

---

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `Gemini__ApiKey` | Google Gemini API key | - |
| `ADMIN_EMAIL` | Admin user email | `admin@pcbuilder.com` |
| `ADMIN_PASSWORD` | Admin user password | `Admin123!` |

## Features

- **Component Browser**: Browse CPUs, GPUs, RAM, and other PC components
- **Build Configurator**: Create custom PC builds with real-time compatibility checking
- **AI Analysis**: Get AI-powered recommendations and bottleneck analysis
- **Admin Panel**: Manual scraping controls and data management

## Project Structure

```
├── Domain/          # Entities, Enums
├── web/             # Blazor Server application
│   ├── Components/  # Razor components
│   ├── Services/    # Business logic
│   └── Data/        # DbContext, Migrations
├── Dockerfile
└── docker-compose.yml
```

## License

MIT