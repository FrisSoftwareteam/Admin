using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstReg.Core.Migrations
{
    /// <inheritdoc />
    public partial class offer_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ShareOffers",
                newName: "RightIssue_Price");

            migrationBuilder.AddColumn<int>(
                name: "PublicOffer_Factor",
                table: "ShareOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PublicOffer_Minimum",
                table: "ShareOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PublicOffer_Price",
                table: "ShareOffers",
                type: "money",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RightIssue_Factor",
                table: "ShareOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicOffer_Factor",
                table: "ShareOffers");

            migrationBuilder.DropColumn(
                name: "PublicOffer_Minimum",
                table: "ShareOffers");

            migrationBuilder.DropColumn(
                name: "PublicOffer_Price",
                table: "ShareOffers");

            migrationBuilder.DropColumn(
                name: "RightIssue_Factor",
                table: "ShareOffers");

            migrationBuilder.RenameColumn(
                name: "RightIssue_Price",
                table: "ShareOffers",
                newName: "Price");
        }
    }
}
