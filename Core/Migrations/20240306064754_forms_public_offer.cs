using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstReg.Core.Migrations
{
    /// <inheritdoc />
    public partial class forms_public_offer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "AspNetUserRoles",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "FormResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UniqueKey = table.Column<string>(type: "varchar(50)", nullable: false),
                    ValidationCode = table.Column<string>(type: "varchar(50)", nullable: true),
                    FullName = table.Column<string>(type: "varchar(500)", nullable: false),
                    EmailAddress = table.Column<string>(type: "varchar(100)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(100)", nullable: false),
                    JsonData = table.Column<string>(type: "varchar(MAX)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShareOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegisterId = table.Column<int>(type: "int", nullable: false),
                    UniqueKey = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "varchar(100)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Price = table.Column<decimal>(type: "money", nullable: false),
                    AllowPublicOffer = table.Column<bool>(type: "bit", nullable: false),
                    AllowRightIssue = table.Column<bool>(type: "bit", nullable: false),
                    RightIssue_NoOfShares = table.Column<int>(type: "int", nullable: false),
                    RightIssue_Rights = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareOffers_Registers_RegisterId",
                        column: x => x.RegisterId,
                        principalTable: "Registers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ShareOfferId = table.Column<int>(type: "int", nullable: false),
                    FormResponseId = table.Column<int>(type: "int", nullable: false),
                    NoOfShares = table.Column<int>(type: "int", nullable: false),
                    Rights = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareSubscriptions_FormResponses_FormResponseId",
                        column: x => x.FormResponseId,
                        principalTable: "FormResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareSubscriptions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShareSubscriptions_ShareOffers_ShareOfferId",
                        column: x => x.ShareOfferId,
                        principalTable: "ShareOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_UniqueKey",
                table: "FormResponses",
                column: "UniqueKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShareOffers_RegisterId",
                table: "ShareOffers",
                column: "RegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareOffers_UniqueKey",
                table: "ShareOffers",
                column: "UniqueKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShareSubscriptions_FormResponseId",
                table: "ShareSubscriptions",
                column: "FormResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareSubscriptions_PaymentId",
                table: "ShareSubscriptions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareSubscriptions_ShareOfferId",
                table: "ShareSubscriptions",
                column: "ShareOfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "ShareSubscriptions");

            migrationBuilder.DropTable(
                name: "FormResponses");

            migrationBuilder.DropTable(
                name: "ShareOffers");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "AspNetUserRoles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
