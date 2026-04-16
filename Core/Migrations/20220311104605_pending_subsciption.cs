using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class pending_subsciption : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Confirmed",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
            @"GO
            UPDATE Subscriptions SET Confirmed = 1;
            GO");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Confirmed",
                table: "Subscriptions");
        }
    }
}
