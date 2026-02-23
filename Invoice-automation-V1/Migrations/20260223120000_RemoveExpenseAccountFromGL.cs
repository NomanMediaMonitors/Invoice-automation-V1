using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExpenseAccountFromGL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop invoice expense_account_id foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_chart_of_accounts_expense_account_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_expense_account_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "expense_account_id",
                table: "invoices");

            // Drop vendor template default_expense_account_id foreign key and index
            migrationBuilder.DropForeignKey(
                name: "FK_vendor_invoice_templates_chart_of_accounts_default_expense_~",
                table: "vendor_invoice_templates");

            migrationBuilder.DropIndex(
                name: "IX_vendor_invoice_templates_default_expense_account_id",
                table: "vendor_invoice_templates");

            migrationBuilder.DropColumn(
                name: "default_expense_account_id",
                table: "vendor_invoice_templates");

            // Drop has_expense_account from vendor templates
            migrationBuilder.DropColumn(
                name: "has_expense_account",
                table: "vendor_invoice_templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add expense_account_id to invoices
            migrationBuilder.AddColumn<Guid>(
                name: "expense_account_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_expense_account_id",
                table: "invoices",
                column: "expense_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_chart_of_accounts_expense_account_id",
                table: "invoices",
                column: "expense_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Re-add has_expense_account to vendor templates
            migrationBuilder.AddColumn<bool>(
                name: "has_expense_account",
                table: "vendor_invoice_templates",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: true);

            // Re-add default_expense_account_id to vendor templates
            migrationBuilder.AddColumn<Guid>(
                name: "default_expense_account_id",
                table: "vendor_invoice_templates",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_default_expense_account_id",
                table: "vendor_invoice_templates",
                column: "default_expense_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_invoice_templates_chart_of_accounts_default_expense_~",
                table: "vendor_invoice_templates",
                column: "default_expense_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
