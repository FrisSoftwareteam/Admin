using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using FirstReg.Data;
using Microsoft.AspNetCore.Http;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirstReg;

public static class MongoTables
{
    public static string Shareholders => "Shareholders";
    public static string Registers => "Registers";
}

public static class Tools
{
    public const string AdminRole = "admin";
    public const int AdminId = 2;
    public const string UrlReferrer = "Referer";

    public static DateTime Now => DateTime.UtcNow.AddHours(1);

    public static string BankAccount => "2013798370 FBN";

    public static async Task<Payment> GetPayStack(string txnref, DateTime cdate, User user)
    {
        PaymentSettings _paystackSetting = PaymentSettings;

        if (string.IsNullOrWhiteSpace(txnref))
            throw new InvalidOperationException("Transaction reference cannot be empty");

        string payResults = await GetPayStackTransaction(_paystackSetting.QueryUrl, txnref, _paystackSetting.SecretKey);

        var payment = new Payment
        {
            Id = txnref,
            Currency = Currency.NGN,
            Date = cdate,
            Updated = cdate,
            Gateway = PaymentGateway.PayStack,
            Status = PaymentStatus.pending,
            UserId = user.Id,
            Description = $"Payment #{txnref}",
            Response = payResults
        };

        try
        {
            var data = payment.PayStackResponse;

            payment.Item = (PaymentItem)Convert.ToInt16(data.GetCustomData(CustomField.pay_item));
            payment.Status = data.data.TranxStatus;
            payment.Amount = data.data.amount / 100;
            payment.Remarks = data.data.status;
        }
        catch
        {
            payment.Status = PaymentStatus.pending;
            payment.Remarks = "Failed to retrieve payment details from source";
        }

        return payment;
    }

    public static SLDocument GetExcelDoc(string title, string filter, out int index, params int[] accountColumnIndexes)
    {
        SLDocument sl = new();

        sl.DocumentProperties.Subject = title;
        sl.DocumentProperties.Creator = "FIRST REGISTRARS AND INVESTOR SERVICES LIMITED";
        sl.DocumentProperties.Title = title;
        sl.DocumentProperties.Description = $"{title} - {filter}";

        sl.SetColumnWidth(1, 100, 16);
        sl.SetColumnStyle(1, 100, new SLStyle
        {
            Alignment = new SLAlignment { Vertical = VerticalAlignmentValues.Center, Horizontal = HorizontalAlignmentValues.Left }
        });

        foreach (int ndx in accountColumnIndexes)
        {
            sl.SetColumnStyle(ndx, new SLStyle
            {
                FormatCode = @"_(* #,##0.00_);_(* (#,##0.00);_(* ""-""??_);_(@_)",
                Alignment = new SLAlignment { Horizontal = HorizontalAlignmentValues.Right }
            });
        }

        index = 1;

        sl.SetCellValue(index, 1, title.ToUpper());
        sl.SetCellStyle(index, 1, new SLStyle
        {
            Font = new SLFont { Bold = true, FontSize = 24 }
        });

        index = 2;

        if (string.IsNullOrEmpty(filter))
            index = 3;
        else
        {
            sl.SetCellValue(index, 1, filter.ToUpper());
            sl.SetCellStyle(index, 1, new SLStyle
            {
                Font = new SLFont { Bold = true, FontSize = 12 }
            });

            index = 4;
        }

        sl.SetRowStyle(index, new SLStyle
        {
            Font = new SLFont { Bold = true }
        });

        return sl;
    }

    public static MemoryStream ExportToExcel(List<RegisterHolding> model, string filter)
    {
        SLDocument sl = GetExcelDoc("Shareholders", filter, out int index, 9);

        sl.SetCellValue(index, 1, "AccountNo");
        sl.SetCellValue(index, 2, "Name");
        sl.SetCellValue(index, 3, "ClearingNo");
        sl.SetCellValue(index, 4, "Phone");
        sl.SetCellValue(index, 5, "Mobile");
        sl.SetCellValue(index, 6, "Email");
        sl.SetCellValue(index, 7, "Address");
        sl.SetCellValue(index, 8, "Units");

        foreach (var itm in model)
        {
            index += 1;
            sl.SetCellValue(index, 1, itm.AccountNo);
            sl.SetCellValue(index, 2, itm.Name);
            sl.SetCellValue(index, 3, itm.ClearingNo);
            sl.SetCellValue(index, 4, itm.Phone);
            sl.SetCellValue(index, 5, itm.Mobile);
            sl.SetCellValue(index, 6, itm.Email);
            sl.SetCellValue(index, 7, itm.Address);
            sl.SetCellValue(index, 8, itm.Units);
        }

        MemoryStream stream = new();
        sl.SaveAs(stream);

        return stream;
    }

    public static MemoryStream ExportToExcel(List<Bson.RegHolding> model, string filter)
    {
        SLDocument sl = GetExcelDoc("Shareholders", filter, out int index);

        sl.SetCellValue(index, 1, "AccountNo");
        sl.SetCellValue(index, 2, "Name");
        sl.SetCellValue(index, 3, "ClearingNo");
        sl.SetCellValue(index, 4, "Phone");
        sl.SetCellValue(index, 5, "Mobile");
        sl.SetCellValue(index, 6, "Email");
        sl.SetCellValue(index, 7, "Address");
        sl.SetCellValue(index, 8, "Units");

        foreach (var itm in model.OrderBy(x => x.FullName))
        {
            index += 1;
            sl.SetCellValue(index, 1, itm.AccountNo);
            sl.SetCellValue(index, 2, itm.FullName);
            sl.SetCellValue(index, 3, itm.ClearingNo);
            sl.SetCellValue(index, 4, itm.Phone);
            sl.SetCellValue(index, 5, itm.Mobile);
            sl.SetCellValue(index, 6, itm.Email);
            sl.SetCellValue(index, 7, itm.Address);
            sl.SetCellValue(index, 8, itm.Units);
        }

        MemoryStream stream = new();
        sl.SaveAs(stream);

        return stream;
    }

    public static MemoryStream ExportToXml(List<Bson.RegHolding> model)
    {
        int index = 1;

        XLWorkbook xlb = new();

        var sl = xlb.Worksheets.Add("Shareholders");

        sl.Cell(index, 1).Value = "AccountNo";
        sl.Cell(index, 2).Value = "Name";
        sl.Cell(index, 3).Value = "ClearingNo";
        sl.Cell(index, 4).Value = "Phone";
        sl.Cell(index, 5).Value = "Mobile";
        sl.Cell(index, 6).Value = "Email";
        sl.Cell(index, 7).Value = "Address";
        sl.Cell(index, 8).Value = "Units";

        foreach (var itm in model.OrderBy(x => x.FullName))
        {
            index += 1;
            sl.Cell(index, 1).Value = itm.AccountNo;
            sl.Cell(index, 2).Value = itm.FullName;
            sl.Cell(index, 3).Value = itm.ClearingNo;
            sl.Cell(index, 4).Value = itm.Phone;
            sl.Cell(index, 5).Value = itm.Mobile;
            sl.Cell(index, 6).Value = itm.Email;
            sl.Cell(index, 7).Value = itm.Address;
            sl.Cell(index, 8).Value = itm.Units;
        }

        MemoryStream stream = new();
        xlb.SaveAs(stream);

        return stream;
    }

    public static MemoryStream ExportToXml(List<ShareSubscription> model)
    {
        int index = 1;

        XLWorkbook xlb = new();

        var sl = xlb.Worksheets.Add("Shareholders");

        sl.Cell(1, 1).Value = "Code";
        sl.Cell(1, 2).Value = "FullName";
        sl.Cell(1, 3).Value = "Phone";
        sl.Cell(1, 4).Value = "Email";
        sl.Cell(1, 5).Value = "Type";
        sl.Cell(1, 6).Value = "NoOfShares";
        sl.Cell(1, 7).Value = "Amount";
        sl.Cell(1, 8).Value = "Date";
        sl.Cell(1, 9).Value = "Rights";
        sl.Cell(1, 10).Value = "Signature";
        sl.Cell(1, 11).Value = "LastName";
        sl.Cell(1, 12).Value = "OtherName";
        sl.Cell(1, 13).Value = "NextOfKin";
        sl.Cell(1, 14).Value = "NextOfKinPhone";
        sl.Cell(1, 15).Value = "ClearingNo";
        sl.Cell(1, 16).Value = "CSCSNumber";
        sl.Cell(1, 17).Value = "StockBroker";
        sl.Cell(1, 18).Value = "MemberCode";
        sl.Cell(1, 19).Value = "BankName";
        sl.Cell(1, 20).Value = "BankAccountNumber";
        sl.Cell(1, 21).Value = "BankBranch";
        sl.Cell(1, 22).Value = "BankCity";
        sl.Cell(1, 23).Value = "BankState";
        sl.Cell(1, 24).Value = "BVN";
        sl.Cell(1, 25).Value = "BVN2";
        sl.Cell(1, 26).Value = "TotalUnits";
        sl.Cell(1, 27).Value = "Title (PublicOffer)";
        sl.Cell(1, 28).Value = "DateOfBirth (PublicOffer)";
        sl.Cell(1, 29).Value = "PostalAddress.Address (PublicOffer)";
        sl.Cell(1, 30).Value = "PostalAddress.City (PublicOffer)";
        sl.Cell(1, 31).Value = "PostalAddress.State (PublicOffer)";
        sl.Cell(1, 32).Value = "PostalAddress.Country (PublicOffer)";
        sl.Cell(1, 33).Value = "OtherApplicant.Title (PublicOffer)";
        sl.Cell(1, 34).Value = "OtherApplicant.LastName (PublicOffer)";
        sl.Cell(1, 35).Value = "OtherApplicant.OtherName (PublicOffer)";
        sl.Cell(1, 36).Value = "CompanySeal (PublicOffer)";
        sl.Cell(1, 37).Value = "RCNumber (PublicOffer)";

        foreach (var itm in model.OrderBy(x => x.Response.FullName))
        {
            index += 1;

            IShareFormModel form = itm.Type switch
            {
                ShareSubscriptionType.PublicOffer => JsonSerializer.Deserialize<PublicOfferModel>(itm.Response.JsonData),
                ShareSubscriptionType.RightIssue => JsonSerializer.Deserialize<RightIssueModel>(itm.Response.JsonData),
                _ => throw new InvalidOperationException("Unknown ShareSubscriptionType")
            };

            sl.Cell(index, 1).Value = itm.Code;
            sl.Cell(index, 2).Value = form.FullName;
            sl.Cell(index, 3).Value = form.Phone;
            sl.Cell(index, 4).Value = form.Email;
            sl.Cell(index, 5).Value = itm.Type.ToString();
            sl.Cell(index, 6).Value = itm.NoOfShares;
            sl.Cell(index, 7).Value = itm.Amount;
            sl.Cell(index, 8).Value = itm.Response.Date;

            // Adding properties from IShareFormModel, excluding Id and OfferId
            sl.Cell(index, 9).Value = form.Rights;
            sl.Cell(index, 10).Value = "";
            sl.Cell(index, 11).Value = form.LastName;
            sl.Cell(index, 12).Value = form.OtherName;
            sl.Cell(index, 13).Value = form.NextOfKin;
            sl.Cell(index, 14).Value = form.NextOfKinPhone;
            sl.Cell(index, 15).Value = form.ClearingNo;
            sl.Cell(index, 16).Value = form.CSCSNumber;
            sl.Cell(index, 17).Value = form.StockBroker;
            sl.Cell(index, 18).Value = form.MemberCode;
            sl.Cell(index, 19).Value = form.BankName;
            sl.Cell(index, 20).Value = form.BankAccountNumber;
            sl.Cell(index, 21).Value = form.BankBranch;
            sl.Cell(index, 22).Value = form.BankCity;
            sl.Cell(index, 23).Value = form.BankState;
            sl.Cell(index, 24).Value = form.BVN;
            sl.Cell(index, 25).Value = form.BVN2;

            // Handle properties specific to derived classes
            if (form is PublicOfferModel publicOffer)
            {
                sl.Cell(index, 26).Value = 0; // TotalUnits
                sl.Cell(index, 27).Value = publicOffer.Title;
                sl.Cell(index, 28).Value = publicOffer.DateOfBirth.ToString(); // Convert DateOnly to string
                if (publicOffer.PostalAddress != null)
                {
                    sl.Cell(index, 29).Value = publicOffer.PostalAddress.Address;
                    sl.Cell(index, 30).Value = publicOffer.PostalAddress.City;
                    sl.Cell(index, 31).Value = publicOffer.PostalAddress.State;
                    sl.Cell(index, 32).Value = publicOffer.PostalAddress.Country;
                }
                if (publicOffer.OtherApplicant != null)
                {
                    sl.Cell(index, 33).Value = publicOffer.OtherApplicant.Title;
                    sl.Cell(index, 34).Value = publicOffer.OtherApplicant.LastName;
                    sl.Cell(index, 35).Value = publicOffer.OtherApplicant.OtherName;
                }
                sl.Cell(index, 37).Value = "";
                sl.Cell(index, 38).Value = publicOffer.RCNumber;
            }
            else if (form is RightIssueModel rightIssue)
            {
                sl.Cell(index, 26).Value = rightIssue.TotalUnits; //TotalUnits
            }
        }

        MemoryStream stream = new();
        xlb.SaveAs(stream);

        return stream;
    }

    public static MemoryStream ExportToXml(RegisterHolderModel model)
    {
        XLWorkbook xlb = AddStatementSheet(new(), model);
        xlb = AddDividendsSheet(xlb, model);

        MemoryStream stream = new();
        xlb.SaveAs(stream);

        return stream;
    }

    private static XLWorkbook AddStatementSheet(XLWorkbook xlb, RegisterHolderModel model)
    {
        int index = 1;

        var sheet = xlb.Worksheets.Add("Statement");

        sheet.Cell(index, 1).Value = $"Statement of account as at {DateTime.Now:dd-MMM-yyy}";

        index += 1;

        index += 1;
        sheet.Cell(index, 2).Value = "Register";
        sheet.Cell(index, 3).Value = model.Register;

        index += 1;
        sheet.Cell(index, 2).Value = "Name";
        sheet.Cell(index, 3).Value = model.Name;

        index += 1;
        sheet.Cell(index, 2).Value = "CSCS No";
        sheet.Cell(index, 3).Value = model.ClearingNo;

        index += 1;
        sheet.Cell(index, 2).Value = "Acc No";
        sheet.Cell(index, 3).Value = model.AccountNo.ToString();

        index += 1;
        sheet.Cell(index, 2).Value = "Old Acc No";
        sheet.Cell(index, 3).Value = "";

        index += 1;
        sheet.Cell(index, 2).Value = "Address";
        sheet.Cell(index, 3).Value = model.Address;

        index += 1;

        int sn = 0;
        decimal balance = 0;

        index += 1;

        sheet.Cell(index, 1).Value = "S/N";
        sheet.Cell(index, 2).Value = "Cert. No.";
        sheet.Cell(index, 3).Value = "Old Cert. No.";
        sheet.Cell(index, 4).Value = "Trans date";
        sheet.Cell(index, 5).Value = "Narration";
        sheet.Cell(index, 6).Value = "Buy";
        sheet.Cell(index, 7).Value = "Sell";
        sheet.Cell(index, 8).Value = "Balance";
        sheet.Cell(index, 9).Value = "Status";

        foreach (var itm in model.Units.OrderBy(x => x.Id))
        {
            index += 1;
            sn += 1;

            decimal credit = itm.TotalUnits > 0 ? itm.TotalUnits : 0;
            decimal debit = itm.TotalUnits < 0 ? Math.Abs(itm.TotalUnits) : 0;
            balance += (credit - debit);

            sheet.Cell(index, 1).Value = sn.ToString();
            sheet.Cell(index, 2).Value = itm.CertNo.ToString();
            sheet.Cell(index, 3).Value = itm.OldCertNo ?? "-";
            sheet.Cell(index, 4).Value = itm.Date;
            sheet.Cell(index, 5).Value = itm.Narration;
            sheet.Cell(index, 6).Value = credit;
            sheet.Cell(index, 7).Value = debit;
            sheet.Cell(index, 8).Value = balance;
            sheet.Cell(index, 9).Value = itm.Status;
        }

        index += 1;
        sheet.Cell(index, 8).Value = model.TotalUnits;

        return xlb;
    }

    private static XLWorkbook AddDividendsSheet(XLWorkbook xlb, RegisterHolderModel model)
    {
        var sl = xlb.Worksheets.Add("Dividends");

        int index = 1;

        sl.Cell(index, 1).Value = $"Dividend History as at {DateTime.Now:dd-MMM-yyy}";

        index += 1;

        index += 1;
        sl.Cell(index, 2).Value = "Name";
        sl.Cell(index, 3).Value = model.Name;

        index += 1;
        sl.Cell(index, 2).Value = "CSCS No";
        sl.Cell(index, 3).Value = model.ClearingNo;

        index += 1;
        sl.Cell(index, 2).Value = "Acc No";
        sl.Cell(index, 3).Value = model.AccountNo.ToString();

        index += 1;
        sl.Cell(index, 2).Value = "Old Acc No";
        sl.Cell(index, 3).Value = "";

        index += 1;
        sl.Cell(index, 2).Value = "Address";
        sl.Cell(index, 3).Value = model.Address;

        index += 1;

        int sn = 0;

        index += 1;

        sl.Cell(index, 1).Value = "S/N";
        sl.Cell(index, 2).Value = "Date";
        sl.Cell(index, 3).Value = "Dividend No.";
        sl.Cell(index, 4).Value = "Warrant No.";
        sl.Cell(index, 5).Value = "Type";
        sl.Cell(index, 6).Value = "Units";
        sl.Cell(index, 7).Value = "Gross";
        sl.Cell(index, 8).Value = "Tax";
        sl.Cell(index, 9).Value = "Net";

        foreach (var itm in model.Dividends.OrderBy(x => x.Id))
        {
            index += 1;
            sn += 1;

            sl.Cell(index, 1).Value = sn.ToString();
            sl.Cell(index, 2).Value = itm.Date;
            sl.Cell(index, 3).Value = itm.DividendNo.ToString();
            sl.Cell(index, 4).Value = itm.WarrantNo.ToString();
            sl.Cell(index, 5).Value = itm.Type;
            sl.Cell(index, 6).Value = itm.Total;
            sl.Cell(index, 7).Value = itm.Gross;
            sl.Cell(index, 8).Value = itm.Tax;
            sl.Cell(index, 9).Value = itm.Net;
        }

        return xlb;
    }

    //public static Subscription GetSubscription(DateTime cdate, int years, DateTime? expiry) => new()
    //{
    //    Date = cdate,
    //    StartDate = expiry > cdate ? (DateTime)expiry : cdate.Date,
    //    EndDate = expiry > cdate ? ((DateTime)expiry).AddYears(years) : cdate.AddYears(years)
    //};

    public static string BlobConnectionString =>
        "DefaultEndpointsProtocol=https;" +
        "AccountName=storeappbc;" +
        "AccountKey=NXIr/BVgeZwb9KxPngewGfZIEG37ZvYkIvNoTRqe6lcbbj/nn1h0ICwdbtM4m7cC9hbTQrKgO+yrC+/FByL0AQ==;" +
        "EndpointSuffix=core.windows.net";
    public static string BlobContainerName => "$web";

    public static string GetUploadPath(blobfolder path) => $"fr\\{path}";
    public static string GetDownloadPath(blobfolder path) => $"https://storeappbc.z19.web.core.windows.net/fr/{path}";
    public static string GetBlobFilePath(blobfolder path, string filename) => $"https://storeappbc.z19.web.core.windows.net/fr/{path}/{filename}";

    public static async Task UploadFileAsync(IFormFile mfile, string filename, blobfolder folder)
    {
        using MemoryStream stream = new() { Position = 0 };
        await mfile.CopyToAsync(stream);
        stream.Position = 0;

        await Clear.Tools.FileManager.UploadToAzureAsync(
            BlobConnectionString, BlobContainerName, stream, mfile.ContentType,
            filename, GetUploadPath(folder));
    }

    public static async Task UploadFileAsync(IFormFile mfile, string filename, blobfolder folder, ImageSize _imgSize)
    {
        Image image = Image.FromStream(mfile.OpenReadStream(), true, true);

        image = Clear.Tools.ImageUtility.ScaleImage(image, _imgSize.Width, _imgSize.Height, Clear.ImageSizePref.Width);
        image = Clear.Tools.ImageUtility.CropImage((Bitmap)image, _imgSize.Width, _imgSize.Height);

        using MemoryStream stream = new() { Position = 0 };
        Clear.Tools.ImageUtility.SaveJpegToStream(stream, image, 60);
        stream.Position = 0;

        await Clear.Tools.FileManager.UploadToAzureAsync(
            BlobConnectionString, BlobContainerName, stream, mfile.ContentType,
            filename, GetUploadPath(folder));
    }

    public static string GenerateFileName(string title, string extension) =>
        Clear.Tools.StringUtility.GenerateFileName(title, extension, "firstReg");

    public static PaymentSettings PaymentSettings => new
    (
        "pk_test_14b08371eabc7e0f4c17d2237928b4870150bdcd",
        "sk_test_a7057044216f24614d8948cb9ac9d02951edccd7",
        "https://js.paystack.co/v1/inline.js",
        "https://api.paystack.co/transaction/verify"
    );

    public static string APIkey => "v#wqhY#3'HQ3v&El*3~0[SSba_@8/6yZ()c(+;dJZO0mI]8B.'+c!@lu,o1CnJv^>t>j=%J!ECf[nr~6&XQp<HF/X=%|9C_/]~TqR2wxI5xX_,4*XOk^wSo8v)|)j_Wx&xWJk.{Hn,MKB4`t%!oT^Kg<!> cjfkZ&^.%+QcD3Av[G < TDO;![kngz}1'<";
    public static int Validatekey => 348723;

    public static string FormsPageLink => "https://firstregistrarsnigeria.com/forms";

    public static string LoginKey => "ZFyUrQ@7hUfJ89!d&F*Wx3S7eS*(@++KAc2ZFyUrQ@7hUfJ89!d&F*Wx3S7eSKAc2";

    public static async Task<string> GetPayStackTransaction(string url, string tranxRef, string secretKey)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");
        var responseMessage = await client.GetAsync($"{url.TrimEnd('/')}/{tranxRef}", HttpCompletionOption.ResponseHeadersRead);
        return responseMessage.Content.ReadAsStringAsync().Result;
    }

    public static string GetValidationCode(string email, DateTime date)
    {
        var code = email.ToCharArray().Sum(x => x) + date.ToFileTimeUtc();
        code /= code.ToString().ToCharArray().Sum(x => x);
        code /= code.ToString().ToCharArray().Sum(x => x);
        code /= code.ToString().ToCharArray().Sum(x => x);
        code /= code.ToString().ToCharArray().Sum(x => x);
        return code.ToString();
    }

    public static bool ValidationCode(string code, string email, DateTime date) =>
        code == GetValidationCode(email, date);

    public static DateTime GetCodeExpiryDate() =>
        Convert.ToDateTime($"{Now.AddHours(2):dd/MMM/yyy HH:00}");

    public static string Shorten(double tt) => Shorten((decimal)tt);
    public static string Shorten(decimal tt)
    {
        if (tt >= 1000000) return $"{(tt / 1000000):N2}M".Replace(".00", "");
        else if (tt >= 1000) return $"{(tt / 1000):N2}K".Replace(".00", "");
        else return tt.ToString("N2").Replace(".00", "");
    }

    public static List<Roles> GetAccessRoles() => Enum.GetValues(typeof(Roles)).Cast<Roles>().ToList();

    public static string GetBase64String(IFormFile mfile)
    {
        using var ms = new MemoryStream();
        mfile.CopyTo(ms);
        return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    public static async Task<Shareholder> UpdateAccountDetails(
        Shareholder sh, List<int> registerIds, Clear.IApiClient _apiClient, EStockApiUrl _apiUrl, Mongo _mondgodb)
    {
        var req = new NewShareholderRequest(sh.Id, sh.ClearingNo, sh.FullName,
            sh.Holdings.Select(x => new NewShareholdingRequest(x.AccountNo, x.RegisterId)));

        var sample = System.Text.Json.JsonSerializer.Serialize(req);

        var model = await _apiClient.PostAsync<NewShareholderRequest, Bson.Shareholder>(_apiUrl.ActivateShareholder, req, "", Common.ApiKeyHeader, false);

        if (model is null)
            throw new InvalidOperationException("Shareholders details could not be retrieved from api");

        _mondgodb.Upsert(model, model.Id, MongoTables.Shareholders);

        sh.LastUpdate = Now;
        foreach (Bson.Holding holdn in model.Holdings)
        {
            if (sh.Holdings.Any(x => x.RegisterId == holdn.RegCode))
            {
                sh.Holdings.First(x => x.RegisterId == holdn.RegCode).AccountNo = holdn.AccountNo;
                sh.Holdings.First(x => x.RegisterId == holdn.RegCode).AccountName = holdn.GetFullName();
                sh.Holdings.First(x => x.RegisterId == holdn.RegCode).Units = holdn.GetTotalUnits();
                //sh.Holdings.First(x => x.RegisterId == holdn.RegCode).Status = ShareHoldingStatus.Verified;
            }
            else if (registerIds.Contains(holdn.RegCode))
            {
                sh.Holdings.Add(new()
                {
                    RegisterId = holdn.RegCode,
                    AccountNo = holdn.AccountNo,
                    AccountName = holdn.GetFullName(),
                    Units = holdn.GetTotalUnits(),
                    Value = 0,
                    Status = ShareHoldingStatus.Verified,
                    Date = Now
                });
            }
        }

        foreach (var h in sh.Holdings)
        {
            if (!model.Holdings.Any(x => x.AccountNo == h.AccountNo && x.RegCode == h.RegisterId))
            {
                h.Units = 0;
                h.Status = ShareHoldingStatus.Pending;
            }
        }

        return sh;
    }
}

public record ImageSize(int Width, int Height);