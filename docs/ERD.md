# ERD

```mermaid
erDiagram
    Course ||--o{ Section : has
    Trainer ||--o{ Section : teaches
    Room ||--o{ Section : hosts
    Section ||--o{ SectionSchedule : has
    Section ||--o{ ClassSession : generates
    Student ||--o{ Enrollment : makes
    Section ||--o{ Enrollment : receives
    Student ||--o{ WaitingListEntry : queues
    Section ||--o{ WaitingListEntry : has
    Enrollment ||--o{ Attendance : has
    ClassSession ||--o{ Attendance : records
    Enrollment ||--o| Grade : has
    Enrollment ||--o| Certificate : earns
    Enrollment ||--o| PaymentPlan : has
    PaymentPlan ||--o{ Installment : splits
    Installment ||--o{ Payment : paid_by
    Payment ||--o| Receipt : issues
    Trainer ||--o{ TrainerPayroll : paid
    Role ||--o{ User : assigned
    Role ||--o{ RolePermission : grants
    Permission ||--o{ RolePermission : in
    User ||--o{ RefreshToken : owns
    User ||--o{ AuditLog : performs
```

## Conventions
- `Id`: auto-increment `int`.
- `BaseEntity`: `CreatedAt`, `UpdatedAt`, `IsDeleted`.
- Without `BaseEntity`: `SectionSchedule`, `RefreshToken`, `Permission`, `RolePermission`, `AuditLog`, `Setting`.
- Money: `decimal`. USD payments store `ExchangeRate` and `AmountInSyp` at entry time.
- Financial data is never hard-deleted (status change only).
- `Installment` overdue state and the student's remaining balance are computed, not stored.
