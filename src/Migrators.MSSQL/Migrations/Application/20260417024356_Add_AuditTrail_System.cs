using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boilerate.Migrators.MSSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class Add_AuditTrail_System : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Auditing");

            migrationBuilder.CreateTable(
                name: "AuditTrails",
                schema: "Auditing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_DateTime",
                schema: "Auditing",
                table: "AuditTrails",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_TableName",
                schema: "Auditing",
                table: "AuditTrails",
                column: "TableName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_Type",
                schema: "Auditing",
                table: "AuditTrails",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_UserId",
                schema: "Auditing",
                table: "AuditTrails",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrails",
                schema: "Auditing");
        }
    }
}
