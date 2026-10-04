# Educational Center Management System

Backend (ASP.NET Core Web API, .NET 9) for managing a training/educational center:
courses and sections, scheduling, students and enrollment, payments and installments,
trainer payroll, certificates, reports.

## Architecture
Clean Architecture, dependencies point inward only:

`Domain` <- `Application` <- `Infrastructure` <- `API`

## Stack
C#, .NET 9, ASP.NET Core Web API, SQL Server + EF Core (Code First),
FluentValidation, Serilog, JWT + Refresh Tokens, xUnit.

## Build
```
dotnet build EducationalCenter.slnx
```

## Status
- [x] Phase 1: Solution + Domain layer
- [x] Phase 2a: Application foundation + Courses feature (template for the rest)
- [x] Phase 2b: Application features (Rooms, Trainers, Students, Sections, ClassSessions, Enrollments, WaitingList, Attendance, Grades, Certificates, PaymentPlans, Payments, Receipts, Expenses, TrainerPayrolls, Settings, Users, Roles, Auth, AuditLogs, Reports, Imports done; Application layer complete)
- [ ] Phase 3: Infrastructure (3a DbContext + EF configurations, 3b repositories + UnitOfWork, 3c seeding + security services, 3d Excel done, PDF parked)
- [ ] Phase 4: API
- [ ] Phase 5: Tests

See `docs/ERD.md` for the data model.
