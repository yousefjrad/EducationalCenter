using EducationalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalCenter.Infrastructure.Persistence.Migrations;

/// <summary>Data only: every trainer and course that already exists is linked to the default branch (Id 1).</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261013200000_LinkExistingToDefaultBranch")]
public partial class LinkExistingToDefaultBranch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
INSERT INTO [TrainerBranches] ([TrainerId], [BranchId])
SELECT t.[Id], 1 FROM [Trainers] t
WHERE EXISTS (SELECT 1 FROM [Branches] WHERE [Id] = 1)
  AND NOT EXISTS (SELECT 1 FROM [TrainerBranches] x WHERE x.[TrainerId] = t.[Id] AND x.[BranchId] = 1);

INSERT INTO [BranchCourses] ([CourseId], [BranchId], [IsActive])
SELECT c.[Id], 1, 1 FROM [Courses] c
WHERE EXISTS (SELECT 1 FROM [Branches] WHERE [Id] = 1)
  AND NOT EXISTS (SELECT 1 FROM [BranchCourses] x WHERE x.[CourseId] = c.[Id] AND x.[BranchId] = 1);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}