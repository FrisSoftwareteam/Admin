using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstReg.Core.Migrations
{
    public partial class strip_register : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualShares",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "Caution",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "Decimal",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "Fraction",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "IncorporatedOn",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "ListedOn",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "Narration",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "NomValue",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "SecurityType",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Registers");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Registers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ActualShares",
                table: "Registers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Caution",
                table: "Registers",
                type: "varchar(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Registers",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Registers",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<int>(
                name: "Decimal",
                table: "Registers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "Registers",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Fraction",
                table: "Registers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IncorporatedOn",
                table: "Registers",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ListedOn",
                table: "Registers",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Narration",
                table: "Registers",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NomValue",
                table: "Registers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityType",
                table: "Registers",
                type: "varchar(25)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Registers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Registers",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Registers",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Registers",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()");
        }
    }
}
