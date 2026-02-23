using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceTaxAndSalesTaxAmounts : Migration
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddColumnIfNotExists(migrationBuilder, "invoices", "advance_tax_amount", "DECIMAL(18,2) NOT NULL DEFAULT 0");
            AddColumnIfNotExists(migrationBuilder, "invoices", "sales_tax_input_amount", "DECIMAL(18,2) NOT NULL DEFAULT 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "advance_tax_amount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "sales_tax_input_amount",
                table: "invoices");
        }
    }
}
