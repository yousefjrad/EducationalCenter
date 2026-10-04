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
