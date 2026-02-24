using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class AddGLAccountsAndPostToGL : Migration
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

        private void CreateIndexIfNotExists(MigrationBuilder migrationBuilder, string indexName, string table, string column)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND INDEX_NAME = '{indexName}') = 0,
                    'CREATE INDEX `{indexName}` ON `{table}` (`{column}`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        private void AddForeignKeyIfNotExists(MigrationBuilder migrationBuilder, string fkName, string table, string column, string refTable, string refColumn, string onDelete)
        {
            migrationBuilder.Sql($@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND CONSTRAINT_NAME = '{fkName}') = 0,
                    'ALTER TABLE `{table}` ADD CONSTRAINT `{fkName}` FOREIGN KEY (`{column}`) REFERENCES `{refTable}` (`{refColumn}`) ON DELETE {onDelete}',
                    'SELECT 1'));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add GL account toggle fields to vendor_invoice_templates
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "has_expense_account", "TINYINT(1) NOT NULL DEFAULT 1");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "has_advance_tax_account", "TINYINT(1) NOT NULL DEFAULT 1");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "has_sales_tax_input_account", "TINYINT(1) NOT NULL DEFAULT 1");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "has_payable_vendors_account", "TINYINT(1) NOT NULL DEFAULT 1");

            // Add default GL account ID fields to vendor_invoice_templates
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "default_expense_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "default_advance_tax_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "default_sales_tax_input_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");
            AddColumnIfNotExists(migrationBuilder, "vendor_invoice_templates", "default_payable_vendors_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");

            // Add GL account assignment fields to invoices
            AddColumnIfNotExists(migrationBuilder, "invoices", "expense_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");
            AddColumnIfNotExists(migrationBuilder, "invoices", "advance_tax_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");
            AddColumnIfNotExists(migrationBuilder, "invoices", "sales_tax_input_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");
            AddColumnIfNotExists(migrationBuilder, "invoices", "payable_vendors_account_id", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");

            // Add GL posting fields to invoices
            AddColumnIfNotExists(migrationBuilder, "invoices", "is_posted_to_gl", "TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfNotExists(migrationBuilder, "invoices", "posted_to_gl_at", "DATETIME NULL");
            AddColumnIfNotExists(migrationBuilder, "invoices", "posted_to_gl_by", "CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL");

            // Create indexes for vendor_invoice_templates GL accounts
            CreateIndexIfNotExists(migrationBuilder, "IX_vendor_invoice_templates_default_expense_account_id", "vendor_invoice_templates", "default_expense_account_id");
            CreateIndexIfNotExists(migrationBuilder, "IX_vendor_invoice_templates_default_advance_tax_account_id", "vendor_invoice_templates", "default_advance_tax_account_id");
            CreateIndexIfNotExists(migrationBuilder, "IX_vendor_invoice_templates_default_sales_tax_input_account_id", "vendor_invoice_templates", "default_sales_tax_input_account_id");
            CreateIndexIfNotExists(migrationBuilder, "IX_vendor_invoice_templates_default_payable_vendors_account_id", "vendor_invoice_templates", "default_payable_vendors_account_id");

            // Create foreign keys for vendor_invoice_templates GL accounts
            AddForeignKeyIfNotExists(migrationBuilder, "FK_vendor_invoice_templates_chart_of_accounts_default_expense_~", "vendor_invoice_templates", "default_expense_account_id", "chart_of_accounts", "id", "SET NULL");
            AddForeignKeyIfNotExists(migrationBuilder, "FK_vendor_invoice_templates_chart_of_accounts_default_adv_tax_~", "vendor_invoice_templates", "default_advance_tax_account_id", "chart_of_accounts", "id", "SET NULL");
            AddForeignKeyIfNotExists(migrationBuilder, "FK_vendor_invoice_templates_chart_of_accounts_default_stx_inp_~", "vendor_invoice_templates", "default_sales_tax_input_account_id", "chart_of_accounts", "id", "SET NULL");
            AddForeignKeyIfNotExists(migrationBuilder, "FK_vendor_invoice_templates_chart_of_accounts_default_pay_ven_~", "vendor_invoice_templates", "default_payable_vendors_account_id", "chart_of_accounts", "id", "SET NULL");

            // Create indexes for invoices GL accounts
            CreateIndexIfNotExists(migrationBuilder, "IX_invoices_expense_account_id", "invoices", "expense_account_id");
            CreateIndexIfNotExists(migrationBuilder, "IX_invoices_advance_tax_account_id", "invoices", "advance_tax_account_id");
            CreateIndexIfNotExists(migrationBuilder, "IX_invoices_sales_tax_input_account_id", "invoices", "sales_tax_input_account_id");
            CreateIndexIfNotExists(migrationBuilder, "IX_invoices_payable_vendors_account_id", "invoices", "payable_vendors_account_id");

            // Create foreign keys for invoices GL accounts
            AddForeignKeyIfNotExists(migrationBuilder, "FK_invoices_chart_of_accounts_expense_account_id", "invoices", "expense_account_id", "chart_of_accounts", "id", "SET NULL");
            AddForeignKeyIfNotExists(migrationBuilder, "FK_invoices_chart_of_accounts_advance_tax_account_id", "invoices", "advance_tax_account_id", "chart_of_accounts", "id", "SET NULL");
            AddForeignKeyIfNotExists(migrationBuilder, "FK_invoices_chart_of_accounts_sales_tax_input_account_id", "invoices", "sales_tax_input_account_id", "chart_of_accounts", "id", "SET NULL");
            AddForeignKeyIfNotExists(migrationBuilder, "FK_invoices_chart_of_accounts_payable_vendors_account_id", "invoices", "payable_vendors_account_id", "chart_of_accounts", "id", "SET NULL");
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
