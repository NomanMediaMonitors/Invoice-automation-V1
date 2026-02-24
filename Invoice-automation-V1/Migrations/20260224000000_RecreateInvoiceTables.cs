using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class RecreateInvoiceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recreate invoices table with full final-state schema if it was deleted
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `invoices` (
                    `id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
                    `company_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
                    `vendor_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `invoice_number` VARCHAR(100) CHARACTER SET utf8mb4 NOT NULL,
                    `invoice_date` DATE NOT NULL,
                    `due_date` DATE NULL,
                    `sub_total` DECIMAL(18,2) NOT NULL,
                    `tax_amount` DECIMAL(18,2) NOT NULL,
                    `total_amount` DECIMAL(18,2) NOT NULL,
                    `currency` VARCHAR(10) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'PKR',
                    `status` VARCHAR(50) CHARACTER SET utf8mb4 NOT NULL,
                    `description` VARCHAR(1000) CHARACTER SET utf8mb4 NULL,
                    `notes` TEXT CHARACTER SET utf8mb4 NULL,
                    `original_file_name` VARCHAR(255) CHARACTER SET utf8mb4 NULL,
                    `file_storage_path` VARCHAR(500) CHARACTER SET utf8mb4 NULL,
                    `file_url` VARCHAR(500) CHARACTER SET utf8mb4 NULL,
                    `file_type` VARCHAR(50) CHARACTER SET utf8mb4 NULL,
                    `file_size` BIGINT NULL,
                    `is_ocr_processed` TINYINT(1) NOT NULL DEFAULT 0,
                    `ocr_processed_at` DATETIME NULL,
                    `ocr_confidence_score` DECIMAL(5,2) NULL,
                    `ocr_raw_data` LONGTEXT CHARACTER SET utf8mb4 NULL,
                    `ocr_error_message` VARCHAR(1000) CHARACTER SET utf8mb4 NULL,
                    `approved_by` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `approved_at` DATETIME NULL,
                    `approval_notes` VARCHAR(1000) CHARACTER SET utf8mb4 NULL,
                    `paid_at` DATETIME NULL,
                    `payment_reference` VARCHAR(100) CHARACTER SET utf8mb4 NULL,
                    `paid_by` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `indraaj_voucher_no` VARCHAR(100) CHARACTER SET utf8mb4 NULL,
                    `synced_to_indraaj_at` DATETIME NULL,
                    `created_by` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    `updated_by` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `advance_tax_account_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `advance_tax_amount` DECIMAL(18,2) NOT NULL DEFAULT 0,
                    `sales_tax_input_account_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `sales_tax_input_amount` DECIMAL(18,2) NOT NULL DEFAULT 0,
                    `payable_vendors_account_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `is_posted_to_gl` TINYINT(1) NOT NULL DEFAULT 0,
                    `posted_to_gl_at` DATETIME NULL,
                    `posted_to_gl_by` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `FK_invoices_companies_company_id` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_invoices_vendors_vendor_id` FOREIGN KEY (`vendor_id`) REFERENCES `vendors` (`id`) ON DELETE SET NULL,
                    CONSTRAINT `FK_invoices_chart_of_accounts_advance_tax_account_id` FOREIGN KEY (`advance_tax_account_id`) REFERENCES `chart_of_accounts` (`id`) ON DELETE SET NULL,
                    CONSTRAINT `FK_invoices_chart_of_accounts_sales_tax_input_account_id` FOREIGN KEY (`sales_tax_input_account_id`) REFERENCES `chart_of_accounts` (`id`) ON DELETE SET NULL,
                    CONSTRAINT `FK_invoices_chart_of_accounts_payable_vendors_account_id` FOREIGN KEY (`payable_vendors_account_id`) REFERENCES `chart_of_accounts` (`id`) ON DELETE SET NULL
                ) CHARACTER SET utf8mb4;");

            // Create indexes on invoices (IF NOT EXISTS supported in MySQL 8.0 for CREATE INDEX is not available, so use a conditional approach)
            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_company_id_invoice_number') = 0,
                    'CREATE UNIQUE INDEX `IX_invoices_company_id_invoice_number` ON `invoices` (`company_id`, `invoice_number`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_vendor_id') = 0,
                    'CREATE INDEX `IX_invoices_vendor_id` ON `invoices` (`vendor_id`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_status') = 0,
                    'CREATE INDEX `IX_invoices_status` ON `invoices` (`status`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_invoice_date') = 0,
                    'CREATE INDEX `IX_invoices_invoice_date` ON `invoices` (`invoice_date`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_due_date') = 0,
                    'CREATE INDEX `IX_invoices_due_date` ON `invoices` (`due_date`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_advance_tax_account_id') = 0,
                    'CREATE INDEX `IX_invoices_advance_tax_account_id` ON `invoices` (`advance_tax_account_id`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_sales_tax_input_account_id') = 0,
                    'CREATE INDEX `IX_invoices_sales_tax_input_account_id` ON `invoices` (`sales_tax_input_account_id`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoices' AND INDEX_NAME = 'IX_invoices_payable_vendors_account_id') = 0,
                    'CREATE INDEX `IX_invoices_payable_vendors_account_id` ON `invoices` (`payable_vendors_account_id`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            // Recreate invoice_line_items table with full final-state schema if it was deleted
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `invoice_line_items` (
                    `id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
                    `invoice_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
                    `line_number` INT NOT NULL,
                    `description` VARCHAR(500) CHARACTER SET utf8mb4 NOT NULL,
                    `quantity` DECIMAL(18,4) NOT NULL,
                    `unit_price` DECIMAL(18,2) NOT NULL,
                    `amount` DECIMAL(18,2) NOT NULL,
                    `tax_rate` DECIMAL(5,2) NOT NULL,
                    `tax_amount` DECIMAL(18,2) NOT NULL,
                    `total_amount` DECIMAL(18,2) NOT NULL,
                    `chart_of_account_id` CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
                    `account_code` VARCHAR(50) CHARACTER SET utf8mb4 NULL,
                    `is_ocr_extracted` TINYINT(1) NOT NULL DEFAULT 0,
                    `ocr_confidence_score` DECIMAL(5,2) NULL,
                    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `FK_invoice_line_items_invoices_invoice_id` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_invoice_line_items_chart_of_accounts_chart_of_account_id` FOREIGN KEY (`chart_of_account_id`) REFERENCES `chart_of_accounts` (`id`) ON DELETE SET NULL
                ) CHARACTER SET utf8mb4;");

            // Create indexes on invoice_line_items
            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoice_line_items' AND INDEX_NAME = 'IX_invoice_line_items_invoice_id_line_number') = 0,
                    'CREATE UNIQUE INDEX `IX_invoice_line_items_invoice_id_line_number` ON `invoice_line_items` (`invoice_id`, `line_number`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @sql = (SELECT IF(
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'invoice_line_items' AND INDEX_NAME = 'IX_invoice_line_items_chart_of_account_id') = 0,
                    'CREATE INDEX `IX_invoice_line_items_chart_of_account_id` ON `invoice_line_items` (`chart_of_account_id`)',
                    'SELECT 1'));
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Don't drop tables in Down - the original AddInvoiceTables migration handles that
        }
    }
}
