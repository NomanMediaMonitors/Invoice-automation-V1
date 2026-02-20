using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class AddGLAccountsAndPostToGL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add GL account toggle fields to vendor_invoice_templates
            migrationBuilder.AddColumn<bool>(
                name: "has_expense_account",
                table: "vendor_invoice_templates",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_advance_tax_account",
                table: "vendor_invoice_templates",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_sales_tax_input_account",
                table: "vendor_invoice_templates",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_payable_vendors_account",
                table: "vendor_invoice_templates",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: true);

            // Add default GL account ID fields to vendor_invoice_templates
            migrationBuilder.AddColumn<Guid>(
                name: "default_expense_account_id",
                table: "vendor_invoice_templates",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "default_advance_tax_account_id",
                table: "vendor_invoice_templates",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "default_sales_tax_input_account_id",
                table: "vendor_invoice_templates",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "default_payable_vendors_account_id",
                table: "vendor_invoice_templates",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            // Add GL account assignment fields to invoices
            migrationBuilder.AddColumn<Guid>(
                name: "expense_account_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "advance_tax_account_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "sales_tax_input_account_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "payable_vendors_account_id",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            // Add GL posting fields to invoices
            migrationBuilder.AddColumn<bool>(
                name: "is_posted_to_gl",
                table: "invoices",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "posted_to_gl_at",
                table: "invoices",
                type: "DATETIME",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "posted_to_gl_by",
                table: "invoices",
                type: "CHAR(36)",
                nullable: true,
                collation: "ascii_general_ci");

            // Create foreign keys for vendor_invoice_templates GL accounts
            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_default_expense_account_id",
                table: "vendor_invoice_templates",
                column: "default_expense_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_default_advance_tax_account_id",
                table: "vendor_invoice_templates",
                column: "default_advance_tax_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_default_sales_tax_input_account_id",
                table: "vendor_invoice_templates",
                column: "default_sales_tax_input_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_invoice_templates_default_payable_vendors_account_id",
                table: "vendor_invoice_templates",
                column: "default_payable_vendors_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_invoice_templates_chart_of_accounts_default_expense_~",
                table: "vendor_invoice_templates",
                column: "default_expense_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_invoice_templates_chart_of_accounts_default_adv_tax_~",
                table: "vendor_invoice_templates",
                column: "default_advance_tax_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_invoice_templates_chart_of_accounts_default_stx_inp_~",
                table: "vendor_invoice_templates",
                column: "default_sales_tax_input_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_invoice_templates_chart_of_accounts_default_pay_ven_~",
                table: "vendor_invoice_templates",
                column: "default_payable_vendors_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Create foreign keys for invoices GL accounts
            migrationBuilder.CreateIndex(
                name: "IX_invoices_expense_account_id",
                table: "invoices",
                column: "expense_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_advance_tax_account_id",
                table: "invoices",
                column: "advance_tax_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_sales_tax_input_account_id",
                table: "invoices",
                column: "sales_tax_input_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_payable_vendors_account_id",
                table: "invoices",
                column: "payable_vendors_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_chart_of_accounts_expense_account_id",
                table: "invoices",
                column: "expense_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_chart_of_accounts_advance_tax_account_id",
                table: "invoices",
                column: "advance_tax_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_chart_of_accounts_sales_tax_input_account_id",
                table: "invoices",
                column: "sales_tax_input_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_chart_of_accounts_payable_vendors_account_id",
                table: "invoices",
                column: "payable_vendors_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop invoice GL foreign keys
            migrationBuilder.DropForeignKey(name: "FK_invoices_chart_of_accounts_expense_account_id", table: "invoices");
            migrationBuilder.DropForeignKey(name: "FK_invoices_chart_of_accounts_advance_tax_account_id", table: "invoices");
            migrationBuilder.DropForeignKey(name: "FK_invoices_chart_of_accounts_sales_tax_input_account_id", table: "invoices");
            migrationBuilder.DropForeignKey(name: "FK_invoices_chart_of_accounts_payable_vendors_account_id", table: "invoices");

            // Drop vendor template GL foreign keys
            migrationBuilder.DropForeignKey(name: "FK_vendor_invoice_templates_chart_of_accounts_default_expense_~", table: "vendor_invoice_templates");
            migrationBuilder.DropForeignKey(name: "FK_vendor_invoice_templates_chart_of_accounts_default_adv_tax_~", table: "vendor_invoice_templates");
            migrationBuilder.DropForeignKey(name: "FK_vendor_invoice_templates_chart_of_accounts_default_stx_inp_~", table: "vendor_invoice_templates");
            migrationBuilder.DropForeignKey(name: "FK_vendor_invoice_templates_chart_of_accounts_default_pay_ven_~", table: "vendor_invoice_templates");

            // Drop indexes
            migrationBuilder.DropIndex(name: "IX_invoices_expense_account_id", table: "invoices");
            migrationBuilder.DropIndex(name: "IX_invoices_advance_tax_account_id", table: "invoices");
            migrationBuilder.DropIndex(name: "IX_invoices_sales_tax_input_account_id", table: "invoices");
            migrationBuilder.DropIndex(name: "IX_invoices_payable_vendors_account_id", table: "invoices");
            migrationBuilder.DropIndex(name: "IX_vendor_invoice_templates_default_expense_account_id", table: "vendor_invoice_templates");
            migrationBuilder.DropIndex(name: "IX_vendor_invoice_templates_default_advance_tax_account_id", table: "vendor_invoice_templates");
            migrationBuilder.DropIndex(name: "IX_vendor_invoice_templates_default_sales_tax_input_account_id", table: "vendor_invoice_templates");
            migrationBuilder.DropIndex(name: "IX_vendor_invoice_templates_default_payable_vendors_account_id", table: "vendor_invoice_templates");

            // Drop invoice columns
            migrationBuilder.DropColumn(name: "expense_account_id", table: "invoices");
            migrationBuilder.DropColumn(name: "advance_tax_account_id", table: "invoices");
            migrationBuilder.DropColumn(name: "sales_tax_input_account_id", table: "invoices");
            migrationBuilder.DropColumn(name: "payable_vendors_account_id", table: "invoices");
            migrationBuilder.DropColumn(name: "is_posted_to_gl", table: "invoices");
            migrationBuilder.DropColumn(name: "posted_to_gl_at", table: "invoices");
            migrationBuilder.DropColumn(name: "posted_to_gl_by", table: "invoices");

            // Drop vendor template columns
            migrationBuilder.DropColumn(name: "has_expense_account", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "has_advance_tax_account", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "has_sales_tax_input_account", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "has_payable_vendors_account", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "default_expense_account_id", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "default_advance_tax_account_id", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "default_sales_tax_input_account_id", table: "vendor_invoice_templates");
            migrationBuilder.DropColumn(name: "default_payable_vendors_account_id", table: "vendor_invoice_templates");
        }
    }
}
