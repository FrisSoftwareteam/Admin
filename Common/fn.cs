namespace FirstReg
{
    public static class Common
    {
        public static string APISettingName => "apiurl";
        public static string AccessSettingName => "accessurl";
        public static string MongoUriSettingName => "mongouri";
        public static Dictionary<string, string> ApiKeyHeader => new() { { "key", APIkey } };
        public static string APIkey => 
            "v#wqhY#3'HQ3v&El*3~0[SSba_@8/6yZ()c(+;dJZO0mI]8B.'+c!@lu,o1CnJv^>t>j=%J!ECf[nr~6&XQp<HF/X=%|9C_/]~TqR2wxI5xX_,4*XOk^wSo8v)|)j_Wx&xWJk.{Hn,MKB4`t%!oT^Kg<!> cjfkZ&^.%+QcD3Av[G < TDO;![kngz}1'<";
        public static string InitScript =>
        @"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[___OnlineRegs]') AND type in (N'U')) 
        BEGIN
	        CREATE TABLE [dbo].[___OnlineRegs] (
		        [Id] [int] NOT NULL, 
		        [Name] [varchar](500) NOT NULL,
	            [ShareholdersCount] [int] NULL,
	            [ShareholdersWithUnitCount] [int] NULL,
	            [MaxUnit] [money] NULL,
	            [AverageUnit] [money] NULL,
	            [TotalUnits] [money] NULL
	        ) ON [PRIMARY] 
        END

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[___OnlineSHs]') AND type in (N'U')) 
        BEGIN
	        CREATE TABLE [dbo].[___OnlineSHs] (
		        [Id] [int] NOT NULL, 
		        [ClearingNo] [varchar](50) NOT NULL, 
		        [Name] [varchar](500) NOT NULL, 
		        [HasHoldings] [bit] NOT NULL
	        ) ON [PRIMARY] 
        END

        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[___OnlineHoldings]') AND type in (N'U')) 
        BEGIN
	        CREATE TABLE [dbo].[___OnlineHoldings] (
		        [Id] [int] NOT NULL, 
		        [AccountNo] [int] NOT NULL, 
		        [RegCode] [int] NOT NULL
	        ) ON [PRIMARY] 
        END

        IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[___Shareholders]'))
        EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[___Shareholders]
        AS
        SELECT        dbo.T_shareholder.auto AS Id, dbo.T_shareholder.account_number AS AccountNo, dbo.T_shareholder.register_code AS RegCode, dbo.T_shareholder.haddress AS Address1, dbo.T_shareholder.holder_address2 AS Address2, 
                                 dbo.T_shareholder.clearing_no AS ClearingNo, dbo.T_shareholder.hfirst_name AS FirstName, dbo.T_shareholder.hlast_name AS LastName, dbo.T_shareholder.hmname AS MiddleName
        FROM            dbo.T_shareholder INNER JOIN
                                 dbo.___OnlineRegs ON dbo.T_shareholder.register_code = dbo.___OnlineRegs.Id INNER JOIN
                                 dbo.___OnlineSHs ON dbo.T_shareholder.clearing_no = dbo.___OnlineSHs.ClearingNo' 

        IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[___Shareholdings]'))
        EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[___Shareholdings]
        AS
        SELECT        dbo.T_shareholder.auto AS Id, dbo.T_shareholder.account_number AS AccountNo, dbo.T_shareholder.register_code AS RegCode, dbo.T_shareholder.haddress AS Address1, dbo.T_shareholder.holder_address2 AS Address2, 
                                 dbo.T_shareholder.clearing_no AS ClearingNo, dbo.T_shareholder.hfirst_name AS FirstName, dbo.T_shareholder.hlast_name AS LastName, dbo.T_shareholder.hmname AS MiddleName, 
                                 dbo.___OnlineHoldings.Id AS ShareholderId
        FROM            dbo.T_shareholder INNER JOIN
                                 dbo.___OnlineRegs ON dbo.T_shareholder.register_code = dbo.___OnlineRegs.Id INNER JOIN
                                 dbo.___OnlineHoldings ON dbo.T_shareholder.account_number = dbo.___OnlineHoldings.AccountNo AND dbo.T_shareholder.register_code = dbo.___OnlineHoldings.RegCode' 

        IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[___RDividends]'))
        EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[___RDividends]
        AS
        SELECT        dbo.T_divs_paid.sno AS Id, dbo.T_divs_paid.div_type AS Description, dbo.T_divs_paid.regcode AS RegCode, dbo.T_divs_paid.pyt AS PaymentNo, ISNULL(dbo.T_divs_paid.price, 0) AS AmountDeclared, 
                                 ISNULL(dbo.T_divs_paid.year_end, dbo.T_divs_paid.cutoff_dt) AS YearEnd, dbo.T_divs_paid.payable_dt AS DatePayable, dbo.T_divs_paid.cutoff_dt AS ClosureDate
        FROM            dbo.T_divs_paid INNER JOIN
                                 dbo.___OnlineRegs ON dbo.T_divs_paid.regcode = dbo.___OnlineRegs.Id'

        IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[___RHoldings]'))
        EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[___RHoldings]
        AS
        SELECT        dbo.T_shareholder.auto AS Id, dbo.T_shareholder.register_code AS RegCode, dbo.___OnlineRegs.Name AS Register, dbo.T_shareholder.account_number AS AccountNo, dbo.T_shareholder.clearing_no AS ClearingNo, dbo.T_shareholder.bvn AS BVN, 
                                 dbo.T_shareholder.hfirst_name AS FirstName, dbo.T_shareholder.hlast_name AS LastName, dbo.T_shareholder.hmname AS MiddleName, dbo.T_shareholder.hsex AS Gender, dbo.T_shareholder.phone AS Phone, 
                                 dbo.T_shareholder.mobile AS Mobile, dbo.T_shareholder.mail AS Email, dbo.T_shareholder.haddress AS Address1, dbo.T_shareholder.holder_address2 AS Address2, dbo.T_shareholder.hcity_town AS City, 
                                 dbo.Qry_SumOfUnit.SumOfUnit AS TotalUnits
        FROM            dbo.T_shareholder INNER JOIN
                                 dbo.Qry_SumOfUnit ON dbo.T_shareholder.account_number = dbo.Qry_SumOfUnit.account_no AND dbo.T_shareholder.register_code = dbo.Qry_SumOfUnit.reg_code INNER JOIN
                                 dbo.___OnlineRegs ON dbo.Qry_SumOfUnit.reg_code = dbo.___OnlineRegs.Id'

        IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[___SDividends]'))
        EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[___SDividends]
        AS
        SELECT        auto AS Id, account_no AS AccountNo, divreg_code AS RegCode, divgross_amt AS Gross, total_holding AS Total, div_netamt AS Net, divpay_no AS DividendNo, divtax_amount AS Tax, divwarrant_no AS WarrantNo, 
                                 dividend_type AS Type, REPLACE(CONVERT(NVARCHAR, divdate_payable, 106), '' '', ''-'') AS Date
        FROM            dbo.T_Divs'

        IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[___Units]'))
        EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[___Units]
        AS
        SELECT        auto AS Id, account_no AS AccountNo, reg_code AS RegCode, cert_no AS CertNo, REPLACE(CONVERT(NVARCHAR, date_issue, 106), '' '', ''-'') AS Date, oldcert AS OldCertNo, no_of_units AS TotalUnits, 
                                 transfer_number AS Description, date_issue, transfer_number AS Narration
        FROM            dbo.T_units'

        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 5) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (5, 'AFRICAN PAINTS (NIGERIA) PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 6) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (6, 'ASWANI TEXTILE') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 7) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (7, 'BCN PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 8) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (8, 'CFAO NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 9) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (9, 'OANDO PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 10) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (10, 'TAYLOR WOODROW NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 11) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (11, 'COSTAIN WEST AFRICA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 12) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (12, 'LEARN AFRICA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 13) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (13, 'FAMAD NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 14) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (14, 'JULI PHARMACY NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 15) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (15, 'JULI PHARMACY NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 16) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (16, 'INTRA-MOTORS NIGERIA PLS') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 31) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (31, 'PENIEL MICROFINANCE BANK LTD FORMERLY (ORE - OFE COMMUNITY BANK LTD)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 46) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (46, 'FIDELITY BANK PLC (ACCUMULATION)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 47) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (47, 'FIDELITY BANK PLC (INCOME)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 54) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (54, 'STANDARD ALLIANCE INSURANCE PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 59) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (59, 'AKWA IBOM STATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 62) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (62, 'PZ CUSSONS NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 63) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (63, 'PRESCO PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 64) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (64, 'PZ (DEBENTURE STOCK) PETERSON ZOCONIS INDUSTRIES PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 80) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (80, 'REDEEMED GLOBAL MEDIA CO. LTD') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 82) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (82, 'STANBIC IBTC BANK PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 83) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (83, 'BANK PHB PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 87) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (87, 'FIDELITY BANK PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 110) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (110, 'FRIESLANDCAMPINA WAMCO NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 112) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (112, 'JAIZ INTERNATIONAL PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 117) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (117, 'ABC TRANSPORT PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 119) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (119, 'STANDARD ALLIANCE INSURANCE(DEBENTURE)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 120) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (120, 'ACAP CANARY GROWTH FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 122) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (122, 'EZEEKLICK SYSTEMS LTD') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 126) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (126, 'DEAP CAPITAL MANAGEMENT & TRUST PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 127) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (127, 'DEAP CAPITAL MANAGEMENT & TRUST PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 128) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (128, 'KAKAWA GUARANTEED INCOME FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 138) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (138, 'ASSET & RESOURCE MANAGEMENT COMPANY LTD') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 139) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (139, 'STANBIC IBTC NIGERIAN EQUITY FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 141) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (141, 'STANBIC IBTC ETHICAL FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 147) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (147, 'RAK UNITY PETROLEUM') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 149) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (149, 'ANCHOR FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 150) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (150, 'BEDROCK FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 151) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (151, 'STACO INSURANCE PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 154) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (154, 'STANBIC IBTC GUARANTEED INVESTMENT FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 157) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (157, 'CHAMS PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 158) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (158, 'ARM AGGRESSIVE GROWTH FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 159) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (159, 'FBN HERITAGE FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 166) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (166, 'NIGERIAN BREWERIES PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 167) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (167, 'PRESTIGE ASSURANCE PLC_2') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 168) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (168, 'UNION DIAGNOSTIC AND CLINICAL SERVICES LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 172) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (172, 'ASO-SAVINGS AND LOANS PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 177) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (177, 'STARCOMMS PLC (PRIVATE PLACEMENT 2008)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 179) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (179, 'PARTNERSHIP INVESTMENT CO. LTD') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 180) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (180, 'LIGHTHOUSE FINANCIAL SERVICES LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 182) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (182, 'JOINT KOMPUTER KOMPANY LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 195) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (195, 'FUMMAN AGRICULTURAL PRODUCTS INDUSTRIES LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 201) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (201, 'LAGOS STATE GOVT. BOND - SERIES 1') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 203) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (203, 'DAAR COMMUNICATIONS PLC(COMBINED REGISTER)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 205) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (205, 'KWARA STATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 208) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (208, 'STANBIC IBTC MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 209) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (209, 'STANBIC IBTC BOND FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 211) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (211, 'PORTFOLIO / INVESTMENT MANAGERS') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 212) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (212, 'GTBANK N200 BILLION DEBT ISSUANCE PROGRAMME') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 213) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (213, 'CHAMSACCESS LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 214) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (214, 'CHAMSSWITCH LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 220) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (220, 'VALUALLIANCE VALUE FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 221) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (221, 'UACN PROPERTY DEVELOPMENT CO. PLC (UPDC) - SERIES 1') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 226) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (226, 'EDO STATE GOVERNMENT BOND-N25 BILLION 14% INFRASTRUCTURAL DEV. BOND DUE 2017') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 230) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (230, 'WEST AFRICAN ALUMINIUM PRODUCTS PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 236) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (236, 'DELTA STATE GOVT. BOND (SERIES 1)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 238) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (238, 'UNITED BANK FOR AFRICA PLC - N35B SUBORDIANTED FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 239) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (239, 'AUSTIN LAZ AND COMPANY PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 244) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (244, 'ARM ETHICAL FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 249) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (249, 'SUNTRUST REAL ESTATE INVESTMENT TRUST SCHEME (REITS)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 251) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (251, 'FORTIS MICROFINANCE BANK PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 252) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (252, 'OASIS INSURANCE PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 253) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (253, 'STANBIC IBTC BALANCED FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 257) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (257, 'FBN MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 259) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (259, 'FBN FIXED INCOME FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 262) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (262, 'STANBIC IBTC HOLDINGS PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 266) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (266, 'THE INFRASTRUCTURE BANK PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 268) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (268, 'INTERCONTINENTAL INTEGRITY FUND (IIF)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 269) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (269, 'NIGERIA POLICE MORTGAGE BANK PLC (FORMERLY FOKAS SAVINGS AND LOANS LTD)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 271) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (271, 'UPDC REAL ESTATE INVESTMENT TRUST') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 277) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (277, 'SUNTRUST SAVINGS & LOANS LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 280) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (280, 'CADBURY NIGERIA PLC (RECONSTRUCTED SHARES)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 281) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (281, 'STANBIC IBTC IMAN FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 284) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (284, 'MTECH COMMUNICATIONS PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 286) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (286, 'DV BALANCED FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 289) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (289, 'STANBIC IBTC ETF 30 FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 290) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (290, 'STANBIC IBTC BANK PLC N100,000,000 SERIES 1 (TRANCHE A)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 291) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (291, 'STANBIC IBTC BANK PLC N15,440,000,000 13.25% SERIES 1 (TRANCHE B)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 300) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (300, 'ELEME PETROCHEMICALS COMPANY COOPERATIVE INVESTMENT & CREDIT SOCIETY LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 302) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (302, 'CROSS RIVERS STATE GOVT.BONS-N8 BILLION DEV BOND DUE 2022(SERIES 1)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 303) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (303, 'ZAMFARA STATE GOVT. BOND-30 BILLION DEV BOND DUE 2022(SERIES 1)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 312) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (312, 'VETIVA S& P NIGERIAN SOVEREIGN BOND ETF') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 316) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (316, 'SIAML PENSION ETF 40') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 317) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (317, 'STANBIC IBTC DOLLAR FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 318) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (318, 'CR SERVICES (CREDIT BUREAU) PLC (ORDINARY)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 321) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (321, 'INDUSTRIAL & MEDICAL GASES NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 322) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (322, 'LOTUS HALAL EQUITY EXCHANGE TRADED FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 323) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (323, 'DUFIL PRIMA FOODS PLC - N10 BILLION 18.25% FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 324) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (324, 'LAGOS STATE GOVT. BOND - SERIES 2 TRANCHE 1 N46370000000 16.75% 2017/2024 FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 325) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (325, 'LAGOS STATE GOVT. BOND - SERIES 2 TRANCHE 2 N38770000000 17.25% 2017/2027 FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 327) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (327, 'MRS OIL NIGERIA PLC') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 328) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (328, 'AFRINVEST PLUTUS FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 329) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (329, 'LAGOS STATE GOVT. BOND - SERIES 2 TRANCHE 3 N6911000000 15.60% 2018/2024 FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 330) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (330, 'LAGOS STATE GOVT. BOND - SERIES 2 TRANCHE 4 N5336000000 15.85% 2018/2027 FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 331) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (331, 'ABACUS MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 334) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (334, 'CORDROS MILESTONE FUND 2023') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 335) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (335, 'CORDROS MILESTONE FUND 2028') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 336) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (336, 'GDL MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 337) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (337, 'UNION BANK OF NIGERIA PLC -DEBT ISSUANCE SERIES 1 N7190214000 15.50% DUE 2021 FIXED RATE BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 338) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (338, 'UNION BANK OF NIGERIA PLC -DEBT ISSUANCE SERIES 11 N6314000000 15.75% DUE 2025 FIXED RATE') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 340) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (340, 'SKYWAY AVIATION HANDLING COMPANY PLC (SAHCO)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 341) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (341, 'STANBIC IBTC BANK PLC STRUCTURED NOTE PROGRAMME N30,000,000,000 15.75% SERIES 1 DUE 2023') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 342) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (342, 'PRINCESS REAL ESTATE INVESTMENT TRUST (FUND)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 343) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (343, 'UNION BANK OF NIGERIA PLC -DEBT ISSUANCE SERIES 3 N30000000000 16.20% DUE 2029 FIXED RATE') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 344) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (344, 'STANBIC IBTC SHARIAH FIXED INCOME FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 345) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (345, 'VETIVA MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 346) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (346, 'WESLEY UNIVERSITY INVESTMENT COMPANY LIMITED') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 347) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (347, 'CORDROS DOLLAR INCOME FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 349) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (349, 'LAGOS STATE GOVT SERIES III N100 BILLION 12.25% 2020/2030 BOND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 352) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (352, 'FBNQ MB FUNDING SPV PLC N5 BILLION 10.5% FIXED RATE DUE 2023 SERIES 1') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 355) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (355, 'VALUALLIANCE MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 360) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (360, 'CORE INVESTMENT MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 361) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (361, 'CORE VALUE MIXED FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 362) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (362, 'FIDELITY BANK N41213000000 8.5% FIXED RATE UNSECURED SUBORDINATED BOND DUE 2031') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 363) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (363, 'CORDROS MILESTONE BALANCED FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 364) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (364, 'STANBIC IBTC ENHANCED SHORT-TERM FIXED INCOME FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 365) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (365, 'EMERGING AFRICA MONEY MARKET FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 366) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (366, 'EMERGING AFRICA BOND FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 411) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (411, 'EMERGING AFRICA BALANCED DIVERSITY FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 412) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (412, 'EMERGING AFRICA EUROBOND FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 413) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (413, 'FBNQ MB FUNDING SPV PLC N8 BILLION 6.25% FIXED RATE DUE 2030 SERIES II') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 414) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (414, 'FSDH FUNDING SPV PLC SERIES 1 TRANCHE A(N7,050,000,000 8.50% FIXED RATE SENIOR UNSECURED BOND DUE 2') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 415) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (415, 'FSDH FUNDING SPV PLC SERIES 1 TRANCHE B (N4,950,000,000 8% FIXED RATE BOND DUE 2026)') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 416) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (416, 'AVA GAM FIXED INCOME FUND') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 417) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (417, 'LEKKI GARDENS ESTATE LIMITED N10 BILLION 10% FIXED RATE SENIOR SECURED BOND DUE 2024') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 418) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (418, 'LAGOS STATE GOVT SERIES IV N137328000000 BILLION BOND DUE 2031') END
        IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = 420) BEGIN INSERT INTO ___OnlineRegs (Id, Name) VALUES (420, 'PRESCO PLC N34,500,000,000 7 YEAR 12.85% SENIOR UNSECURED FIXED RATE') END

        IF NOT EXISTS (SELECT 1 FROM ___OnlineRegs WHERE ShareholdersCount IS NOT NULL)
        BEGIN
            UPDATE ___OnlineRegs SET
                ShareholdersCount = (SELECT COUNT(*) FROM T_shold WITH (NOLOCK) WHERE regcode = ___OnlineRegs.Id),
                ShareholdersWithUnitCount = (SELECT COUNT(*) FROM ___RHoldings WITH (NOLOCK) WHERE RegCode = ___OnlineRegs.Id AND TotalUnits > 0),
                MaxUnit = (SELECT MAX(TotalUnits) FROM ___RHoldings WITH (NOLOCK) WHERE RegCode = ___OnlineRegs.Id),
                AverageUnit = (SELECT AVG(TotalUnits) FROM ___RHoldings WITH (NOLOCK) WHERE RegCode = ___OnlineRegs.Id),
                TotalUnits = (SELECT SUM(TotalUnits) FROM ___RHoldings WITH (NOLOCK) WHERE RegCode = ___OnlineRegs.Id)
        END
        ";
        public static string ClearUnwantedEstockTablesScript =>
        @"
        BEGIN TRANSACTION
        SET QUOTED_IDENTIFIER ON
        SET ARITHABORT ON
        SET NUMERIC_ROUNDABORT OFF
        SET CONCAT_NULL_YIELDS_NULL ON
        SET ANSI_NULLS ON
        SET ANSI_PADDING ON
        SET ANSI_WARNINGS ON
        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.zarch_agnt_Brn

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.yes

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.xxx

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.withholdingtax

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.verifndatabs

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.UNITS2

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.UNIT4

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Transfer_correction

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.[TRADING REPORT]

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.TokenUsers

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.TokenLogs

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.testing

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Tbl_Member

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Table1

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_wmessage_center

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_wclntacct

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_wclint

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_warr_others

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_warr_misc

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_wadmins

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_verif_hist

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_update

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_units_max

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_UNITS_2_banke

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_unclaimed_dividend

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_transferor

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_transferee

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_stop

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_stockv

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_state

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_slit_mast

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_slit_certificate

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_slit_cert_source

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.t_slit_cert

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_signature

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_holder_type]
  
        AS
        SELECT description as typedesc,
        hold_type as holdertype,
        who as enteredby


        From dbo.T_shold_type

        DROP TABLE dbo.T_shold_type

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_shold_add

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.t_shartabs

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_sh_cap

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_servdtdt

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_servdt

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_sec_type]
  
        AS
        SELECT description as secu_desc,
        sec_sector as secu_type,
        who as enteredby



        From dbo.T_secu_type

        DROP TABLE dbo.T_secu_type

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_bonuses]
  
        AS
        SELECT acct_frac as fractact,
        acqdtpd as actdatepd,
        allot_dt as allot_date,
        bonus_seq as bonus_number,
        chg_dt as changedat,
        chg_who as changedby,
        close_dt as closure_date,
        enterd_dt as createdat,
        paid as paynow,
        ratio1 as bonus_ratio1,
        ratio2 as bonus_ratio2,
        Regcode as reg_code,
        who as enteredby


        From dbo.T_script

        DROP TABLE dbo.T_script

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_revalid

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_return_money

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_rept_sig

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_reissue_dept

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_reissue

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_register

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_company]
  
        AS
        SELECT     auth_sh AS auth_share, chg_dt AS changedat, chg_who AS changedby, coy_addr AS prc_address, coy_email AS prc_email, coy_fax AS prc_fax, 
                              coy_name AS prc_name, coy_no AS prc_no, coy_phone AS prc_phone, coy_type AS prc_type, enterd_dt AS createdat, incop_dt AS date_incorporate, 
                              list_dt AS date_listed, rc_num AS rc_number, who AS enteredby, website
        FROM         dbo.T_reg_name

        DROP TABLE dbo.T_reg_name

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_reg_category

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_regist]
  
        AS
        SELECT     chg_dt AS changedat, chg_who AS changedby, coy_no AS prc_no, date_tak AS takeupdt, description AS register_desc, enterd_dt AS createdat, 
                              nominal AS nomvalue, regcode AS register_code, sec_type AS security_type, shares AS actual_shares, who AS enteredby, caution, active, symbol, 
                              fraction, fund_cert_narr
        FROM         dbo.T_reg

        DROP TABLE dbo.T_reg

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_redeem_certs

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_REDEEM_WARRANT]
  
        AS
        SELECT     acctno AS account_number, chg_dt AS changedat, chg_who AS changedby, description AS narration, enterd_dt AS createdat, holding AS red_holding, 
                              instal AS red_instalment, net AS red_amount, paid_dt AS date_paid, payable_dt AS redate_payable, regcode AS register_code, reissue AS red_reissue, 
                              reissue_dt AS redate_reissue, serial AS red_sno, solid_acct AS sourceact, status AS red_status, storage_no AS storageno, unclaim AS red_unclaim, 
                              verif AS red_verify, verif_dt AS redverify_date, warr_code AS redwarrant_code, who AS enteredby, consolid_id, claimed, claimedate, claimedby, batch, 
                              Audit_check, auto, annotation, fcle AS finacle
        FROM         dbo.T_red_warr

        DROP TABLE dbo.T_red_warr

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_REDEMPTION]
  
        AS
        SELECT acctno as red_account,
        acqdtpd as actdatepd,
        chg_dt as changedat,
        chg_who as changedby,
        enterd_dt as createdat,
        paid as paynow,
        payable_dt as redate_payable,
        rate as redemption_rate,
        rate_p as penalty_rate,
        redeemdt as redem_date,
        regcode as register_code,
        serial as installment_serial_code,
        sno_red as redeem_sno,
        total_amt as red_totamt,
        who as enteredby


        From dbo.T_red

        DROP TABLE dbo.T_red

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_range_auth

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_process_right_hist

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_probate_change_auth

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_probate_arch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_probate

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_price

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_picture_signtory

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_picture

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_online

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_occupation

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_Name_change_auth

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_mstf1_nm

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_mstf1_md

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_mstf1_ad

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_mstf

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_move_lg

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_move_gp

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_move_ex

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_move

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_monitor

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_message_center_arch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_message_center

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_manual_div_setup

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_manual_div

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_manual_cert_mast

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_manual_cert_details

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_mandate_change_auth

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_mailer_conf

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_logger1

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_logger_type

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_local_govt

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_lga

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_int_warr_old

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_INTEREST_WARRANT]
  
        AS
        SELECT     acctno AS account_number, chg_dt AS changedat, chg_who AS changedby, description AS narration, enterd_dt AS createdat, gross AS intgross_amt, 
                              holding AS int_holding, net AS intnet_amt, oldint_warr AS intoldno, paid_dt AS date_paid, payable_dt AS intdate_payable, pyt AS intpay_no, regcode AS register_code, 
                              reissue AS int_reissue, reissue_dt AS intdate_reissue, solid_acct AS sourceact, status AS intstatus, storage AS storageno, tax AS intax_amt, 
                              unclaim AS int_unclaimed, verif AS intverify, verif_dt AS intverify_date, warr_code AS intwar_code, who AS enteredby, consolid_id, claimed, claimedate, claimedby, 
                              batch, audit_check, audit_check_dt, auto, annotation, fcle AS finacle
        FROM         dbo.T_int_warr

        DROP TABLE dbo.t_int_warr

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_INTEREST_PAY]
  
        AS
        SELECT acqdtpd as actdatepd,
        chg_dt as changedat,
        crat_dt as createdat,
        chg_who as changedby,
        cut_off as int_cutoff,
        inst_cd as instalmnt_code,
        int_acct as int_account,
        int_rate as interest_rate,
        int_serial as int_sno,
        int_tx_rt as intaxrate,
        paid as paynow,
        pay_dt as intdate_payable,
        regcode as reg_code,
        totamount as int_totamt,
        who as enteredby,
        yr_freq as yr_frequency


        From dbo.T_int_pay

        DROP TABLE dbo.T_int_pay

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_INSTALLMENT

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_imp_batch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_getUnits

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_getStocks

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_fed_constituency

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_expu

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_exp_date

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_exar

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_ex

        COMMIT
        BEGIN TRANSACTION

        ALTER VIEW [dbo].[T_dividends_type]
  
        AS
        SELECT description as div_type_desc,
        div_type as dividend_type,
        who as enteredby


        From dbo.T_divs_type

        DROP TABLE dbo.T_divs_type

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_dividend_warrant_group

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.t_div_reprint

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_dept

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_days

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_curr

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscslien

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscs_trans_notupdated

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscs_trans

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscs_mast

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscs_disk_G

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscs_CERTIFICATE_temp

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cscs_cert

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cross

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_country

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_countByPhone

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_countByEmail

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.t_corresp_dept

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_corresp_aknowdg

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_corresp

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_control

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_consolid_sub

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_consolid_mast

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_company_contacts

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_claimed_dividend

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_certificate_split

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_certificate_slit

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_certificate_group

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cert_status

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cert_mast

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_cert_detail

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_caut_add

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_canx

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_brokers

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_brokerage

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.t_branch_oper_hd

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_branch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_batch_oldcert

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_agerange

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_agent_type

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_agent_Branch_arch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_agent_Branch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_agent_arch

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_agent

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_address_archive

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_addr_change_auth

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_additional

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.T_account

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.subscriptionDoc

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.stateinNigeria

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.StanbicScrip_ViewTest

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Stanbic_Archive

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Sheet1$

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.shareholdertable

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Results

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.range_cutoff

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.['POLICE NOMINAL ROLL$']

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.['POLICE']

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.phonenumbers

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.outlier2

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.otpLogs

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.newTUNITS

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.newImageTable

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.newdelta

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.new_TUNITSSSS

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.new_companylist

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.nbrecords

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.nbrecord

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.nb_non_closure_update

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Frismob

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.[FOKAS SHAREHOLDERS]

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.[FIDELITY REGISTER]

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.[FIDELITY DIV 15 APPEND]

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.FBN_LINK_TABLE_AFTER_SEARCH_OUTSTANDING_CERT_WITHOUT_E

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.FBN_LINK_E_Only

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.FBN_LINK_Certificate_Only

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.FBN_LINK_Both_Cert_and_E

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.etf30_20170427

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Edas_MassV_DataA

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.duplicated_unit_transfer_nos

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.deltared

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.deltainterest

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc_no_recordsin

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc_289

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc_281

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc_209

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc_154

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc_139

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_subscriptionDoc

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_bankstatment

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.dbo_api_divclientcompany$

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.complainCopy

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_OTI_20230201012542PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_OTI_20230201012511PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_julian_20220926121908PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_julian_20220909024446PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_JULIAN_20201118024733PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_JULIAN_20200528100935AM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_imaobong_20200430075115PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_imaobong_20200429015824PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_hannah_20221012014147PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_hannah_20221012014131PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_hannah_20221012014130PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_daramola_20230201033638PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_daramola_20230201015344PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_DARAMOLA_20230201013807PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_DARAMOLA_20221114024756PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_DARAMOLA_20220926110839AM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_daramola_20220620094054AM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_DARAMOLA_20220620030006PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_DARAMOLA_20210610113736AM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Cert_BIDEMI_20210304020246PM

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.['BROAD STREET$']

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.Banksortcodes

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.banksforeadvice

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.arm_accts_with_image$

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.api_divclientcompany

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.api_divclien_old

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.allFbnWithEmailPhone

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.ActiveRegistersFiltered

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.a1

        COMMIT
        BEGIN TRANSACTION

        DROP TABLE dbo.a

        COMMIT
        ";
    }
}