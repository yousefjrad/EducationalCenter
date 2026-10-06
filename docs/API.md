# API

## Run
```powershell
dotnet run --project src/EducationalCenter.API
```
Open http://localhost:5080/swagger (Development only). On start the API applies migrations and seeds the database,
so the database does not need to exist beforehand. The `Jwt:SecretKey`, `Seed:AdminEmail` and `Seed:AdminPassword`
secrets must be set first (see SETUP.md).

## Sign in with Swagger
1. `POST /api/v1/auth/login` with the seeded Admin email and password.
2. Copy `accessToken` from the answer.
3. Press **Authorize** (top right) and paste the token.
4. Try `GET /api/v1/auth/me`: it shows the user and the permissions of the role.

Access tokens last 15 minutes. `POST /api/v1/auth/refresh` with the `refreshToken` returns a new pair
(each refresh token works once).

## Rules every endpoint follows
- URL: `/api/v1/{controller}`, controller names in kebab-case (`class-sessions`).
- Sign-in required unless marked anonymous. Protected endpoints also need a permission, for example `Payments.Create`;
  a missing permission answers 403. Permissions are read from the database on each request, so changing a role applies at once.
- Errors use the standard problem format: 400 validation (with an `errors` map), 401 not signed in, 403 not allowed,
  404 not found, 409 conflict with the current data, 501 feature not installed (PDF for now), 500 unexpected.
- Dates are `yyyy-MM-dd`, times `HH:mm`, enums are sent and received as text (for example `"Usd"`).

## Logs
`src/EducationalCenter.API/logs/` holds one file per day for 30 days (also written to the console).
