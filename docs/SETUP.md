# Setup

## Requirements
- .NET SDK 9
- SQL Server (LocalDB, Express or full) reachable from your machine
- EF Core tools (once): `dotnet tool install --global dotnet-ef`

## Configuration
Nothing secret is stored in the repository. From the repository root:

```powershell
dotnet user-secrets set "Jwt:SecretKey" "<a random string of at least 32 characters>" --project src/EducationalCenter.API
dotnet user-secrets set "Seed:AdminEmail" "admin@yourcenter.com" --project src/EducationalCenter.API
dotnet user-secrets set "Seed:AdminPassword" "<a strong password>" --project src/EducationalCenter.API
```

The connection string is in `src/EducationalCenter.API/appsettings.json` (`ConnectionStrings:DefaultConnection`).
Override it for your machine with a user secret or the `ConnectionStrings__DefaultConnection` environment variable.

## Database
The design-time factory reads `EDUCATIONAL_CENTER_CONNECTION`, or falls back to `Server=.;Database=EducationalCenter;Trusted_Connection=True;TrustServerCertificate=True`.
For SQL Server Express:

```powershell
$env:EDUCATIONAL_CENTER_CONNECTION = "Server=.\SQLEXPRESS;Database=EducationalCenter;Trusted_Connection=True;TrustServerCertificate=True"
```

Create the first migration and the database:

```powershell
dotnet ef migrations add InitialCreate --project src/EducationalCenter.Infrastructure --startup-project src/EducationalCenter.API --output-dir Persistence/Migrations
dotnet ef database update --project src/EducationalCenter.Infrastructure --startup-project src/EducationalCenter.API
```

Later model changes: `dotnet ef migrations add <Name> ...` with the same two project options.

## First start (next phase)
On start the API applies pending migrations and seeds: permissions, the Admin and Receptionist roles,
default settings, default certificate templates (Arabic and English), and the first Admin user from the `Seed:*` secrets.

## PDF and Excel
- Excel files (report exports, import templates, import reading) use ClosedXML.
- PDF files (receipts, certificates, report PDFs) are parked for now: see `docs/PDF.md`.
- ClosedXML is pinned to 0.104.2. To check versions use
  `dotnet list package`.

## Background jobs
Both start with the API (next phase) and need no setup:
- Every 5 minutes, temporary enrollment holds that ran out are cancelled.
- Every day at `Backup:DailyAtLocalTime` (default 02:00 local time) a full database backup is taken and verified.

## Backups
Settings in `appsettings.json`, section `Backup`:
- `Enabled`: turn the daily backup on or off.
- `Directory`: leave empty to use SQL Server's default backup folder (always writable by SQL Server).
  If you set a folder, it must be on the machine where SQL Server runs and SQL Server's service account
  must be able to write to it. Pointing it at an external drive is the safest place for a second copy.
- `DailyAtLocalTime`: "HH:mm".
- `RetentionDays`: older backups are deleted, but the 3 newest are always kept. Deleting is best effort:
  if the application is not allowed to delete in that folder, a warning is logged and the file stays.

Backups are compressed except on SQL Server Express/LocalDB, which does not support compression.
Each backup is verified (`RESTORE VERIFYONLY`). The Admin can also take one on demand and list the recent ones
(permissions `Backups.Create` and `Backups.View`).

To restore (run in SQL Server Management Studio, with the API stopped):

```sql
USE master;
ALTER DATABASE [EducationalCenter] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [EducationalCenter] FROM DISK = N'C:\path\to\EducationalCenter_20261004_020000.bak' WITH REPLACE;
ALTER DATABASE [EducationalCenter] SET MULTI_USER;
```
