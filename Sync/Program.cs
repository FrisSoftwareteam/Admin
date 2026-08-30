using Dapper;
using FirstReg.Bson;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirstReg.Sync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"Sync started at {DateTime.Now}");

            try
            {
                var tool = new SyncTool();

                await tool.DownloadCloudData();

                await tool.SyncRegisterDividends();
                await tool.SyncShareholders();
                await tool.SyncRegisterShareholders();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine($"Sync completed at {DateTime.Now}");
        }
    }

    public class SyncTool
    {
        readonly Connection _conn;
        readonly int _retentionPeriod = 3;
        readonly string _apikey = "b1p4jZwrDIWAqOtQaRbzJHdvUGIBo5qa";
        readonly Clear.IApiClient _client;

        readonly List<string> _logs;
        readonly string _connDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fr");
        readonly string _logDir = @"c:\frsync_logs\";
        readonly string _logFile = $"log-{DateTime.Now:yyMMddHHmmssffff}";

        readonly Dictionary<string, string> _dico;
        //readonly int _batchSize = 50000;

        public SyncTool()
        {
            try
            {
                _logs = new List<string>();

                LogLine("Initializing sync tools...");
                LogLine("");

                if (!Directory.Exists(_logDir))
                {
                    LogLine("Creating sync log folder...");
                    Directory.CreateDirectory(_logDir);
                }

                if (!Directory.Exists(_connDir))
                {
                    LogLine("Creating sync log folder...");
                    Directory.CreateDirectory(_connDir);
                }

                LogLine("Getting connection parameters...");

                var file = new FileInfo(Path.Combine(_connDir, "_app.json"));

                if (file.Exists)
                {
                    var json = Clear.Tools.FileManager.ReadFile(file.FullName);
                    _conn = JsonSerializer.Deserialize<Connection>(json);
                }
                else
                {
                    LogLine(
                        $"Connection file is missing, copy the connection file to {file.FullName} " +
                        $"or hold the Ctrl key at start-up to set a new file");
                    LogLine("");

                    LogLine("Enter or paste the api url below...");
                    var apiurl = Console.ReadLine();
                    LogLine("");

                    LogLine("Enter or paste the connection to local db below...");
                    var dnconn = Console.ReadLine();
                    LogLine("");

                    _conn = new Connection(dnconn, apiurl);

                    LogLine("Writing connection details to file...");

                    Clear.Tools.FileManager.WriteToFile(file.FullName, JsonSerializer.Serialize(_conn));
                }

                LogLine("Configuring http headers...");
                LogLine("");

                _dico = new() { { "key", Tools.APIkey } };

                LogLine("Initializing http control...");
                LogLine("");

                _client = new Clear.ApiClient(new HttpClient());

                LogLine("Clearing stale logs...");

                ClearStaleLog();

                LogLine("Sync tools initialization completed!");
                LogLine("");

                LogLine("==========================================================");
                LogLine("==========================================================");
                LogLine("==========================================================");

                LogLine("");

                SaveLog();
            }
            catch (Exception ex)
            {
                LogLine($"Error occured while initializing sync tools:\n{ex.Message}.");
                LogLine("");

                SaveLog();

                Environment.Exit(0);
            }
        }

        public async Task DownloadCloudData()
        {
            try
            {
                LogLine("Preparing to download required cloud data...");
                LogLine("");

                LogLine("Connecting to the local db...");
                LogLine("");

                using SqlConnection conn = new(_conn.LocalString);

                LogLine("Creating db objects in local db...");

                await conn.ExecuteAsync(InitScript);

                try
                {
                    LogLine("Checking local shareholders count...");

                    var count = conn.Query<int>($"SELECT COUNT(*) FROM ___OnlineSHs",
                        commandTimeout: 3600).First();

                    if (count <= 0)
                        await _client.PutAsync(_conn.GetShareholdersResetURL(), 0, "", _dico, false);

                    LogLine("Fetching shareholders list from the cloud...");

                    var shareholders = await _client.GetAsync<List<Shareholder>>(_conn.GetShareholdersURL(), "", _dico);

                    if (shareholders.Count > 0)
                    {
                        LogLine("Updating shareholders list in local db...");

                        await conn.ExecuteAsync(string.Join("\n", shareholders.Select(x => 
                        $"IF NOT EXISTS(SELECT * FROM ___OnlineSHs WHERE Id = {x.Id}) " +
                        $"BEGIN INSERT INTO ___OnlineSHs (Id, ClearingNo, Name, HasHoldings) " +
                        $"VALUES ({x.Id}, '{Clear.Tools.StringUtility.SQLSerialize(x.ClearingNo)}', " +
                        $"'{Clear.Tools.StringUtility.SQLSerialize(x.FullName)}', " +
                        $"{Clear.Tools.StringUtility.SQLSerialize(x.Holdings.Any())}) " +
                        $"END")));

                        if (shareholders.Any(x => x.Holdings.Count > 0))
                        {
                            LogLine("Updating shareholdings list in local db...");

                            await conn.ExecuteAsync(string.Join("\n", shareholders.SelectMany(x => x.Holdings).Select(x =>
                            $"IF NOT EXISTS(SELECT * FROM ___OnlineHoldings WHERE AccountNo = {x.AccountNo} AND RegCode = {x.RegCode}) " +
                            $"BEGIN INSERT INTO ___OnlineHoldings (Id, AccountNo, RegCode) " +
                            $"VALUES ({x.Id}, {x.AccountNo}, {x.RegCode}) " +
                            $"END")));
                        }
                    }

                    shareholders = new();
                }
                catch (Exception sh_ex)
                {
                    LogLine($"Error occured while downloading shareholders list from the cloud:\n{sh_ex.Message}.");
                    LogLine("");
                }

                try
                {
                    LogLine("Fetching registers list from the cloud...");

                    var registers = await _client.GetAsync<List<Register>>(_conn.GetRegistersURL(), "", _dico);

                    if (registers.Count > 0)
                    {
                        LogLine("Updating registers list in local db...");

                        await conn.ExecuteAsync(string.Join("\n", registers.Select(x => 
                        $"IF NOT EXISTS(SELECT * FROM ___OnlineRegs WHERE Id = {x.Id}) " +
                        $"BEGIN INSERT INTO ___OnlineRegs (Id, Name) " +
                        $"VALUES ({x.Id}, '{Clear.Tools.StringUtility.SQLSerialize(x.Name)}') END")));
                    }

                    registers = new();
                }
                catch (Exception reg_ex)
                {
                    LogLine($"Error occured while downloading shareholders list from the cloud:\n{reg_ex.Message}.");
                    LogLine("");
                }

                LogLine("");
                LogLine("==========================================================");
                LogLine("");

                SaveLog();
            }
            catch (Exception ex)
            {
                LogLine($"Error occured while running sync:\n{ex.Message}.");
                LogLine("");
            }

            SaveLog();
        }

        public async Task SyncShareholders()
        {
            try
            {
                LogLine("Preparing to sync records of verified shareholders...");
                LogLine("");

                LogLine("Fetching last id from the cloud...");

                var lastid = await _client.GetAsync<int>(_conn.GetShareholdersLastIdURL(), "", _dico);

                LogLine("Connecting to the local db...");
                LogLine("");

                using SqlConnection conn = new(_conn.LocalString);

                LogLine("Fetching shareholders list...");
                LogLine("");

                var shareholders = conn.Query<Shareholder>($"SELECT TOP ({_conn.SHBatch}) * FROM ___OnlineSHs WHERE Id > {lastid} ORDER BY Id",
                    commandTimeout: 3600).ToList();

                LogLine("==========================================================");
                LogLine("");

                if (shareholders.Count > 0)
                {

                foreach (var sharehlder in shareholders)
                {
                    try
                    {
                        LogLine($"Fetching accounts for - {sharehlder.FullName} ({sharehlder.ClearingNo})...");
                        LogLine("");

                        var holdings = sharehlder.HasHoldings
                                ? conn.Query<Holding>($"SELECT * FROM ___Shareholdings WHERE ShareholderId = {sharehlder.Id}", commandTimeout: 3600).ToList()
                                : conn.Query<Holding>($"SELECT * FROM ___Shareholders WHERE ClearingNo = '{sharehlder.ClearingNo}'", commandTimeout: 3600).ToList();

                        foreach (var holding in holdings)
                        {
                            try
                            {
                                LogLine($"Fetching transactions from Account: {holding.AccountNo} in Register: {holding.RegCode}...");

                                holding.Units.AddRange(conn.Query<Bson.Unit>(
                                    $"SELECT * FROM ___Units WHERE RegCode = {holding.RegCode} AND AccountNo = {holding.AccountNo}",
                                    commandTimeout: 3600));

                                LogLine($"Fetching dividends from Account: {holding.AccountNo} in Register: {holding.RegCode}...");

                                holding.Dividends.AddRange(conn.Query<Bson.Dividend>(
                                    $"SELECT * FROM ___SDividends WHERE (RegCode = {holding.RegCode}) AND (AccountNo = {holding.AccountNo})",
                                    commandTimeout: 3600));

                                LogLine($"Updating {holding.GetFullName} records...");

                                sharehlder.Holdings.Add(holding);

                                LogLine("");
                            }
                            catch (Exception exh)
                            {
                                LogLine($"Error occured while processing {holding.GetFullName} ({holding.AccountNo}) in register {holding.RegCode}:\n{exh.Message}.");
                                LogLine("");
                            }
                        }

                        LogLine($"Uploading {sharehlder.FullName} records to the cloud...");

                        await _client.PostAsync(_conn.GetShareholdersURL(), sharehlder, "", _dico, false);

                        LogLine($"{sharehlder.FullName} records were successfully uploaded");
                        LogLine("");
                        LogLine("==========================================================");
                        LogLine("");

                        SaveLog();
                    }
                    catch (Exception exsh)
                    {
                        LogLine($"Error occured while processing {sharehlder.FullName}:\n{exsh.Message}.");
                        LogLine("");
                    }
                }

                }
                else
                {
                    LogLine($"All batches have been uploaded, restarting batching...");
                    lastid = 0;
                }

                SaveLog();
            }
            catch (Exception ex)
            {
                LogLine($"Error occured while running sync:\n{ex.Message}.");
                LogLine("");
            }

            SaveLog();
        }

        public async Task SyncRegisterDividends()
        {
            try
            {
                LogLine("Preparing to sync dividend history for all registers...");
                LogLine("");

                LogLine("Connecting to the local db...");
                LogLine("");

                using SqlConnection conn = new(_conn.LocalString);

                LogLine("==========================================================");
                LogLine("");
                LogLine("Fetching all registers dividend records...");
                LogLine("");

                var holdings = conn.Query<RegDividend>(
                    "SELECT * FROM ___RDividends", commandTimeout: 3600).ToList();

                LogLine("Uploading records to the cloud...");

                await _client.PostAsync(_conn.GetDividendsURL(), holdings, "", _dico, false);

                LogLine("Dividend records were successfully uploaded");
                LogLine("");
                LogLine("==========================================================");
                LogLine("");

                SaveLog();
            }
            catch (Exception ex)
            {
                LogLine($"Error occured while running sync:\n{ex.Message}.");
                LogLine("");
            }

            SaveLog();
        }

        public async Task SyncRegisterShareholders()
        {
            try
            {
                LogLine("Preparing to sync shareholder for all registers...");
                LogLine("");

                LogLine("Fetching last id from the cloud...");

                var lastid = await _client.GetAsync<int>(_conn.GetRegShareholdersLastIdURL(), "", _dico);

                LogLine("Connecting to the local db...");
                LogLine("");

                using SqlConnection conn = new(_conn.LocalString);

                LogLine("");

                try
                {
                    LogLine($"Uploading records from ({lastid}), creating batches...");
                    LogLine("");

                    int count = 0;

                    for (int i = 1; i <= 10; i++)
                    {
                        LogLine($"Fetching batch {i} of the shareholder's list...");

                        var holdings = conn.Query<RegSH>(
                            $"SELECT TOP ({_conn.BatchSize}) * FROM ___RHoldings WHERE Id > {lastid} ORDER BY Id",
                            commandTimeout: 3600).ToList();

                        // max: 32196264

                        if (holdings.Count > 0)
                        {
                            count += holdings.Count;

                            LogLine($"Uploading batch ({i}) to the cloud...");

                            await _client.PostAsync(_conn.GetRegShareholdersURL(), holdings.Select(x => x.ArrayString), "", _dico, false);

                            LogLine($"Registering batch ({i}) position...");

                            lastid = holdings.OrderByDescending(x => x.Id).First().Id;
                        }
                        else
                        {
                            LogLine($"All batches have been uploaded, restarting batching...");
                            lastid = 0;
                        }

                        LogLine($"Updating batch position in the cloud...");
                        await _client.PutAsync($"{_conn.GetRegShareholdersLastIdURL()}/{lastid}", 0, "", _dico, false);

                        LogLine("");
                        SaveLog();
                    }

                    LogLine($"{count} records were successfully uploaded");
                    LogLine("");
                    LogLine("==========================================================");
                    LogLine("");

                    SaveLog();
                }
                catch (Exception exsh)
                {
                    LogLine($"Error occured while processing register shareholders:\n{exsh.Message}.");
                    LogLine("");
                }

                SaveLog();
            }
            catch (Exception ex)
            {
                LogLine($"Error occured while running sync:\n{ex.Message}.");
                LogLine("");
            }

            SaveLog();
        }

        #region helper - could be a class but no time

        private string InitScript =>
        @"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[___OnlineRegs]') AND type in (N'U')) 
        BEGIN
	        CREATE TABLE [dbo].[___OnlineRegs] (
		        [Id] [int] NOT NULL, 
		        [Name] [varchar](500) NOT NULL
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
        SELECT        dbo.T_shareholder.auto AS Id, dbo.T_shareholder.register_code AS RegCode, dbo.T_shareholder.account_number AS AccountNo, dbo.T_shareholder.clearing_no AS ClearingNo, dbo.T_shareholder.bvn AS BVN, 
                                 dbo.T_shareholder.hfirst_name AS FirstName, dbo.T_shareholder.hlast_name AS LastName, dbo.T_shareholder.hmname AS MiddleName, dbo.T_shareholder.hsex AS Gender, dbo.T_shareholder.phone AS Phone, 
                                 dbo.T_shareholder.mobile AS Mobile, dbo.T_shareholder.mail AS Email, dbo.T_shareholder.haddress AS Address1, dbo.T_shareholder.holder_address2 AS Address2, dbo.T_shareholder.hcity_town AS City, 
                                 dbo.Qry_SumOfUnit.SumOfUnit AS Units
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
                                 transfer_number AS Description
        FROM            dbo.T_units'

        ";

        public void LogLine(string log)
        {
            log = string.IsNullOrEmpty(log) ? log : $"[{DateTime.Now}] {log}";
            _logs.Add(log);
            Console.WriteLine(log);
        }

        public void SaveLog()
        {
            try
            {
                if (!Directory.Exists(_logDir))
                    Directory.CreateDirectory(_logDir);

                // create the the stream reader
                using StreamWriter sr = new(Path.Combine(_logDir, $"{_logFile}.txt"));
                sr.WriteLine(string.Join(Environment.NewLine, _logs));
            }
            catch { }
        }

        public void ClearStaleLog()
        {
            try
            {
                Directory.GetFiles(_logDir)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * _retentionPeriod))
                    .ToList()
                    .ForEach(f => f.Delete());
            }
            catch { }
        }

        public async Task<TEntity> FetchAsync<TEntity>(string requestUrl) =>
            await _client.GetAsync<TEntity>(requestUrl, _apikey);

        public async Task<string> PostAsync<TEntity>(string requestUrl, TEntity model) =>
            await _client.PostAsync(requestUrl, model, _apikey);

        #endregion
    }

    public record Connection(string LocalString, string ApiUrl, int BatchSize = 10000, int SHBatch = 1000)
    {
        public string GetShareholdersURL() => $"{ApiUrl}/shareholders";
        public string GetShareholdersLastIdURL() => $"{ApiUrl}/shareholders/lastid";
        public string GetShareholdersResetURL() => $"{ApiUrl}/shareholders/reset";
        public string GetRegistersURL() => $"{ApiUrl}/registers";
        public string GetDividendsURL() => $"{ApiUrl}/dividends";
        public string GetRegShareholdersURL() => $"{ApiUrl}/regshareholders";
        public string GetRegShareholdersLastIdURL() => $"{ApiUrl}/regshareholders/lastid";
    }
}