using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevolaAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyKnowledge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyRecords",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DataJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    Source = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRecords", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CompanyRecords_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyRevisions",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataJson = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    Source = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRevisions", x => new { x.TenantId, x.RecordId, x.Version });
                    table.ForeignKey(
                        name: "FK_CompanyRevisions_CompanyRecords_TenantId_RecordId",
                        columns: x => new { x.TenantId, x.RecordId },
                        principalTable: "CompanyRecords",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRecords_TenantId_Kind_UpdatedAt",
                table: "CompanyRecords",
                columns: new[] { "TenantId", "Kind", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRevisions_TenantId_RecordId_CreatedAt",
                table: "CompanyRevisions",
                columns: new[] { "TenantId", "RecordId", "CreatedAt" });
            migrationBuilder.Sql("""
                CREATE TRIGGER company_revisions_append_only
                BEFORE UPDATE OR DELETE OR TRUNCATE ON "CompanyRevisions"
                FOR EACH STATEMENT EXECUTE FUNCTION revola_reject_audit_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS company_revisions_append_only ON \"CompanyRevisions\";");
            migrationBuilder.DropTable(
                name: "CompanyRevisions");

            migrationBuilder.DropTable(
                name: "CompanyRecords");
        }
    }
}
