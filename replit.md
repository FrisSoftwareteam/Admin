# FirstReg Online Access Portal

## Project Overview

A multi-component ASP.NET Core 8.0 repository for First Registrars (share registrar system). The **Access** shareholder portal is the active project running on port 5000.

## Projects

- **Access/** - Online shareholder access portal (ASP.NET Core MVC) — **actively running on port 5000**
- **Web/** - FirstReg public-facing website (ASP.NET Core MVC)
- **API/** - REST API (ASP.NET Core)
- **Core/** - EF Core data layer + business logic (shared library)
- **Common/** - Shared utilities and enums (`UserType` enum, etc.)
- **FirstReg.Mobile/** - Mobile API endpoints
- **Sync/** - Sync service

## Running the Application

### Workflow command
```bash
cd Access && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://0.0.0.0:5000
```
**Note**: Compile takes ~3 minutes. Port 5000 maps to external port 80.

## Configuration & Secrets

All sensitive credentials are stored as Replit environment variables (not in config files):

| Environment Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server (frdb) connection string |
| `ConnectionStrings__EStockConnection` | SQL Server (estock) connection string |
| `mongouri` | MongoDB Atlas connection string |

ASP.NET Core automatically reads `ConnectionStrings__DefaultConnection` as `ConnectionStrings:DefaultConnection`, overriding any blank values in appsettings.json.

- **eStock API**: `https://fr-access-api.azurewebsites.net/` — base URL for shareholder data API

## Database (frdb — SQL Server, remote)

Key tables:
| Table | Purpose |
|---|---|
| ShareHoldings | Shareholder holdings per register |
| Shareholders | Shareholder profile |
| AspNetUsers | Identity users (ASP.NET Core Identity) |
| AuditLogs | Audit trail |

## Authentication

Uses **ASP.NET Core Identity** (built-in) — no external auth provider. Login path: `/login`.

## User Types (UserType enum in Common/enumie.cs)
```
0 = Shareholder
1 = StockBroker
2 = CompanySec
3 = FRAdmin
4 = SystemAdmin
```

## Notes

- HTTPS redirect is disabled (Replit proxies via HTTP internally)
- `SameSite=None` and DataProtection key warnings are harmless in development
- EF Core decimal precision warnings are harmless
