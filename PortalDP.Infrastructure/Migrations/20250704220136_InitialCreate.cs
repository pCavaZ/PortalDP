using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalDP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DNI = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassCancellations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ClassDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalScheduleId = table.Column<int>(type: "integer", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassCancellations_Schedules",
                        column: x => x.OriginalScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassCancellations_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ClassDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    OriginalCancellationId = table.Column<int>(type: "integer", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryClasses_ClassCancellations",
                        column: x => x.OriginalCancellationId,
                        principalTable: "ClassCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecoveryClasses_Students",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "CreatedAt", "DNI", "Email", "IsActive", "Name", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3448), "12345678A", "maria.garcia@email.com", true, "María García López", "666123456", new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3449) },
                    { 2, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3452), "87654321B", "ana.lopez@email.com", true, "Ana López Martín", "666654321", new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3453) },
                    { 3, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3455), "11223344C", "carmen.ruiz@email.com", true, "Carmen Ruiz Sánchez", "666112233", new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3455) },
                    { 4, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3457), "55667788D", "marta.sanchez@email.com", true, "Marta Sánchez Rodríguez", "666556677", new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3458) },
                    { 5, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3459), "99887766E", "rosa.martin@email.com", true, "Rosa Martín González", "666998877", new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3460) }
                });

            migrationBuilder.InsertData(
                table: "TimeSlots",
                columns: new[] { "Id", "DayOfWeek", "EndTime", "IsActive", "MaxCapacity", "StartTime" },
                values: new object[,]
                {
                    { 1, 1, new TimeSpan(0, 12, 0, 0, 0), true, 10, new TimeSpan(0, 10, 0, 0, 0) },
                    { 2, 1, new TimeSpan(0, 14, 0, 0, 0), true, 10, new TimeSpan(0, 12, 0, 0, 0) },
                    { 3, 1, new TimeSpan(0, 18, 0, 0, 0), true, 10, new TimeSpan(0, 16, 0, 0, 0) },
                    { 4, 1, new TimeSpan(0, 20, 0, 0, 0), true, 10, new TimeSpan(0, 18, 0, 0, 0) },
                    { 5, 2, new TimeSpan(0, 12, 0, 0, 0), true, 10, new TimeSpan(0, 10, 0, 0, 0) },
                    { 6, 2, new TimeSpan(0, 14, 0, 0, 0), true, 10, new TimeSpan(0, 12, 0, 0, 0) },
                    { 7, 2, new TimeSpan(0, 18, 0, 0, 0), true, 10, new TimeSpan(0, 16, 0, 0, 0) },
                    { 8, 2, new TimeSpan(0, 20, 0, 0, 0), true, 10, new TimeSpan(0, 18, 0, 0, 0) },
                    { 9, 3, new TimeSpan(0, 12, 0, 0, 0), true, 10, new TimeSpan(0, 10, 0, 0, 0) },
                    { 10, 3, new TimeSpan(0, 14, 0, 0, 0), true, 10, new TimeSpan(0, 12, 0, 0, 0) },
                    { 11, 3, new TimeSpan(0, 18, 0, 0, 0), true, 10, new TimeSpan(0, 16, 0, 0, 0) },
                    { 12, 3, new TimeSpan(0, 20, 0, 0, 0), true, 10, new TimeSpan(0, 18, 0, 0, 0) },
                    { 13, 4, new TimeSpan(0, 12, 0, 0, 0), true, 10, new TimeSpan(0, 10, 0, 0, 0) },
                    { 14, 4, new TimeSpan(0, 14, 0, 0, 0), true, 10, new TimeSpan(0, 12, 0, 0, 0) },
                    { 15, 4, new TimeSpan(0, 18, 0, 0, 0), true, 10, new TimeSpan(0, 16, 0, 0, 0) },
                    { 16, 4, new TimeSpan(0, 20, 0, 0, 0), true, 10, new TimeSpan(0, 18, 0, 0, 0) },
                    { 17, 5, new TimeSpan(0, 12, 0, 0, 0), true, 10, new TimeSpan(0, 10, 0, 0, 0) },
                    { 18, 5, new TimeSpan(0, 14, 0, 0, 0), true, 10, new TimeSpan(0, 12, 0, 0, 0) },
                    { 19, 5, new TimeSpan(0, 18, 0, 0, 0), true, 10, new TimeSpan(0, 16, 0, 0, 0) },
                    { 20, 5, new TimeSpan(0, 20, 0, 0, 0), true, 10, new TimeSpan(0, 18, 0, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Schedules",
                columns: new[] { "Id", "CreatedAt", "DayOfWeek", "EndTime", "IsActive", "StartTime", "StudentId" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3485), 1, new TimeSpan(0, 14, 0, 0, 0), true, new TimeSpan(0, 12, 0, 0, 0), 1 },
                    { 2, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3488), 2, new TimeSpan(0, 12, 0, 0, 0), true, new TimeSpan(0, 10, 0, 0, 0), 2 },
                    { 3, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3490), 3, new TimeSpan(0, 18, 0, 0, 0), true, new TimeSpan(0, 16, 0, 0, 0), 3 },
                    { 4, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3491), 4, new TimeSpan(0, 14, 0, 0, 0), true, new TimeSpan(0, 12, 0, 0, 0), 4 },
                    { 5, new DateTime(2025, 7, 4, 22, 1, 35, 245, DateTimeKind.Utc).AddTicks(3493), 5, new TimeSpan(0, 12, 0, 0, 0), true, new TimeSpan(0, 10, 0, 0, 0), 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassCancellations_Date",
                table: "ClassCancellations",
                column: "ClassDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClassCancellations_OriginalScheduleId",
                table: "ClassCancellations",
                column: "OriginalScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassCancellations_Student_Date",
                table: "ClassCancellations",
                columns: new[] { "StudentId", "ClassDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryClasses_OriginalCancellation",
                table: "RecoveryClasses",
                column: "OriginalCancellationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryClasses_Student_Date",
                table: "RecoveryClasses",
                columns: new[] { "StudentId", "ClassDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryClasses_TimeSlot",
                table: "RecoveryClasses",
                columns: new[] { "ClassDate", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Student_Day_Active",
                table: "Schedules",
                columns: new[] { "StudentId", "DayOfWeek", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TimeSlot",
                table: "Schedules",
                columns: new[] { "DayOfWeek", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_DNI",
                table: "Students",
                column: "DNI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_IsActive",
                table: "Students",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_DayTime",
                table: "TimeSlots",
                columns: new[] { "DayOfWeek", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_IsActive",
                table: "TimeSlots",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryClasses");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "ClassCancellations");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
