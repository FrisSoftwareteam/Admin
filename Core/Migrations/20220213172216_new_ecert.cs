using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class new_ecert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletedECertRequests");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "ECertRequests",
                newName: "AuthLetterFileName");

            migrationBuilder.CreateTable(
                name: "ECertHolder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "varchar(250)", nullable: false),
                    Brief = table.Column<string>(type: "varchar(MAX)", nullable: false),
                    IdFileName = table.Column<string>(type: "varchar(100)", nullable: false),
                    Signature = table.Column<string>(type: "varchar(100)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    ECertRequestId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ECertHolder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ECertHolder_ECertRequests_ECertRequestId",
                        column: x => x.ECertRequestId,
                        principalTable: "ECertRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ECertHolding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolderId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "varchar(250)", nullable: false),
                    Brief = table.Column<string>(type: "varchar(MAX)", nullable: false),
                    CertFileName = table.Column<string>(type: "varchar(100)", nullable: false),
                    Signature = table.Column<string>(type: "varchar(100)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    ECertHolderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ECertHolding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ECertHolding_ECertHolder_ECertHolderId",
                        column: x => x.ECertHolderId,
                        principalTable: "ECertHolder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ECertHolder_ECertRequestId",
                table: "ECertHolder",
                column: "ECertRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ECertHolding_ECertHolderId",
                table: "ECertHolding",
                column: "ECertHolderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ECertHolding");

            migrationBuilder.DropTable(
                name: "ECertHolder");

            migrationBuilder.RenameColumn(
                name: "AuthLetterFileName",
                table: "ECertRequests",
                newName: "FileName");

            migrationBuilder.CreateTable(
                name: "CompletedECertRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "varchar(500)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    FileName = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedECertRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedECertRequests_ECertRequests_Id",
                        column: x => x.Id,
                        principalTable: "ECertRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }
    }
}
