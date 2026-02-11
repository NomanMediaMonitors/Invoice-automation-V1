using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceAutomation.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAndEmployeeModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "companies",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "companies",
                type: "LONGTEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "companies",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "IndraajConnectedAt",
                table: "companies",
                type: "DATETIME(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FiscalYearStart",
                table: "companies",
                type: "DATETIME(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<string>(
                name: "DefaultCurrency",
                table: "companies",
                type: "LONGTEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "companies",
                type: "LONGTEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "companies",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "companies",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "LONGTEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "TINYINT(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "IndraajConnectedAt",
                table: "companies",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FiscalYearStart",
                table: "companies",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(6)");

            migrationBuilder.AlterColumn<string>(
                name: "DefaultCurrency",
                table: "companies",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "LONGTEXT")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "companies",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "LONGTEXT")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
