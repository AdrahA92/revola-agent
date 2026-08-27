using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevolaAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DemoAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRuns",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRuns", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AuditRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRuns_TenantId_CreatedAt",
                table: "AuditRuns",
                columns: new[] { "TenantId", "CreatedAt" });
            migrationBuilder.Sql("""
                CREATE TRIGGER audit_runs_append_only
                BEFORE UPDATE OR DELETE OR TRUNCATE ON "AuditRuns"
                FOR EACH STATEMENT EXECUTE FUNCTION revola_reject_audit_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS audit_runs_append_only ON \"AuditRuns\";");
            migrationBuilder.DropTable(
                name: "AuditRuns");
        }
    }
}
