using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Route("api")]
    [ApiController]
    [AllowAnonymous]
    public class APIController : BaseController<APIController>
    {
        private readonly Mongo _db;

        public APIController(ILogger<APIController> logger, Mongo db, Service service) : base(logger, service) => _db = db;

        public IActionResult Index() => Ok("FR API 1.7.29");

        #region shareholders

        [HttpGet("shareholders")]
        public async Task<IActionResult> GetShareholders()
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                int count = 1000;

                string sqlWhere = 
                    "WHERE (Downloaded = 0) " +
                    "AND (Verified = 1) " +
                    "AND ((ClearingNo <> '') OR (Id IN (SELECT ShareHolderId FROM ShareHoldings)))";

                var sss = await _service.Data.FromSql<Shareholder>(
                    $"SELECT TOP({count}) * FROM Shareholders {sqlWhere}\n" +
                    $"UPDATE Shareholders SET Downloaded = 1 WHERE Id IN (SELECT TOP({count}) Id FROM Shareholders {sqlWhere})");

                return Ok(sss.Select(x => new
                {
                    x.Id,
                    x.ClearingNo,
                    x.FullName,
                    x.Verified,
                    Holdings = x.Holdings.Select(o => new 
                    {
                        x.Id,
                        o.AccountNo,
                        RegCode = o.RegisterId
                    })
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPost("shareholders")]
        public async Task<IActionResult> PostShareholders(Bson.Shareholder model)
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                var builder = new StringBuilder();

                foreach (var holding in model.Holdings)
                {
                    builder.AppendLine(
                        $"IF EXISTS(SELECT * FROM ShareHoldings WHERE ShareHolderId = {model.Id} AND RegisterId = {holding.RegCode}) \n" +
                        $"BEGIN\n" +
                        $" UPDATE ShareHoldings SET AccountNo = '{holding.AccountNo}', AccountName = '{holding.GetFullName}', Units = {holding.GetTotalUnits},\n" +
                        $" Status = 1 WHERE (RegisterId = {holding.RegCode}) AND (ShareHolderId = {model.Id}) \n" +
                        $"END ELSE BEGIN\n" +
                        $"IF EXISTS(SELECT * FROM Registers WHERE Id = {holding.RegCode}) BEGIN \n" +
                        $" INSERT INTO ShareHoldings (RegisterId, ShareHolderId, AccountNo, AccountName, Units, Value, Status, Date)\n" +
                        $" VALUES ({holding.RegCode}, {model.Id}, '{holding.AccountNo}', '{holding.GetFullName}', {holding.GetTotalUnits}, 0, 1, GETDATE()) \nEND \n" +
                        $"END\n");
                }

                await _service.Data.ExecuteSql(builder.ToString());
                _db.Upsert(model, model.Id, MongoTables.Shareholders);

                await UpdateLastId(model.Id, LastIdField.ShareHolders);

                return Ok("Upload was successful");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpGet("shareholders/lastid")]
        public async Task<IActionResult> GetShareholdersLastId()
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                var sss = await _service.Data.Get<LastId>();
                return Ok(sss.Count > 0 ? sss.First().ShareHolders : 0);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPut("shareholders/lastid/{id}")]
        public async Task<IActionResult> PutShareholdersLastId(int id)
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();
                await UpdateLastId(id, LastIdField.ShareHolders);
                return Ok("Update was successful");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPut("shareholders/reset")]
        public async Task<IActionResult> ResetShareholders(int id)
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();
                await _service.Data.ExecuteSql("UPDATE Shareholders SET Downloaded = 0");
                return Ok("Update was successful");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        private async Task UpdateLastId(int id, LastIdField field) => 
            await _service.Data.ExecuteSql($"UPDATE LastIds SET {field} = {id}");

        #endregion

        #region registers

        [HttpGet("registers")]
        public async Task<IActionResult> GetRegisters()
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                var sss = await _service.Data.Get<Register>();
                return Ok(sss.Select(x => new Bson.Register
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPost("registers")]
        public async Task<IActionResult> PostRegisters(Bson.Register model) => await PostDividends(model.Dividends);

        [HttpPost("dividends")]
        public async Task<IActionResult> PostDividends(IEnumerable<Bson.RegDividend> model)
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                var sql = string.Join("\n", model.Select(holding =>
                    $"IF EXISTS(SELECT * FROM Dividends WHERE Id = {holding.Id}) \n" +
                    $"BEGIN\n" +
                    $" UPDATE Dividends SET RegisterId = {holding.RegCode}, PaymentNo = '{holding.PaymentNo}', Description = '{holding.Description}'," +
                    $" AmountDeclared = {holding.AmountDeclared}, YearEnd = '{holding.YearEnd}', DatePayable = '{holding.DatePayable}'," +
                    $" ClosureDate = '{holding.ClosureDate}' WHERE (Id = {holding.Id}) \n" +
                    $"END ELSE BEGIN\n" +
                    $"IF EXISTS(SELECT * FROM Registers WHERE Id = {holding.RegCode}) BEGIN \n" +
                    $"INSERT INTO Dividends (Id, RegisterId, PaymentNo, Description, AmountDeclared, YearEnd, DatePayable, ClosureDate, Date)" +
                    $" VALUES ({holding.Id}, {holding.RegCode}, '{holding.PaymentNo}', '{holding.Description}', {holding.AmountDeclared}, '{holding.YearEnd}'," +
                    $" '{holding.DatePayable}', '{holding.ClosureDate}', GETDATE()) \n END \n" +
                    $"END\n"));

                await _service.Data.ExecuteSql(sql);

                return Ok("Upload was successful");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpGet("regshareholders/lastid")]
        public async Task<IActionResult> GetRegisterShareholdersLastId()
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                var sss = await _service.Data.Get<LastId>();
                return Ok(sss.Count > 0 ? sss.First().RegisterHolding : 0);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPut("regshareholders/lastid/{id}")]
        public async Task<IActionResult> PutRegisterShareholdersLastId(int id)
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();
                await UpdateLastId(id, LastIdField.RegisterHolding);
                return Ok("Update was successful");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        [HttpPost("regshareholders")]
        public async Task<IActionResult> PostRegisterShareholders(IEnumerable<string[]> astrings)
        {
            try
            {
                if (RequestNotValid()) return InvalidResponse();

                var model = astrings.Select(x => new Bson.RegHolding(x));

                var sql = string.Join("\n", model.Select(h =>
                    $"IF EXISTS(SELECT * FROM RegisterHoldings WHERE Id = {h.Id}) " +
                    $"BEGIN" +
                    $" UPDATE RegisterHoldings SET RegisterId = {h.RegCode}, AccountNo = {h.AccountNo}," +
                    $" Name = '{Clear.Tools.StringUtility.SQLSerialize(h.FullName)}'," +
                    $" Address = '{Clear.Tools.StringUtility.SQLSerialize(h.Address)}'," +
                    $" Email = '{Clear.Tools.StringUtility.SQLSerialize(h.Email)}'," +
                    $" Phone = '{Clear.Tools.StringUtility.SQLSerialize(h.Phone)}'," +
                    $" Mobile = '{Clear.Tools.StringUtility.SQLSerialize(h.Mobile)}'," +
                    $" Units = {h.Units}, Date = GETDATE()," +
                    $" ClearingNo = '{Clear.Tools.StringUtility.SQLSerialize(h.ClearingNo)}'," +
                    $" Gender = '{Clear.Tools.StringUtility.SQLSerialize(h.Gender)}'" +
                    $" WHERE (Id = {h.Id}) " +
                    $"END ELSE BEGIN" +
                    $" INSERT INTO RegisterHoldings (Id, RegisterId, AccountNo, Name, Address, Email," +
                    $" Phone, Mobile, Units, Date, ClearingNo, Gender)" +
                    $" VALUES ({h.Id}, {h.RegCode}, {h.AccountNo}, '{Clear.Tools.StringUtility.SQLSerialize(h.FullName)}'," +
                    $" '{Clear.Tools.StringUtility.SQLSerialize(h.Address)}', '{Clear.Tools.StringUtility.SQLSerialize(h.Email)}'," +
                    $" '{Clear.Tools.StringUtility.SQLSerialize(h.Phone)}', '{Clear.Tools.StringUtility.SQLSerialize(h.Mobile)}'," +
                    $" {h.Units}, GETDATE(), '{Clear.Tools.StringUtility.SQLSerialize(h.ClearingNo)}'," +
                    $" '{Clear.Tools.StringUtility.SQLSerialize(h.Gender)}') " +
                    $"END"));

                await _service.Data.ExecuteSql(sql);

                return Ok("Upload was successful");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }

        #endregion

        #region misc

        private bool RequestNotValid() => HttpContext.Request.Headers["key"] != Tools.APIkey;
        private IActionResult InvalidResponse() => StatusCode(StatusCodes.Status401Unauthorized, "Could not validate http request");

        #endregion

        //[HttpPost("upload-accounts")]
        //public IActionResult UploadAccounts(List<AccountDetail> model)
        //{
        //    try
        //    {
        //        if (RequestNotValid()) return InvalidResponse();

        //        foreach (var itm in model)
        //        {
        //            _db.Upsert(itm, itm.Id);
        //        }

        //        return Ok("Upload was successful");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
        //        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        //    }
        //}

        //[HttpPost("upload-dividends")]
        //public IActionResult UploadDividends(List<Dividend> model)
        //{
        //    try
        //    {
        //        if (RequestNotValid()) return InvalidResponse();

        //        foreach (var itm in model)
        //        {
        //            _db.Upsert(itm, itm.Id);
        //        }

        //        return Ok("Upload was successful");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
        //        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        //    }
        //}

        //[HttpPost("upload-dividend-history")]
        //public IActionResult UploadDividendHistory(List<DividendHistory> model)
        //{
        //    try
        //    {
        //        if (RequestNotValid()) return InvalidResponse();

        //        foreach (var itm in model)
        //        {
        //            _db.Upsert(itm, itm.Id);
        //        }

        //        return Ok("Upload was successful");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
        //        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        //    }
        //}

        //public async Task<IActionResult> PostRegisters(Bson.Register model)
        //{
        //    try
        //    {
        //        if (RequestNotValid()) return InvalidResponse();

        //        var builder = new StringBuilder();

        //        foreach (var holding in model.Dividends)
        //        {
        //            builder.AppendLine(
        //                $"IF EXISTS(SELECT * FROM Dividends WHERE Id = {holding.Id}) \n" +
        //                $"BEGIN\n" +
        //                $" UPDATE Dividends SET RegisterId = {holding.RegCode}, PaymentNo = '{holding.PaymentNo}'," +
        //                $" AmountDeclared = {holding.AmountDeclared}, YearEnd = '{holding.YearEnd}', DatePayable = '{holding.DatePayable}'," +
        //                $" ClosureDate = '{holding.ClosureDate}' WHERE (Id = {holding.Id}) \n" +
        //                $"END ELSE BEGIN\n" +
        //                $"INSERT INTO Dividends (Id, RegisterId, PaymentNo, AmountDeclared, YearEnd, DatePayable, ClosureDate, Date)" +
        //                $" VALUES ({holding.Id}, {holding.RegCode}, '{holding.PaymentNo}', {holding.AmountDeclared}, '{holding.YearEnd}'," +
        //                $" '{holding.DatePayable}', '{holding.ClosureDate}', GETDATE()) \n" +
        //                $"END\n");
        //        }

        //        await _service.Data.ExecuteSql(builder.ToString());

        //        return Ok("Upload was successful");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
        //        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
        //    }
        //}
    }
}