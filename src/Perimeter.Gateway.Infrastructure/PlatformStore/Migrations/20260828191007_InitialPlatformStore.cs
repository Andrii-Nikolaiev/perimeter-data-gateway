using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Perimeter.Gateway.Infrastructure.PlatformStore.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatformStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pdg");

            migrationBuilder.CreateTable(
                name: "actor",
                schema: "pdg",
                columns: table => new
                {
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    actor_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actor", x => x.actor_id);
                });

            migrationBuilder.CreateTable(
                name: "audit_record",
                schema: "pdg",
                columns: table => new
                {
                    audit_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    subject_id = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    capability = table.Column<string>(type: "text", nullable: false),
                    resource_name = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    reason_category = table.Column<string>(type: "text", nullable: false),
                    normalized_parameters = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    effective_row_scope = table.Column<string>(type: "jsonb", nullable: true),
                    rows_returned = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_record", x => x.audit_id);
                    table.CheckConstraint("ck_audit_record_decision", "decision IN ('ALLOW', 'DENY')");
                    table.CheckConstraint("ck_audit_record_deny_rows_returned", "decision = 'ALLOW' OR rows_returned = 0");
                    table.CheckConstraint("ck_audit_record_rows_returned", "rows_returned >= 0");
                });

            migrationBuilder.CreateTable(
                name: "resource",
                schema: "pdg",
                columns: table => new
                {
                    resource_name = table.Column<string>(type: "text", nullable: false),
                    required_capability = table.Column<string>(type: "text", nullable: false),
                    max_rows = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource", x => x.resource_name);
                    table.CheckConstraint("ck_resource_max_rows", "max_rows > 0");
                });

            migrationBuilder.CreateTable(
                name: "subject",
                schema: "pdg",
                columns: table => new
                {
                    subject_id = table.Column<string>(type: "text", nullable: false),
                    role_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject", x => x.subject_id);
                });

            migrationBuilder.CreateTable(
                name: "actor_capability",
                schema: "pdg",
                columns: table => new
                {
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    capability = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actor_capability", x => new { x.actor_id, x.capability });
                    table.ForeignKey(
                        name: "FK_actor_capability_actor_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "pdg",
                        principalTable: "actor",
                        principalColumn: "actor_id");
                });

            migrationBuilder.CreateTable(
                name: "resource_output_field",
                schema: "pdg",
                columns: table => new
                {
                    resource_name = table.Column<string>(type: "text", nullable: false),
                    field_name = table.Column<string>(type: "text", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_output_field", x => new { x.resource_name, x.field_name });
                    table.UniqueConstraint("AK_resource_output_field_resource_name_ordinal", x => new { x.resource_name, x.ordinal });
                    table.CheckConstraint("ck_resource_output_field_ordinal", "ordinal > 0");
                    table.ForeignKey(
                        name: "FK_resource_output_field_resource_resource_name",
                        column: x => x.resource_name,
                        principalSchema: "pdg",
                        principalTable: "resource",
                        principalColumn: "resource_name");
                });

            migrationBuilder.CreateTable(
                name: "resource_parameter",
                schema: "pdg",
                columns: table => new
                {
                    resource_name = table.Column<string>(type: "text", nullable: false),
                    param_name = table.Column<string>(type: "text", nullable: false),
                    param_type = table.Column<string>(type: "text", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_parameter", x => new { x.resource_name, x.param_name });
                    table.ForeignKey(
                        name: "FK_resource_parameter_resource_resource_name",
                        column: x => x.resource_name,
                        principalSchema: "pdg",
                        principalTable: "resource",
                        principalColumn: "resource_name");
                });

            migrationBuilder.CreateTable(
                name: "delegation",
                schema: "pdg",
                columns: table => new
                {
                    subject_id = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delegation", x => new { x.subject_id, x.actor_id });
                    table.ForeignKey(
                        name: "FK_delegation_actor_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "pdg",
                        principalTable: "actor",
                        principalColumn: "actor_id");
                    table.ForeignKey(
                        name: "FK_delegation_subject_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "pdg",
                        principalTable: "subject",
                        principalColumn: "subject_id");
                });

            migrationBuilder.CreateTable(
                name: "subject_resource_permission",
                schema: "pdg",
                columns: table => new
                {
                    subject_id = table.Column<string>(type: "text", nullable: false),
                    resource_name = table.Column<string>(type: "text", nullable: false),
                    allowed = table.Column<bool>(type: "boolean", nullable: false),
                    row_scope_mode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject_resource_permission", x => new { x.subject_id, x.resource_name });
                    table.CheckConstraint("ck_subject_resource_permission_row_scope_mode", "row_scope_mode IN ('ALL', 'ALLOW_LIST')");
                    table.ForeignKey(
                        name: "FK_subject_resource_permission_resource_resource_name",
                        column: x => x.resource_name,
                        principalSchema: "pdg",
                        principalTable: "resource",
                        principalColumn: "resource_name");
                    table.ForeignKey(
                        name: "FK_subject_resource_permission_subject_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "pdg",
                        principalTable: "subject",
                        principalColumn: "subject_id");
                });

            migrationBuilder.CreateTable(
                name: "subject_row_scope",
                schema: "pdg",
                columns: table => new
                {
                    subject_id = table.Column<string>(type: "text", nullable: false),
                    resource_name = table.Column<string>(type: "text", nullable: false),
                    dimension = table.Column<string>(type: "text", nullable: false),
                    allowed_value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject_row_scope", x => new { x.subject_id, x.resource_name, x.dimension, x.allowed_value });
                    table.ForeignKey(
                        name: "FK_subject_row_scope_subject_resource_permission_subject_id_re~",
                        columns: x => new { x.subject_id, x.resource_name },
                        principalSchema: "pdg",
                        principalTable: "subject_resource_permission",
                        principalColumns: new[] { "subject_id", "resource_name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_record_occurred_at",
                schema: "pdg",
                table: "audit_record",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_record_subject_actor",
                schema: "pdg",
                table: "audit_record",
                columns: new[] { "subject_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "IX_delegation_actor_id",
                schema: "pdg",
                table: "delegation",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_resource_permission_resource_name",
                schema: "pdg",
                table: "subject_resource_permission",
                column: "resource_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actor_capability",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "audit_record",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "delegation",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "resource_output_field",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "resource_parameter",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "subject_row_scope",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "actor",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "subject_resource_permission",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "resource",
                schema: "pdg");

            migrationBuilder.DropTable(
                name: "subject",
                schema: "pdg");
        }
    }
}
