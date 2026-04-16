using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstReg.Core.Migrations
{
    public partial class sh_last_update_tracker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Shareholders",
                type: "datetime",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Shareholders");
        }
    }
}
