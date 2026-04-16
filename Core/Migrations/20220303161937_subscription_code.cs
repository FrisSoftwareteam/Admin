using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class subscription_code : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Subscriptions",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Subscriptions");
        }
    }
}
