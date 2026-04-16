namespace FirstReg.API;

public static class DefaultEndpoints
{
    public static void AddDefaultEndpoints(this WebApplication app, DataService _service)
    {
        app.MapPost("/activate-shareholder", (NewShareholderRequest model)
        => _service.GetShareholder(model)).WithName("GetShareholder");

        app.MapGet("/regshareholders/{regcode}", (int regcode, string s, decimal min, decimal max, DateTime? mindate, DateTime? maxdate, int page, int size)
        => _service.GetRegShareholders(regcode, s, min, max, mindate, maxdate, page, size)).WithName("GetRegShareholders");
        
        app.MapGet("/regshareholder/{regcode}/{accno}", (int regcode, string accno)
        => _service.GetRegShareholder(regcode, accno)).WithName("GetRegShareholder");

        app.MapGet("/shareholders/{regcode}", (int regcode, string global, string name, string addr, string cscs, string acc, string oldacc, int page, int size)
        => _service.GetShareholders(regcode, global, name, addr, cscs, acc, oldacc, page, size)).WithName("GetShareholders");

        app.MapGet("/shareholders", (int regcode, string global, string name, string addr, string cscs, string acc, string oldacc, int page, int size)
        => _service.GetShareholders(regcode, global, name, addr, cscs, acc, oldacc, page, size)).WithName("GetGlobalShareholders");

        app.MapGet("/holding/{regcode}/{accno}", (int regcode, string accno)
        => _service.GetShareholding(regcode, accno)).WithName("GetShareholding");

        app.MapGet("/units/{regcode}/{accno}", (int regcode, string accno)
        => _service.GetShareholder(regcode, accno)).WithName("GetUnits");

        app.MapGet("/dividends/{regcode}/{accno}", (int regcode, string accno)
        => _service.GetDividends(regcode, accno)).WithName("GetDividends");

        app.MapGet("/register/summary/{regcode}", (int regcode)
        => _service.GetRegisterSummary(regcode)).WithName("GetRegSummary");
    }
}