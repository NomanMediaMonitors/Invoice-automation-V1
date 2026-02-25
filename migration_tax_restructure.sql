-- ============================================================================
-- Migration: Replace generic Tax with Advance Tax & Sales Tax on line items
-- Database: MySQL
-- Date: 2026-02-25
--
-- This migration:
-- 1. Adds AdvanceTaxRate, AdvanceTaxAmount, SalesTaxRate, SalesTaxAmount to invoice_line_items
-- 2. Migrates existing TaxRate/TaxAmount data to AdvanceTaxRate/AdvanceTaxAmount
-- 3. Drops old TaxRate, TaxAmount columns from invoice_line_items
-- 4. Drops TaxAmount from invoices (now calculated from line items)
-- 5. Drops HasTaxRate, DefaultTaxRate from vendor_invoice_templates
-- 6. Adds DefaultAdvanceTaxRate, DefaultSalesTaxRate to vendor_invoice_templates
-- 7. Recalculates invoice totals from line items
-- ============================================================================

-- Step 1: Add new columns to invoice_line_items
ALTER TABLE invoice_line_items
    ADD COLUMN advance_tax_rate DECIMAL(5,2) NOT NULL DEFAULT 0.00 AFTER amount,
    ADD COLUMN advance_tax_amount DECIMAL(18,2) NOT NULL DEFAULT 0.00 AFTER advance_tax_rate,
    ADD COLUMN sales_tax_rate DECIMAL(5,2) NOT NULL DEFAULT 0.00 AFTER advance_tax_amount,
    ADD COLUMN sales_tax_amount DECIMAL(18,2) NOT NULL DEFAULT 0.00 AFTER sales_tax_rate;

-- Step 2: Migrate existing tax data to advance_tax columns
-- (existing TaxRate/TaxAmount becomes AdvanceTaxRate/AdvanceTaxAmount)
UPDATE invoice_line_items
SET advance_tax_rate = tax_rate,
    advance_tax_amount = tax_amount
WHERE tax_rate > 0 OR tax_amount > 0;

-- Step 3: Drop old tax columns from invoice_line_items
ALTER TABLE invoice_line_items
    DROP COLUMN tax_rate,
    DROP COLUMN tax_amount;

-- Step 4: Drop TaxAmount from invoices (now calculated from line items)
ALTER TABLE invoices
    DROP COLUMN tax_amount;

-- Step 5: Recalculate invoice-level AdvanceTaxAmount and SalesTaxInputAmount from line items
UPDATE invoices i
SET i.advance_tax_amount = (
        SELECT COALESCE(SUM(li.advance_tax_amount), 0)
        FROM invoice_line_items li
        WHERE li.invoice_id = i.id
    ),
    i.sales_tax_input_amount = (
        SELECT COALESCE(SUM(li.sales_tax_amount), 0)
        FROM invoice_line_items li
        WHERE li.invoice_id = i.id
    ),
    i.total_amount = i.sub_total + (
        SELECT COALESCE(SUM(li.advance_tax_amount), 0)
        FROM invoice_line_items li
        WHERE li.invoice_id = i.id
    ) + (
        SELECT COALESCE(SUM(li.sales_tax_amount), 0)
        FROM invoice_line_items li
        WHERE li.invoice_id = i.id
    );

-- Step 6: Add new default tax rate columns to vendor_invoice_templates
ALTER TABLE vendor_invoice_templates
    ADD COLUMN default_advance_tax_rate DECIMAL(5,2) NULL AFTER default_payable_vendors_account_id,
    ADD COLUMN default_sales_tax_rate DECIMAL(5,2) NULL AFTER default_advance_tax_rate;

-- Step 7: Migrate existing DefaultTaxRate to DefaultAdvanceTaxRate
UPDATE vendor_invoice_templates
SET default_advance_tax_rate = default_tax_rate
WHERE default_tax_rate IS NOT NULL AND default_tax_rate > 0;

-- Step 8: Drop old columns from vendor_invoice_templates
ALTER TABLE vendor_invoice_templates
    DROP COLUMN has_tax_rate,
    DROP COLUMN default_tax_rate;
