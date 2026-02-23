using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExpenseAccountFromGL : Migration
    {
        private void DropForeignKeyIfExists(MigrationBuilder migrationBuilder, string fkName, string table)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND CONSTRAINT_NAME = '{fkName}') > 0,
                    'ALTER TABLE `{table}` DROP FOREIGN KEY `{fkName}`',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        private void DropIndexIfExists(MigrationBuilder migrationBuilder, string indexName, string table)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND INDEX_NAME = '{indexName}') > 0,
                    'DROP INDEX `{indexName}` ON `{table}`',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        private void DropColumnIfExists(MigrationBuilder migrationBuilder, string table, string column)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}') > 0,
                    'ALTER TABLE `{table}` DROP COLUMN `{column}`',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop invoice expense_account_id foreign key and index
            DropForeignKeyIfExists(migrationBuilder, "FK_invoices_chart_of_accounts_expense_account_id", "invoices");
            DropIndexIfExists(migrationBuilder, "IX_invoices_expense_account_id", "invoices");
            DropColumnIfExists(migrationBuilder, "invoices", "expense_account_id");

            // Drop vendor template default_expense_account_id foreign key and index
            DropForeignKeyIfExists(migrationBuilder, "FK_vendor_invoice_templates_chart_of_accounts_default_expense_~", "vendor_invoice_templates");
            DropIndexIfExists(migrationBuilder, "IX_vendor_invoice_templates_default_expense_account_id", "vendor_invoice_templates");
            DropColumnIfExists(migrationBuilder, "vendor_invoice_templates", "default_expense_account_id");

            // Drop has_expense_account from vendor templates
            DropColumnIfExists(migrationBuilder, "vendor_invoice_templates", "has_expense_account");
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
