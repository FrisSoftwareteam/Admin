using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class align_ecertreq : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ECertHolder_ECertRequests_ECertRequestId",
                table: "ECertHolder");

            migrationBuilder.DropForeignKey(
                name: "FK_ECertHolding_ECertHolder_ECertHolderId",
                table: "ECertHolding");

            migrationBuilder.DropForeignKey(
                name: "FK_ECertRequests_StockBrokers_StockBrokerId",
                table: "ECertRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ECertHolding",
                table: "ECertHolding");

            migrationBuilder.DropIndex(
                name: "IX_ECertHolding_ECertHolderId",
                table: "ECertHolding");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ECertHolder",
                table: "ECertHolder");

            migrationBuilder.DropIndex(
                name: "IX_ECertHolder_ECertRequestId",
                table: "ECertHolder");

            migrationBuilder.DropColumn(
                name: "Brief",
                table: "ECertHolding");

            migrationBuilder.DropColumn(
                name: "CertFileName",
                table: "ECertHolding");

            migrationBuilder.DropColumn(
                name: "ECertHolderId",
                table: "ECertHolding");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "ECertHolding");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "ECertHolding");

            migrationBuilder.DropColumn(
                name: "Brief",
                table: "ECertHolder");

            migrationBuilder.DropColumn(
                name: "ECertRequestId",
                table: "ECertHolder");

            migrationBuilder.RenameTable(
                name: "ECertHolding",
                newName: "ECertHoldings");

            migrationBuilder.RenameTable(
                name: "ECertHolder",
                newName: "ECertHolders");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "ECertHoldings",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AddColumn<string>(
                name: "AccountNo",
                table: "ECertHoldings",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CertificateNo",
                table: "ECertHoldings",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClearingNo",
                table: "ECertHoldings",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RegisterId",
                table: "ECertHoldings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Units",
                table: "ECertHoldings",
                type: "money",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Signature",
                table: "ECertHolders",
                type: "varchar(MAX)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "ECertHolders",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "ECertHolders",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ECertHoldings",
                table: "ECertHoldings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ECertHolders",
                table: "ECertHolders",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ECertHoldings_HolderId",
                table: "ECertHoldings",
                column: "HolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ECertHoldings_RegisterId",
                table: "ECertHoldings",
                column: "RegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_ECertHolders_RequestId",
                table: "ECertHolders",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ECertHolders_ECertRequests_RequestId",
                table: "ECertHolders",
                column: "RequestId",
                principalTable: "ECertRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ECertHoldings_ECertHolders_HolderId",
                table: "ECertHoldings",
                column: "HolderId",
                principalTable: "ECertHolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ECertHoldings_Registers_RegisterId",
                table: "ECertHoldings",
                column: "RegisterId",
                principalTable: "Registers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ECertRequests_StockBrokers_StockBrokerId",
                table: "ECertRequests",
                column: "StockBrokerId",
                principalTable: "StockBrokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ECertHolders_ECertRequests_RequestId",
                table: "ECertHolders");

            migrationBuilder.DropForeignKey(
                name: "FK_ECertHoldings_ECertHolders_HolderId",
                table: "ECertHoldings");

            migrationBuilder.DropForeignKey(
                name: "FK_ECertHoldings_Registers_RegisterId",
                table: "ECertHoldings");

            migrationBuilder.DropForeignKey(
                name: "FK_ECertRequests_StockBrokers_StockBrokerId",
                table: "ECertRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ECertHoldings",
                table: "ECertHoldings");

            migrationBuilder.DropIndex(
                name: "IX_ECertHoldings_HolderId",
                table: "ECertHoldings");

            migrationBuilder.DropIndex(
                name: "IX_ECertHoldings_RegisterId",
                table: "ECertHoldings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ECertHolders",
                table: "ECertHolders");

            migrationBuilder.DropIndex(
                name: "IX_ECertHolders_RequestId",
                table: "ECertHolders");

            migrationBuilder.DropColumn(
                name: "AccountNo",
                table: "ECertHoldings");

            migrationBuilder.DropColumn(
                name: "CertificateNo",
                table: "ECertHoldings");

            migrationBuilder.DropColumn(
                name: "ClearingNo",
                table: "ECertHoldings");

            migrationBuilder.DropColumn(
                name: "RegisterId",
                table: "ECertHoldings");

            migrationBuilder.DropColumn(
                name: "Units",
                table: "ECertHoldings");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "ECertHolders");

            migrationBuilder.RenameTable(
                name: "ECertHoldings",
                newName: "ECertHolding");

            migrationBuilder.RenameTable(
                name: "ECertHolders",
                newName: "ECertHolder");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "ECertHolding",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Brief",
                table: "ECertHolding",
                type: "varchar(MAX)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CertFileName",
                table: "ECertHolding",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ECertHolderId",
                table: "ECertHolding",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "ECertHolding",
                type: "varchar(250)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "ECertHolding",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Signature",
                table: "ECertHolder",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(MAX)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "ECertHolder",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Brief",
                table: "ECertHolder",
                type: "varchar(MAX)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ECertRequestId",
                table: "ECertHolder",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ECertHolding",
                table: "ECertHolding",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ECertHolder",
                table: "ECertHolder",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ECertHolding_ECertHolderId",
                table: "ECertHolding",
                column: "ECertHolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ECertHolder_ECertRequestId",
                table: "ECertHolder",
                column: "ECertRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ECertHolder_ECertRequests_ECertRequestId",
                table: "ECertHolder",
                column: "ECertRequestId",
                principalTable: "ECertRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ECertHolding_ECertHolder_ECertHolderId",
                table: "ECertHolding",
                column: "ECertHolderId",
                principalTable: "ECertHolder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ECertRequests_StockBrokers_StockBrokerId",
                table: "ECertRequests",
                column: "StockBrokerId",
                principalTable: "StockBrokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
