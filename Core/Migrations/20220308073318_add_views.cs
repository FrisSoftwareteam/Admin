using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class add_views : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Subscriptions",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.Sql(
            @"GO
            DECLARE @viewsql nvarchar(MAX)
            SET @viewsql = (SELECT 'DROP VIEW ' + name + '; ' FROM sys.views FOR xml path(''))
            EXEC (@viewsql)
            GO
            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            CREATE VIEW [dbo].[vw_Registers]
            AS
            SELECT Id, UserId, Code, Name, Email, Phone, Fax, Address, RCNo, Active, Symbol
            FROM   dbo.Registers
            GO
            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            CREATE VIEW [dbo].[vw_Shareholders]
            AS
            SELECT Id, Code, FullName, Street, City, State, Country, PrimaryPhone, SecondaryPhone, PostCode, ClearingNo, Verified
            FROM   dbo.Shareholders
            GO
            SET ANSI_NULLS ON
            GO
            SET QUOTED_IDENTIFIER ON
            GO
            CREATE VIEW [dbo].[vw_Shareholdings]
            AS
            SELECT dbo.ShareHoldings.Id, dbo.ShareHoldings.RegisterId, dbo.Registers.Code AS RegisterCode, dbo.Registers.Name AS Register, dbo.ShareHoldings.ShareHolderId, dbo.Shareholders.Code AS ShareholderCode, dbo.Shareholders.FullName AS Shareholder, dbo.Shareholders.ClearingNo, dbo.ShareHoldings.AccountNo, 
                       dbo.ShareHoldings.AccountName, dbo.ShareHoldings.Units, dbo.ShareHoldings.Value, dbo.ShareHoldings.Status, dbo.ShareHoldings.Date
            FROM   dbo.ShareHoldings INNER JOIN
                       dbo.Shareholders ON dbo.ShareHoldings.ShareHolderId = dbo.Shareholders.Id INNER JOIN
                       dbo.Registers ON dbo.ShareHoldings.RegisterId = dbo.Registers.Id
            GO");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Subscriptions",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.Sql(
            @"GO
            DECLARE @viewsql nvarchar(MAX)
            SET @viewsql = (SELECT 'DROP VIEW ' + name + '; ' FROM sys.views FOR xml path(''))
            EXEC (@viewsql)
            GO");
        }
    }
}
