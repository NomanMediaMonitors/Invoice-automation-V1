using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class vendor_invoice_templates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_vendors_vendor_id",
                table: "invoices");

            migrationBuilder.AlterColumn<Guid>(
                name: "vendor_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "CHAR(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "vendor_invoice_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    vendor_id = table.Column<Guid>(type: "CHAR(36)", nullable: false, collation: "ascii_general_ci"),
                    template_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    has_invoice_number = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    has_invoice_date = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    has_due_date = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    has_description = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    has_line_items = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    has_tax_rate = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    has_sub_total = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    invoice_number_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    invoice_date_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    due_date_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sub_total_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tax_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    total_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    default_tax_rate = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    default_chart_of_account_id = table.Column<Guid>(type: "CHAR(36)", nullable: true, collation: "ascii_general_ci"),
                    notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_invoice_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendor_invoice_templates_chart_of_accounts_default_chart_of_~",
                        column: x => x.default_chart_of_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vendor_invoice_templates_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_default_chart_of_account_id",
                table: "vendor_invoice_templates",
                column: "default_chart_of_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_vendor_id",
                table: "vendor_invoice_templates",
                column: "vendor_id");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_vendors_vendor_id",
                table: "invoices",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_vendors_vendor_id",
                table: "invoices");

            migrationBuilder.DropTable(
                name: "vendor_invoice_templates");

            migrationBuilder.AlterColumn<Guid>(
                name: "vendor_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "CHAR(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_vendors_vendor_id",
                table: "invoices",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
