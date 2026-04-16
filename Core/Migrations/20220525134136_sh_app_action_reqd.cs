using Microsoft.EntityFrameworkCore.Migrations;

namespace FirstReg.Core.Migrations
{
    public partial class sh_app_action_reqd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ActionRequired",
                table: "Shareholders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "Shareholders",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
            SELECT dbo.Shareholders.Id, dbo.Shareholders.Code, dbo.Shareholders.FullName, dbo.Shareholders.Street, dbo.Shareholders.City, dbo.Shareholders.State, dbo.Shareholders.Country, dbo.Shareholders.PrimaryPhone, dbo.Shareholders.SecondaryPhone, dbo.Shareholders.PostCode, dbo.Shareholders.ClearingNo, 
                       dbo.Shareholders.Verified, dbo.Shareholders.ActionRequired, dbo.AspNetUsers.AllowGroup, dbo.AspNetUsers.Email, dbo.AspNetUsers.UserName, dbo.AspNetUsers.PhoneNumber, dbo.Shareholders.StartDate, dbo.Shareholders.ExpiryDate
            FROM   dbo.Shareholders INNER JOIN
                       dbo.AspNetUsers ON dbo.Shareholders.UserId = dbo.AspNetUsers.Id
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
            migrationBuilder.DropColumn(
                name: "ActionRequired",
                table: "Shareholders");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Shareholders");

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
            SELECT dbo.Shareholders.Id, dbo.Shareholders.Code, dbo.Shareholders.FullName, dbo.Shareholders.Street, dbo.Shareholders.City, dbo.Shareholders.State, dbo.Shareholders.Country, dbo.Shareholders.PrimaryPhone, dbo.Shareholders.SecondaryPhone, dbo.Shareholders.PostCode, dbo.Shareholders.ClearingNo, 
                       dbo.Shareholders.Verified, dbo.AspNetUsers.AllowGroup, dbo.AspNetUsers.Email, dbo.AspNetUsers.UserName, dbo.AspNetUsers.PhoneNumber, dbo.Shareholders.StartDate, dbo.Shareholders.ExpiryDate
            FROM   dbo.Shareholders INNER JOIN
                       dbo.AspNetUsers ON dbo.Shareholders.UserId = dbo.AspNetUsers.Id
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
    }
}
