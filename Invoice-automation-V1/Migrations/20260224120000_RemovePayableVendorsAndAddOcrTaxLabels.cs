using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class RemovePayableVendorsAndAddOcrTaxLabels : Migration
    {
        private void AddColumnIfNotExists(MigrationBuilder migrationBuilder, string table, string column, string columnDef)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}') = 0,
                    'ALTER TABLE `{table}` ADD COLUMN `{column}` {columnDef}',
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

        private void DropForeignKeyIfExists(MigrationBuilder migrationBuilder, string table, string keyName)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND CONSTRAINT_NAME = '{keyName}') > 0,
                    'ALTER TABLE `{table}` DROP FOREIGN KEY `{keyName}`',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        private void DropIndexIfExists(MigrationBuilder migrationBuilder, string table, string indexName)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND INDEX_NAME = '{indexName}') > 0,
                    'ALTER TABLE `{table}` DROP INDEX `{indexName}`',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove Payable Vendors from invoices table (no longer needed on invoice - comes from template)
            DropForeignKeyIfExists(migrationBuilder, "invoices", "FK_invoices_chart_of_accounts_payable_vendors_account_id");
            DropIndexIfExists(migrationBuilder, "invoices", "IX_invoices_payable_vendors_account_id");
            DropColumnIfExists(migrationBuilder, "invoices", "payable_vendors_account_id");

            // Remove has_payable_vendors_account toggle from vendor_invoice_templates (always required now)
            DropColumnIfExists(migrationBuilder, "vendor_invoice_templates", "has_payable_vendors_account");

            // Keep default_payable_vendors_account_id in vendor_invoice_templates (it stays)

            // Add OCR label mapping columns for Advance Tax and Sales Tax Input amounts
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "advance_tax_amount_label", "VARCHAR(100) NULL");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "sales_tax_input_amount_label", "VARCHAR(100) NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add Payable Vendors to invoices
            AddColumnIfNotExists(migrationBuilder, "invoices", "payable_vendors_account_id", "CHAR(36) NULL");

            // Re-add has_payable_vendors_account toggle to vendor_invoice_templates
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "has_payable_vendors_account", "TINYINT(1) NOT NULL DEFAULT 1");

            // Remove OCR label mapping columns
            DropColumnIfExists(migrationBuilder, "vendor_invoice_templates", "advance_tax_amount_label");
            DropColumnIfExists(migrationBuilder, "vendor_invoice_templates", "sales_tax_input_amount_label");
        }
    }
}
