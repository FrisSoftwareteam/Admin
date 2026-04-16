# FirstReg Online Access Portal

## Project Overview

A multi-component ASP.NET Core 8.0 repository for First Registrars (share registrar system). The active running project is `Access/` — a shareholder online access portal.

## Projects

- **Access/** - Online shareholder access portal (ASP.NET Core MVC) — **actively running on port 5000**
- **Admin/** - Internal admin panel (ASP.NET Core MVC)
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
**Note**: Compile takes ~3 minutes. Restart via `restartWorkflow("Start application")`.

## Configuration (Access/appsettings.json)

- **SQL Server**: `Server=104.211.8.144;Database=frdb;User Id=access;Password=hbG3F4UpsJ#r/Zm;MultipleActiveResultSets=true;TrustServerCertificate=True;`
- **MongoDB**: `mongodb+srv://fruser:xOTlhQyg4Z0U1VCh@cluster0.csbuh.mongodb.net`
- **eStock API**: `https://fr-access-api.azurewebsites.net/` — base URL reachable, but `/shareholders` endpoint times out from Replit

## Database (frdb — SQL Server)

Key tables with data:
| Table | Rows | Purpose |
|---|---|---|
| ShareHoldings | 24,157 | Shareholder holdings per register (AccountName, AccountNo, Units) |
| Shareholders | 14,750 | Shareholder profile (ClearingNo, PrimaryPhone, Street, City) |
| AspNetUsers | 14,733 | Identity users linked to shareholders |
| AuditLogs | 45,582 | Audit trail |
| RegisterHoldings | 0 | Local cache from eStock API — **empty**, populated only when shareholders log in |

## User Types (UserType enum in Common/enumie.cs)
```
0 = Shareholder
1 = StockBroker
2 = CompanySec
3 = FRAdmin      ← can log into Access portal admin area
4 = SystemAdmin  ← blocked from Access portal (use Admin app instead)
```

## FRAdmin Shareholders List — Architecture

**Problem solved**: The `/admin/shareholders` page previously called the eStock API (`/shareholders` endpoint) which times out from Replit. The `RegisterHoldings` table (local cache) is empty.

**Fix**: `FRAdminController.GetShareholderListz` now queries `ShareHoldings` table directly via EF Core:
- Uses a `.Select()` projection (not `.Include()`) for efficient single-SQL JOIN query
- Joins to `Shareholders` (for ClearingNo, Phone) and `Registers` (for Register name)
- Returns up to 5,000 rows per request in <1 second
- Filters: `regid`, `name`, `acc`, `cscs`, `addr`, `global`

## Notes

- HTTPS redirect is disabled (Replit proxies via HTTP)
- `SameSite=None` cookie warnings are harmless in development
- EF Core decimal precision warnings are harmless (no data loss for share units)
- `GetShareholderDetails` in FRAdminController still calls eStock API — may fail if API times out
