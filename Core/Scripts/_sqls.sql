--SELECT 

--	UPPER(FullName) AS FullName, LOWER(EmailAddress) AS UserName, UPPER(EmailAddress) AS NormalizedUserName, 
--	LOWER(EmailAddress) AS Email, UPPER(EmailAddress) AS NormalizedEmail, 0 AS EmailConfirmed, 
--	PrimaryGSM AS PhoneNumber, 0 AS PhoneNumberConfirmed, 0 AS Type, 0 AS TwoFactorEnabled,
--	1 AS LockoutEnabled, 0 AS AccessFailedCount,

--	0 AS Status, '' AS City, '' AS State, '' AS Country, '' AS PostCode, GETDATE() AS Date, 
--	CardUserID AS CardId, ClearingNo, CreateDate AS CreatedOn, OnlineID AS LegacyId, 
--	UserName AS LegacyUsername, mAccessPIN AS MAccessPin, SecondaryGSM AS SecondaryPhone, 
--	'' AS Street, NULL AS Signature, ID AS LegacyAccId

--FROM Qry_Online_Details
--WHERE (EmailAddress <> '') AND (EmailAddress LIKE '%@%')



--INSERT INTO AspNetUsers
--           (Type, FullName, UserName, NormalizedUserName, Email, NormalizedEmail, 
--		    EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
--			LockoutEnabled, AccessFailedCount)
--SELECT 
--			Type, FullName, UserName, NormalizedUserName, Email, NormalizedEmail, 
--			EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
--			LockoutEnabled, LegacyAccId
--FROM Account.dbo._Shareholders 
--WHERE Email NOT IN (SELECT UserName FROM Account.dbo._Shareholders GROUP BY UserName HAVING(COUNT(*) > 1))
--ORDER BY Email


-- new

--INSERT INTO AspNetUsers
--           (Type, FullName, UserName, NormalizedUserName, Email, NormalizedEmail, 
--		    EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
--			LockoutEnabled, AccessFailedCount, AllowGroup)
--SELECT 
--			0 AS Type, MIN(FullName), LOWER(LTRIM(RTRIM(EmailAddress))), UPPER(LTRIM(RTRIM(EmailAddress))), LOWER(LTRIM(RTRIM(EmailAddress))), UPPER(LTRIM(RTRIM(EmailAddress))), 
--			1 AS EmailConfirmed, min(primarygsm) AS PhoneNumber, 0 AS PhoneNumberConfirmed, 0 AS TwoFactorEnabled, 
--			1 AS LockoutEnabled, MIN(ID) AS AccessFailedCount, (CASE WHEN COUNT(*) > 1 THEN 1 ELSE 0 END) AS AllowGroup
--FROM            Account.dbo.Qry_Online_Details
--where LTRIM(RTRIM(EmailAddress)) <> '' and LTRIM(RTRIM(EmailAddress)) like '%@%'
--group by LTRIM(RTRIM(EmailAddress))
--order by MIN(FullName)



--INSERT INTO Shareholders
--		(UserId, Code, FullName, Street, City, State, Country, PrimaryPhone, SecondaryPhone, PostCode, 
--		 Date, Signature, Verified, ClearingNo, VerifiedOn, VerifiedBy, IsCompany, 
--		 LegacyAccId, LegacyId, LegacyUsername, MAccessPin, CreatedOn, CardId)
--SELECT	 AspNetUsers.Id, '' AS Code, SH.FullName, Street, City, State, Country, SH.PhoneNumber AS PrimaryPhone, 
--		 SecondaryPhone, PostCode, Date, 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGQAAAAyCAIAAAAlV+npAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAB3SURBVGhD7dAxAQAwEAOh+jedWvjbQQJvnMkKZAWyAlmBrEBWICuQFcgKZAWyAlmBrEBWICuQFcgKZAWyAlmBrEBWICuQFcgKZAWyAlmBrEBWICuQFcgKZAWyAlmBrEBWICuQFcgKZAWyAlmBrEBWICuQFcg62z5o0mDPbKb5CQAAAABJRU5ErkJggg==' AS Signature, 
--		 1 AS Verified, ClearingNo, CreatedOn As VerifiedOn, 'admin' AS VerifiedBy, 0 AS IsCompany, LegacyAccId,
--		 LegacyId, LegacyUsername, MAccessPin, CreatedOn, CardId
--FROM Account.dbo._Shareholders AS SH
--INNER JOIN AspNetUsers ON SH.Email COLLATE DATABASE_DEFAULT = AspNetUsers.Email COLLATE DATABASE_DEFAULT
--ORDER BY FullName




SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [dbo].[_Registers]
AS
SELECT        TOP (100) PERCENT MAX(dbo.T_regist.prc_no) AS prc_no, MAX(dbo.T_Company.prc_name) AS prc_name, dbo.T_regist.register_code, dbo.T_regist.symbol, MAX(Tbl_Register_1.StatusID) AS StatusID, 
                         MAX(Tbl_Register_1.nDecimal) AS nDecimal, MAX(dbo.T_Company.auth_share) AS auth_share, MAX(dbo.T_regist.changedat) AS changedat, MAX(dbo.T_regist.changedby) AS changedby, MAX(dbo.T_Company.prc_address) 
                         AS prc_address, MAX(dbo.T_Company.prc_email) AS prc_email, MAX(dbo.T_Company.prc_fax) AS prc_fax, MAX(dbo.T_Company.prc_phone) AS prc_phone, MAX(dbo.T_Company.prc_type) AS prc_type, 
                         MAX(dbo.T_regist.createdat) AS createdat, MAX(dbo.T_Company.date_incorporate) AS date_incorporate, MAX(dbo.T_Company.date_listed) AS date_listed, MAX(dbo.T_Company.rc_number) AS rc_number, 
                         MAX(dbo.T_regist.enteredby) AS enteredby, MAX(dbo.T_Company.website) AS website, MAX(dbo.T_regist.takeupdt) AS takeupdt, MAX(dbo.T_regist.register_desc) AS register_desc, dbo.T_regist.nomvalue, 
                         dbo.T_regist.security_type, dbo.T_regist.actual_shares, dbo.T_regist.caution, dbo.T_regist.active, dbo.T_regist.fraction, dbo.T_regist.fund_cert_narr
FROM            dbo.T_Company INNER JOIN
                         dbo.T_regist ON dbo.T_Company.prc_no = dbo.T_regist.prc_no INNER JOIN
                         dbo.Tbl_Register AS Tbl_Register_1 ON dbo.T_Company.prc_no = Tbl_Register_1.Prc_No
GROUP BY dbo.T_regist.register_code, dbo.T_regist.symbol, dbo.T_regist.nomvalue, dbo.T_regist.security_type, dbo.T_regist.actual_shares, dbo.T_regist.caution, dbo.T_regist.fund_cert_narr, dbo.T_regist.active, dbo.T_regist.fraction
GO

-----

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [dbo].[_Shareholders]
AS
SELECT        UPPER(FullName) AS FullName, LOWER(EmailAddress) AS UserName, UPPER(EmailAddress) AS NormalizedUserName, LOWER(EmailAddress) AS Email, UPPER(EmailAddress) AS NormalizedEmail, 0 AS EmailConfirmed, 
                         PrimaryGSM AS PhoneNumber, 0 AS PhoneNumberConfirmed, 0 AS Type, 0 AS TwoFactorEnabled, 1 AS LockoutEnabled, 0 AS AccessFailedCount, 0 AS Status, '' AS City, '' AS State, '' AS Country, '' AS PostCode, 
                         CreateDate AS Date, CardUserID AS CardId, ClearingNo, CreateDate AS CreatedOn, OnlineID AS LegacyId, UserName AS LegacyUsername, mAccessPIN AS MAccessPin, SecondaryGSM AS SecondaryPhone, '' AS Street, NULL 
                         AS Signature, ID AS LegacyAccId, 1 AS Verified, GETDATE() AS VerifiedOn, 'admin' AS VerifiedBy, 0 AS IsCompany
FROM            dbo.Qry_Online_Details
GO