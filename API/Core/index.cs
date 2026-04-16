using Dapper;
using FirstReg.Bson;
using Microsoft.Data.SqlClient;
using System.Text;

namespace FirstReg.API;

public class DataService
{
    private readonly string _connectString;
    private const int _commandTimeout = 3600;

    public DataService(string connectString)
    {
        _connectString = connectString;
        EnsureObjects().Wait();
    }

    public List<string[]> GetShareholders(int regCode, string global, string name, 
        string addr, string cscs, string acc, string oldacc, int page = 1, int size = 100)
    {
        if (page < 1) page = 1;

        using SqlConnection conn = new(_connectString);

        StringBuilder sb = new($"SELECT * FROM ___RHoldings WITH (NOLOCK) WHERE (TotalUnits > 0) ");

        if (regCode > 0) sb.Append($"AND (RegCode = {regCode}) ");

        if (!string.IsNullOrEmpty(global)) sb.Append(
            $"AND ((FirstName LIKE '%{global}%') OR (MiddleName LIKE '%{global}%') OR (LastName LIKE '%{global}%')" +
            $" OR (CAST(AccountNo AS varchar) LIKE '%{global}%') OR (ClearingNo LIKE '%{global}%')" +
            $" OR (Gender LIKE '%{global}%') OR (Address1 LIKE '%{global}%') OR (Address2 LIKE '%{global}%')" +
            $" OR (City LIKE '%{global}%') OR (Email LIKE '%{global}%') OR (Phone LIKE '%{global}%')" +
            $" OR (Mobile LIKE '%{global}%') OR (CAST(TotalUnits AS varchar) LIKE '%{global}%')) ");

        if (!string.IsNullOrEmpty(name)) sb.Append($"AND ((FirstName LIKE '%{name}%') OR (MiddleName LIKE '%{name}%') OR (LastName LIKE '%{name}%')) ");
        if (!string.IsNullOrEmpty(addr)) sb.Append($"AND ((Address1 LIKE '%{addr}%') OR (Address2 LIKE '%{addr}%') OR (City LIKE '%{addr}%')) ");
        if (!string.IsNullOrEmpty(cscs)) sb.Append($"AND (ClearingNo LIKE '%{cscs}%') ");
        if (!string.IsNullOrEmpty(acc)) sb.Append($"AND (CAST(AccountNo AS varchar) LIKE '%{acc}%') ");
        if (!string.IsNullOrEmpty(oldacc)) sb.Append($"AND (oldacct LIKE '%{oldacc}%') ");

        sb.Append($"ORDER BY FirstName, LastName ");

        if (size > 0)
            sb.Append($"OFFSET {size * (page - 1)} ROWS FETCH NEXT {size} ROWS ONLY ");

        return conn.Query<RegSH>(sb.ToString(), commandTimeout: _commandTimeout).Select(x => x.ArrayString).ToList();
    }

    public List<string[]> GetRegShareholders(int regCode, string s, decimal min, 
        decimal max, DateTime? minDate, DateTime? maxDate, int page = 1, int size = 100)
    {
        if (page < 1) page = 1;

        using SqlConnection conn = new(_connectString);

        StringBuilder sb = new($"SELECT * FROM ___RHoldings WITH (NOLOCK) WHERE (RegCode = {regCode}) AND (TotalUnits > 0) ");

        if (!string.IsNullOrEmpty(s)) sb.Append(
            $"AND ((FirstName LIKE '%{s}%') OR (MiddleName LIKE '%{s}%') OR (LastName LIKE '%{s}%')" +
            $" OR (CAST(AccountNo AS varchar) LIKE '%{s}%') OR (ClearingNo LIKE '%{s}%')" +
            $" OR (Gender LIKE '%{s}%') OR (Address1 LIKE '%{s}%') OR (Address2 LIKE '%{s}%')" +
            $" OR (City LIKE '%{s}%') OR (Email LIKE '%{s}%') OR (Phone LIKE '%{s}%')" +
            $" OR (Mobile LIKE '%{s}%') OR (CAST(TotalUnits AS varchar) LIKE '%{s}%')) ");

        if (min > 0) sb.Append($"AND (TotalUnits >= {min}) ");
        if (max > 0) sb.Append($"AND (TotalUnits <= {max}) ");

        if (minDate is not null && maxDate is not null)
            sb.Append("AND AccountNo IN (SELECT acctno FROM T_unitss WITH (NOLOCK) " +
            $"WHERE (regcode = {regCode}) AND (issue_dt BETWEEN '{minDate:dd/MMM/yyy}' AND '{maxDate:dd/MMM/yyy} 23:59')) ");

        sb.Append($"ORDER BY FirstName ");

        if (size > 0)
            sb.Append($"OFFSET {size * (page - 1)} ROWS FETCH NEXT {size} ROWS ONLY ");

        return conn.Query<RegSH>(sb.ToString(), commandTimeout: _commandTimeout).Select(x => x.ArrayString).ToList();
    }

    public RegSH GetRegShareholder(int regCode, string accNo)
    {
        using SqlConnection conn = new(_connectString);

        return conn.QueryFirstOrDefault<RegSH>($"""
        SELECT * FROM ___RHoldings WITH (NOLOCK) 
        WHERE (RegCode = {regCode}) AND (AccountNo = {accNo})
        """, commandTimeout: _commandTimeout);
    }

    public Bson.Holding GetShareholding(int regCode, string accountNo)
    {
        using SqlConnection conn = new(_connectString);
        var units = conn.Query<Bson.Unit>($"SELECT * FROM ___Units WITH (NOLOCK) WHERE RegCode = {regCode} AND AccountNo = {accountNo}", commandTimeout: _commandTimeout).ToList();
        var dividends = conn.Query<Bson.Dividend>($"""
            SELECT s.Id, s.AccountNo, s.RegCode, s.Gross, s.Total, s.Net, s.DividendNo, s.Tax, s.WarrantNo, s.Type, s.Date,
                   FORMAT(q.date_paid, 'dd-MMM-yyyy') AS DatePaid
            FROM ___SDividends s WITH (NOLOCK)
            LEFT JOIN Qry_Divs q WITH (NOLOCK)
                ON q.divreg_code = s.RegCode AND q.account_no = s.AccountNo AND q.divpay_no = s.DividendNo
            WHERE s.RegCode = {regCode} AND s.AccountNo = {accountNo}
            """, commandTimeout: _commandTimeout).ToList();
        return new Bson.Holding()
        {
            RegCode = regCode,
            AccountNo = accountNo,
            Dividends = dividends,
            Units = units
        };
    }

    public RegSH GetShareholder(int regCode, string accountNo)
    {
        using SqlConnection conn = new(_connectString);
        var shareholder = conn.Query<RegSH>($"SELECT * FROM ___RHoldings WHERE (RegCode = {regCode}) AND (AccountNo = {accountNo})").First();
        shareholder.Units = conn.Query<Bson.Unit>($"SELECT * FROM ___Units WITH (NOLOCK) WHERE RegCode = {regCode} AND AccountNo = {accountNo}", commandTimeout: _commandTimeout).ToList();
        shareholder.Dividends = conn.Query<Bson.Dividend>($"""
            SELECT s.Id, s.AccountNo, s.RegCode, s.Gross, s.Total, s.Net, s.DividendNo, s.Tax, s.WarrantNo, s.Type, s.Date,
                   FORMAT(q.date_paid, 'dd-MMM-yyyy') AS DatePaid
            FROM ___SDividends s WITH (NOLOCK)
            LEFT JOIN Qry_Divs q WITH (NOLOCK)
                ON q.divreg_code = s.RegCode AND q.account_no = s.AccountNo AND q.divpay_no = s.DividendNo
            WHERE s.RegCode = {regCode} AND s.AccountNo = {accountNo}
            """, commandTimeout: _commandTimeout).ToList();
        return shareholder;
    }

    public List<Bson.Unit> GetUnits(int regCode, string accountNo)
    {
        using SqlConnection conn = new(_connectString);
        return conn.Query<Bson.Unit>($"SELECT * FROM ___Units WITH (NOLOCK) WHERE RegCode = {regCode} AND AccountNo = {accountNo}", commandTimeout: _commandTimeout).ToList();
    }

    public List<Bson.Dividend> GetDividends(int regCode, string accountNo)
    {
        using SqlConnection conn = new(_connectString);
        return conn.Query<Bson.Dividend>($"""
            SELECT s.Id, s.AccountNo, s.RegCode, s.Gross, s.Total, s.Net, s.DividendNo, s.Tax, s.WarrantNo, s.Type, s.Date,
                   FORMAT(q.date_paid, 'dd-MMM-yyyy') AS DatePaid
            FROM ___SDividends s WITH (NOLOCK)
            LEFT JOIN Qry_Divs q WITH (NOLOCK)
                ON q.divreg_code = s.RegCode AND q.account_no = s.AccountNo AND q.divpay_no = s.DividendNo
            WHERE s.RegCode = {regCode} AND s.AccountNo = {accountNo}
            """, commandTimeout: _commandTimeout).ToList();
    }

    public Bson.RegMaxUnitResponse GetRegShareholderMaxUnit(int regcode)
    {
        using SqlConnection conn = new(_connectString);
        var maxs = conn.Query<int>($"SELECT MAX(Units) AS MaxUnit FROM ___RHoldings WITH (NOLOCK) WHERE (RegCode = {regcode})", commandTimeout: _commandTimeout).ToList();
        return maxs.FirstOrDefault();
    }

    public Bson.RegSummary GetRegisterSummary(int regcode)
    {
        using SqlConnection conn = new(_connectString);
        var regs = conn.Query<Bson.RegSummary>($"SELECT * FROM ___OnlineRegs WITH (NOLOCK) WHERE (Id = {regcode})", commandTimeout: _commandTimeout).ToList();
        return regs[0];
    }

    public Bson.Shareholder GetShareholder(NewShareholderRequest model)
    {
        using SqlConnection conn = new(_connectString);

        StringBuilder sb = new();

        if (string.IsNullOrEmpty(model.ClearingNo))
        {
            return new Bson.Shareholder
            {
                ClearingNo = model.ClearingNo,
                FullName = model.Name,
                HasHoldings = model.Holdings.Any(),
                Verified = false
            };
        }

        sb.AppendLine(
        $"IF NOT EXISTS(SELECT * FROM ___OnlineSHs WHERE Id = {model.Id}) " +
        $"BEGIN INSERT INTO ___OnlineSHs (Id, ClearingNo, Name, HasHoldings) " +
        $"VALUES ({model.Id}, " +
        $"'{Clear.Tools.StringUtility.SQLSerialize(model.ClearingNo)}', " +
        $"'{Clear.Tools.StringUtility.SQLSerialize(model.Name)}', " +
        $"{Clear.Tools.StringUtility.SQLSerialize(model.Holdings.Any())}) " +
        $"END");

        if (model.ClearingNo != null)
        {
            sb.AppendLine(
            $"UPDATE ___OnlineSHs SET " +
            $"ClearingNo = '{Clear.Tools.StringUtility.SQLSerialize(model.ClearingNo)}', " +
            $"Name = '{Clear.Tools.StringUtility.SQLSerialize(model.Name)}', " +
            $"HasHoldings = {Clear.Tools.StringUtility.SQLSerialize(model.Holdings.Any())} " +
            $"WHERE (Id = {model.Id}) ");
        }

        int outInt = 0;
        var modelHoldings = model.Holdings
            .Where(x => int.TryParse(x.AccountNo, out outInt) && int.TryParse(x.RegCode, out outInt))
            .ToList();

        if (modelHoldings.Any())
        {
            modelHoldings.ForEach(x => sb.AppendLine(
                $"IF NOT EXISTS(SELECT * FROM ___OnlineHoldings WHERE AccountNo = {x.AccountNo} AND RegCode = {x.RegCode}) " +
                $"BEGIN INSERT INTO ___OnlineHoldings (Id, AccountNo, RegCode) " +
                $"VALUES ({model.Id}, {x.AccountNo}, {x.RegCode}) " +
                $"END"));
        }

        sb.AppendLine($"SELECT * FROM ___OnlineSHs WITH (NOLOCK) WHERE Id = {model.Id}");

        var shareholders = conn.Query<Bson.Shareholder>(sb.ToString(), commandTimeout: 3600).ToList();

        if (shareholders.Count <= 0)
            throw new InvalidOperationException("Shareholder not found");

        var shareholder = shareholders[0];

        StringBuilder hsb = new();
        hsb.AppendLine($"SELECT *, {shareholder.Id} AS ShareholderId FROM ___Shareholders WITH (NOLOCK) WHERE ClearingNo = '{shareholder.ClearingNo}'");
        if (modelHoldings.Any())
        {
            hsb.AppendLine($"UNION");
            hsb.AppendLine($"SELECT * FROM ___Shareholdings WITH (NOLOCK) WHERE ShareholderId = {shareholder.Id}");
        }

        var holdings = conn.Query<Bson.Holding>(hsb.ToString(), commandTimeout: 3600).ToList();

        foreach (var holding in holdings)
        {
            holding.Units.AddRange(conn.Query<Bson.Unit>(
                $"SELECT * FROM ___Units WITH (NOLOCK) WHERE RegCode = {holding.RegCode} AND AccountNo = {holding.AccountNo}",
                commandTimeout: 3600));

            holding.Dividends.AddRange(conn.Query<Bson.Dividend>($"""
                SELECT s.Id, s.AccountNo, s.RegCode, s.Gross, s.Total, s.Net, s.DividendNo, s.Tax, s.WarrantNo, s.Type, s.Date,
                       FORMAT(q.date_paid, 'dd-MMM-yyyy') AS DatePaid
                FROM ___SDividends s WITH (NOLOCK)
                LEFT JOIN Qry_Divs q WITH (NOLOCK)
                    ON q.divreg_code = s.RegCode AND q.account_no = s.AccountNo AND q.divpay_no = s.DividendNo
                WHERE s.RegCode = {holding.RegCode} AND s.AccountNo = {holding.AccountNo}
                """, commandTimeout: 3600));

            shareholder.Holdings.Add(holding);
        }

        shareholder.Verified = true;
        shareholder.FullName = model.Name;
        shareholder.ClearingNo = model.ClearingNo;
        shareholder.HasHoldings = shareholder.Holdings.Any();

        return shareholder;
    }

    private async Task EnsureObjects()
    {
        using SqlConnection conn = new(_connectString);
        await conn.ExecuteAsync(Common.InitScript, commandTimeout: _commandTimeout);
    }

    #region mobile

    public Mobile.StockBalance GetStockBalance(int regCode, string accountNo)
    {
        using SqlConnection conn = new(_connectString);

        return conn.QueryFirstOrDefault<Mobile.StockBalance>($"""
        SELECT TOP(10) Id, RegCode, AccountNo, ClearingNo, FirstName, LastName, MiddleName, TotalUnits AS Balance 
        FROM ___RHoldings WITH (NOLOCK)
        WHERE RegCode = {regCode} AND AccountNo = {accountNo}
        """, commandTimeout: _commandTimeout);
    }

    public Mobile.StockBalance GetStockBalance(string clearingNo)
    {
        using SqlConnection conn = new(_connectString);

        return conn.QueryFirstOrDefault<Mobile.StockBalance>($"""
        SELECT TOP(10) Id, RegCode, AccountNo, ClearingNo, FirstName, LastName, MiddleName, TotalUnits AS Balance 
        FROM ___RHoldings WITH (NOLOCK)
        WHERE ClearingNo = '{clearingNo}'
        """, commandTimeout: _commandTimeout);
    }

    #endregion
}