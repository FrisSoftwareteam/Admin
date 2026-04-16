using FirstReg.API.MobileContract;
using FirstReg.Mobile;

namespace FirstReg.API;

public static class MobileEndpoints
{
    public static void AddMobileEndpoints(this WebApplication app, DataService _service)
    {
        app.MapGet(Routes.StockBalanceByAccount, (int regcode, string accno) =>
        {
            var balance = _service.GetStockBalance(regcode, accno);

            return balance is null 
                ? GenericResponse<StockBalance>.CreateFailure("Could not get account balance")
                : GenericResponse<StockBalance>.CreateSuccess(balance);

        }).WithName("GetMobileStockBalance");

        app.MapGet(Routes.StockBalanceByClearing, (string clearingno) => 
        {
            var balance = _service.GetStockBalance(clearingno);

            return balance is null 
                ? GenericResponse<StockBalance>.CreateFailure("Could not get account balance")
                : GenericResponse<StockBalance>.CreateSuccess(balance);
        }).WithName("GetMobileStockBalanceByClearingNo");

        app.MapGet(Routes.Account, (int regcode, string accno) =>
        {
            var holding = _service.GetShareholding(regcode, accno);

            return holding is null
                ? GenericResponse<Bson.Holding>.CreateFailure("Could not get account balance")
                : GenericResponse<Bson.Holding>.CreateSuccess(holding);

        }).WithName("GetMobileHolding");

        app.MapGet(Routes.Dividends, (int regcode, string accno) =>
        {
            var dividends = _service.GetDividends(regcode, accno);

            return dividends is null
                ? GenericResponse<List<Bson.Dividend>>.CreateFailure("Could not get account balance")
                : GenericResponse<List<Bson.Dividend>>.CreateSuccess(dividends);

        }).WithName("GetMobileDividends");

        app.MapGet(Routes.Units, (int regcode, string accno) =>
        {
            var dividends = _service.GetUnits(regcode, accno);

            return dividends is null
                ? GenericResponse<List<Bson.Unit>>.CreateFailure("Could not get account balance")
                : GenericResponse<List<Bson.Unit>>.CreateSuccess(dividends);

        }).WithName("GetMobileUnits");

        //app.MapPost(Routes.Units, (int regcode, VerifyPhoneNumberRequest request) =>
        //{
        //    var holdings = _service.GetHoldingsByPhone(regcode, request.PhoneNumber);

        //    return holdings is null
        //        ? GenericResponse<Bson.Holding>.CreateFailure("Could not get account balance")
        //        : GenericResponse<Bson.Holding>.CreateSuccess(holdings);

        //}).WithName("VerifyPhoneNumber");

        //app.MapPost(Routes.Units, (int regcode, VerifyEmailRequest request) =>
        //{
        //    var holdings = _service.GetHoldingsByEmail(regcode, request.EmailAddress);

        //    return holdings is null
        //        ? GenericResponse<Bson.Holding>.CreateFailure("Could not get account balance")
        //        : GenericResponse<Bson.Holding>.CreateSuccess(holdings);

        //}).WithName("VerifyEmailAddress");
    }

    static class Routes
    {
        const string Default = "/mobile";
        internal const string StockBalanceByAccount = Default + "/stock-balance/{regcode}/{accno}";
        internal const string StockBalanceByClearing = Default + "/stock-balance/{clearingno}";
        internal const string Account = Default + "/account/{regcode}/{accno}";
        internal const string Units = Default + "/units/{regcode}/{accno}";
        internal const string Dividends = Default + "/dividends/{regcode}/{accno}";
        //internal const string VerifyPhoneNumber = Default + "/regcode/verify/phone";
        //internal const string VerifyEmailAddress = Default + "/regcode/verify/email";
    }
}