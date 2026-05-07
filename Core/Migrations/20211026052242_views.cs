using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class views : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Data.Seed.Data);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
