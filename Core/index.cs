using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using FirstReg.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
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

    /// <summary>
    /// Live register codes shown in the Add known certificates Security dropdown.
    /// Older/inactive books are excluded.
    /// </summary>
    public const int OandoRegisterId = 9;

    public static readonly int[] CertificateRegisterIds =
    [
        5, 6, 7, 9, 10, 11, 12, 13, 14, 16, 31, 62, 63, 80, 82, 83, 87,
        110, 112, 117, 120, 127, 138, 139, 141, 147, 149, 150, 151, 154, 157, 159,
        166, 167, 168, 172, 177, 179, 182, 203, 206, 208, 209, 213, 214, 220, 230, 239,
        251, 252, 253, 257, 262, 266, 271, 277, 280, 281, 284, 286, 289, 300,
        312, 316, 317, 318, 321, 322, 325, 327, 328, 330, 334, 335, 336, 340,
        342, 343, 344, 345, 346, 347, 349, 355, 360, 361, 362, 363, 364, 365, 366,
        411, 412, 413, 416, 418, 420
    ];

    public static bool IsCertificateRegister(int registerId) =>
        CertificateRegisterIds.Contains(registerId);

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
                    Status = ShareHoldingStatus.Pending,
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

    /// <summary>
    /// True when a CSCS/CHN can be used to match Shareholders_staging.
    /// Placeholder values like 0 and ##PARSE_ERROR## are ignored.
    /// </summary>
    public static bool IsRealClearingNo(string clearingNo)
    {
        if (string.IsNullOrWhiteSpace(clearingNo))
            return false;

        var value = clearingNo.Trim();
        if (value.Trim('0').Length == 0)
            return false;
        if (value.Equals("##PARSE_ERROR##", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    static readonly HashSet<string> NameSalutations = new(StringComparer.OrdinalIgnoreCase)
    {
        "MR", "MRS", "MISS", "MS", "DR"
    };

    static string[] SignificantNameTokens(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<string>();

        return name.Trim().ToUpperInvariant()
            .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim('.', ',', ';', ':'))
            .Where(t => t.Length > 1 && !NameSalutations.Contains(t))
            .ToArray();
    }

    static string NormalizePersonName(string name) =>
        string.Join(" ", SignificantNameTokens(name));

    public static bool NamesLikelySame(string left, string right)
    {
        var a = NormalizePersonName(left);
        var b = NormalizePersonName(right);
        if (a.Length == 0 || b.Length == 0)
            return false;
        if (a == b)
            return true;

        var aTokens = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bTokens = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var shared = aTokens.Intersect(bTokens).Count();
        if (shared >= 2)
            return true;
        return aTokens.All(bTokens.Contains) || bTokens.All(aTokens.Contains);
    }

    public static decimal ParseStagingHoldings(string holdings)
    {
        if (string.IsNullOrWhiteSpace(holdings))
            return 0m;
        var cleaned = holdings.Replace(",", "").Trim();
        return decimal.TryParse(cleaned, out var units) ? units : 0m;
    }

    /// <summary>
    /// Looks up one account number in one register (live estock first, then staging).
    /// </summary>
    public static async Task<ShareholderStaging> FindRegisterAccount(
        int registerId, string accountNo, FirstReg.Services.DataService data)
    {
        if (registerId <= 0 || !int.TryParse((accountNo ?? "").Trim(), out var acc))
            return null;

        var live = await FindLiveRegisterHoldings(
            [new ShareHolding { RegisterId = registerId, AccountNo = acc.ToString() }], data);
        var row = live.FirstOrDefault();
        if (row != null)
            return row;

        var staging = await data.Find<ShareholderStaging>(x =>
            x.RegisterCode == registerId && x.AccountNumber == acc);
        return staging.FirstOrDefault();
    }

    /// <summary>
    /// Register name belongs to this pending applicant when it has no extra given names.
    /// "Anosike Chioma Lilian" is not the same person as "Anosike Chioma".
    /// </summary>
    public static bool AccountNameMatchesApplicant(string fullName, string accountName)
    {
        var profile = SignificantNameTokens(fullName);
        var account = SignificantNameTokens(accountName);
        if (profile.Length == 0 || account.Length == 0)
            return false;
        return account.All(profile.Contains);
    }

    /// <summary>
    /// Pending accounts should only keep registrar + account numbers from signup or Admin
    /// update. Do not keep extra companies pulled from the register by CHN, name, or
    /// account-number search.
    /// </summary>
    public static void RestrictUnverifiedHoldingsToRegistration(Shareholder sh)
    {
        if (sh == null || sh.Verified || sh.Holdings == null || sh.Holdings.Count == 0)
            return;

        var typedAcc = (sh.AccountNo ?? "").Trim();

        // CHN-only signup: every holding was imported from the register. Hide them all
        // until Admin types the actual registrar + account number on Update.
        if (string.IsNullOrWhiteSpace(typedAcc))
        {
            foreach (var h in sh.Holdings)
                h.Hidden = true;
            return;
        }

        foreach (var h in sh.Holdings)
        {
            if (SameShareAccountNo(h.AccountNo, typedAcc) ||
                AccountNameMatchesApplicant(sh.FullName, h.AccountName))
                continue;
            h.Hidden = true;
        }
    }

    public static bool SameShareAccountNo(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        if (int.TryParse(left.Trim(), out var a) && int.TryParse(right.Trim(), out var b))
            return a == b;
        return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets the visible investments to the registrar + account number pairs supplied by Admin.
    /// Extra auto-attached companies stay hidden. New rows stay pending for Review.
    /// </summary>
    public static void ApplyRegisteredAccounts(Shareholder sh, IEnumerable<(int RegisterId, string AccountNo)> accounts)
    {
        if (sh?.Holdings == null)
            return;

        var entries = (accounts ?? [])
            .Where(x => x.RegisterId > 0 && IsCertificateRegister(x.RegisterId) && !string.IsNullOrWhiteSpace(x.AccountNo))
            .Select(x => (x.RegisterId, Acc: x.AccountNo.Trim()))
            .GroupBy(x => $"{x.RegisterId}:{(int.TryParse(x.Acc, out var n) ? n.ToString() : x.Acc.ToUpperInvariant())}")
            .Select(g => g.First())
            .ToList();

        var stamp = sh.Holdings.Count > 0 ? sh.Holdings.Min(h => h.Date) : Now;

        foreach (var h in sh.Holdings)
        {
            var keep = entries.Any(e => e.RegisterId == h.RegisterId && SameShareAccountNo(h.AccountNo, e.Acc));
            h.Hidden = !keep;
        }

        foreach (var entry in entries)
        {
            var match = sh.Holdings.FirstOrDefault(h =>
                h.RegisterId == entry.RegisterId && SameShareAccountNo(h.AccountNo, entry.Acc));
            if (match != null)
            {
                match.Hidden = false;
                match.AccountNo = entry.Acc;
                match.Date = stamp;
                match.AccountName = sh.FullName;
                continue;
            }

            sh.Holdings.Add(new ShareHolding
            {
                Date = stamp,
                RegisterId = entry.RegisterId,
                AccountNo = entry.Acc,
                AccountName = sh.FullName,
                Units = 0,
                Status = ShareHoldingStatus.Pending
            });
        }

        if (entries.Count > 0)
            sh.AccountNo = entries[0].Acc;
    }

    /// <summary>
    /// Staging rows belong to this profile when the CHN matches, or the names match
    /// after ignoring salutations such as Mr/Mrs. Account number alone is not enough.
    /// </summary>
    public static bool StagingRowBelongsToShareholder(Shareholder sh, ShareholderStaging row)
    {
        if (sh == null || row == null)
            return false;

        var shChn = IsRealClearingNo(sh.ClearingNo);
        var rowChn = IsRealClearingNo(row.ClearingNo);

        if (shChn && rowChn &&
            string.Equals(sh.ClearingNo.Trim(), row.ClearingNo.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        return NamesLikelySame(sh.FullName, row.Names);
    }

    /// <summary>
    /// Populates a shareholder's holdings/units from frdb's Shareholders_staging import,
    /// matched by clearing house number. Existing account numbers are used only when
    /// the staging row also belongs to this shareholder.
    /// </summary>
    public static async Task<Shareholder> UpdateAccountDetailsFromStaging(
        Shareholder sh, List<int> registerIds, FirstReg.Services.DataService data,
        bool restoreHidden = false, bool attachNew = false)
    {
        sh.LastUpdate = Now;

        if (!sh.Verified)
        {
            var existing = sh.Holdings.Where(h => !h.Hidden).ToList();
            if (existing.Count == 0)
                return sh;

            var liveRows = await FindLiveRegisterHoldings(existing, data);
            foreach (var h in existing)
            {
                if (!int.TryParse(h.AccountNo, out var acc))
                    continue;
                var live = liveRows.FirstOrDefault(x =>
                    x.RegisterCode == h.RegisterId && x.AccountNumber == acc);
                if (live == null)
                    continue;
                if (h.Status == ShareHoldingStatus.Verified)
                {
                    h.AccountName = live.Names;
                    h.Units = ParseStagingHoldings(live.Holdings);
                }
                else if (string.IsNullOrWhiteSpace(h.AccountName))
                {
                    h.AccountName = live.Names;
                }
            }

            return sh;
        }

        var holdingKeys = sh.Holdings
            .Select(h => new
            {
                h.RegisterId,
                AccountNo = int.TryParse(h.AccountNo, out var acc) ? acc : (int?)null
            })
            .Where(x => x.AccountNo.HasValue)
            .Select(x => (x.RegisterId, AccountNo: x.AccountNo.Value))
            .ToList();

        var accountNos = holdingKeys.Select(x => x.AccountNo).Distinct().ToList();
        int? profileAcc = int.TryParse((sh.AccountNo ?? "").Trim(), out var parsedProfileAcc)
            ? parsedProfileAcc
            : null;
        if (profileAcc.HasValue && !accountNos.Contains(profileAcc.Value))
            accountNos.Add(profileAcc.Value);

        var hasChn = IsRealClearingNo(sh.ClearingNo);
        var chn = hasChn ? sh.ClearingNo.Trim() : null;
        var nameTokens = SignificantNameTokens(sh.FullName);

        if (!hasChn && accountNos.Count == 0 && nameTokens.Length < 2)
            return sh;

        List<ShareholderStaging> fetched = new();
        if (hasChn && accountNos.Count > 0)
            fetched = await data.Find<ShareholderStaging>(x => x.ClearingNo == chn || accountNos.Contains(x.AccountNumber));
        else if (hasChn)
            fetched = await data.Find<ShareholderStaging>(x => x.ClearingNo == chn);
        else if (accountNos.Count > 0)
            fetched = await data.Find<ShareholderStaging>(x => accountNos.Contains(x.AccountNumber));

        var foreign = sh.Holdings.Where(h =>
        {
            if (h.Hidden) return false;
            if (h.Status != ShareHoldingStatus.Verified) return false;
            if (!int.TryParse(h.AccountNo, out var acc))
                return false;
            var row = fetched.FirstOrDefault(x => x.AccountNumber == acc && x.RegisterCode == h.RegisterId);
            return row != null && !StagingRowBelongsToShareholder(sh, row);
        }).ToList();
        foreach (var h in foreign)
            h.Hidden = true;

        var rows = fetched.Where(x =>
            StagingRowBelongsToShareholder(sh, x) &&
            (
                (hasChn && string.Equals((x.ClearingNo ?? "").Trim(), chn, StringComparison.OrdinalIgnoreCase))
                || holdingKeys.Any(k => k.RegisterId == x.RegisterCode && k.AccountNo == x.AccountNumber)
                || (profileAcc.HasValue && x.AccountNumber == profileAcc.Value)
                || NamesLikelySame(sh.FullName, x.Names)
            )
        ).ToList();

        if (hasChn)
        {
            foreach (var live in await FindLiveRegisterHoldingsByChn(chn, data))
                MergeLiveRegisterRow(rows, sh, holdingKeys, live);
        }

        // Oando is often missing from Shareholders_staging. Always take it from the live register.
        var oandoKeys = accountNos
            .Distinct()
            .Select(acc => (Acc: acc, Reg: OandoRegisterId))
            .ToList();
        foreach (var key in holdingKeys.Where(k => k.RegisterId == OandoRegisterId))
        {
            if (!oandoKeys.Any(x => x.Acc == key.AccountNo && x.Reg == key.RegisterId))
                oandoKeys.Add((key.AccountNo, key.RegisterId));
        }
        foreach (var live in await FindLiveRegisterHoldingsByAccounts(oandoKeys, data))
            MergeLiveRegisterRow(rows, sh, holdingKeys, live);

        static bool SameAccountNo(string stored, int accountNumber)
        {
            if (string.IsNullOrWhiteSpace(stored))
                return false;
            return int.TryParse(stored.Trim(), out var acc) && acc == accountNumber;
        }

        var canAttach = attachNew || restoreHidden;

        foreach (var row in rows)
        {
            var units = ParseStagingHoldings(row.Holdings);
            var accountNo = row.AccountNumber.ToString();

            var existing = sh.Holdings.FirstOrDefault(x =>
                x.RegisterId == row.RegisterCode && SameAccountNo(x.AccountNo, row.AccountNumber));
            if (existing != null)
            {
                if (restoreHidden)
                    existing.Hidden = false;

                if (existing.Status == ShareHoldingStatus.Verified)
                {
                    existing.AccountNo = accountNo;
                    existing.AccountName = row.Names;
                    existing.Units = units;
                }
                continue;
            }

            var typed = holdingKeys.Any(k => k.RegisterId == row.RegisterCode && k.AccountNo == row.AccountNumber);
            if (!canAttach || !typed)
                continue;

            if (registerIds.Contains(row.RegisterCode) &&
                !sh.Holdings.Any(x => x.RegisterId == row.RegisterCode && SameAccountNo(x.AccountNo, row.AccountNumber)))
            {
                sh.Holdings.Add(new()
                {
                    RegisterId = row.RegisterCode,
                    AccountNo = accountNo,
                    AccountName = row.Names,
                    Units = units,
                    Value = 0,
                    Status = ShareHoldingStatus.Pending,
                    Date = Now
                });
            }
        }

        var unmatched = sh.Holdings.Where(x => !x.Hidden && x.Status == ShareHoldingStatus.Verified).Where(h =>
        {
            if (!int.TryParse(h.AccountNo, out var acc))
                return true;
            return !rows.Any(x => x.AccountNumber == acc && x.RegisterCode == h.RegisterId);
        }).ToList();

        if (unmatched.Count > 0)
        {
            var liveRows = await FindLiveRegisterHoldings(unmatched, data);
            foreach (var h in unmatched)
            {
                if (!int.TryParse(h.AccountNo, out var acc))
                    continue;

                var live = liveRows.FirstOrDefault(x =>
                    x.AccountNumber == acc && x.RegisterCode == h.RegisterId);

                if (live != null && StagingRowBelongsToShareholder(sh, live))
                {
                    h.AccountNo = live.AccountNumber.ToString();
                    h.AccountName = live.Names;
                    h.Units = ParseStagingHoldings(live.Holdings);
                }
            }
        }

        return sh;
    }

    static string EstockConnectionString(FirstReg.Services.DataService data)
    {
        var frdbCs = data.GetConnectionString();
        if (string.IsNullOrWhiteSpace(frdbCs))
            return null;
        return new SqlConnectionStringBuilder(frdbCs) { InitialCatalog = "estock" }.ConnectionString;
    }

    static void MergeLiveRegisterRow(
        List<ShareholderStaging> rows,
        Shareholder sh,
        List<(int RegisterId, int AccountNo)> holdingKeys,
        ShareholderStaging live)
    {
        if (live == null)
            return;

        var alreadyOnProfile = holdingKeys.Any(k =>
            k.RegisterId == live.RegisterCode && k.AccountNo == live.AccountNumber);
        if (!alreadyOnProfile && !StagingRowBelongsToShareholder(sh, live))
            return;

        var idx = rows.FindIndex(r =>
            r.RegisterCode == live.RegisterCode && r.AccountNumber == live.AccountNumber);
        if (idx >= 0)
            rows[idx] = live;
        else
            rows.Add(live);
    }

    static async Task<decimal> LiveRegisterUnits(SqlConnection conn, int accountNo, int registerId)
    {
        try
        {
            await using var cmd = new SqlCommand(@"
SELECT TOP 1 SumOfno_of_units
FROM getSumOfHoldings
WHERE account_no = @acc AND reg_code = @reg", conn)
            {
                CommandTimeout = 15
            };
            cmd.Parameters.AddWithValue("@acc", accountNo);
            cmd.Parameters.AddWithValue("@reg", registerId);
            var value = await cmd.ExecuteScalarAsync();
            if (value != null && value != DBNull.Value)
                return Convert.ToDecimal(value);
        }
        catch { /* fall through to T_units */ }

        try
        {
            await using var cmd = new SqlCommand(@"
SELECT ISNULL(SUM(no_of_units), 0)
FROM T_units WITH (NOLOCK)
WHERE account_no = @acc AND reg_code = @reg AND ISNULL(certificate_status, 0) = 1", conn)
            {
                CommandTimeout = 15
            };
            cmd.Parameters.AddWithValue("@acc", accountNo);
            cmd.Parameters.AddWithValue("@reg", registerId);
            var value = await cmd.ExecuteScalarAsync();
            if (value != null && value != DBNull.Value)
                return Convert.ToDecimal(value);
        }
        catch { }

        return 0m;
    }

    static async Task<List<ShareholderStaging>> FindLiveRegisterHoldingsByAccounts(
        List<(int Acc, int Reg)> keys, FirstReg.Services.DataService data)
    {
        if (keys == null || keys.Count == 0)
            return [];

        var holdings = keys
            .Distinct()
            .Select(k => new ShareHolding { AccountNo = k.Acc.ToString(), RegisterId = k.Reg })
            .ToList();
        return await FindLiveRegisterHoldings(holdings, data);
    }

    static async Task<List<ShareholderStaging>> FindLiveRegisterHoldingsByChn(
        string chn, FirstReg.Services.DataService data)
    {
        var results = new List<ShareholderStaging>();
        var estockCs = EstockConnectionString(data);
        if (string.IsNullOrWhiteSpace(estockCs) || !IsRealClearingNo(chn))
            return results;

        try
        {
            await using var conn = new SqlConnection(estockCs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(@"
SELECT Acctno, regcode,
    LTRIM(RTRIM(CONCAT(
        ISNULL(last_nm, ''), ' ',
        ISNULL(first_nm, ''), ' ',
        ISNULL(middle_nm, '')
    ))) AS Names,
    LTRIM(RTRIM(ISNULL(chn, ''))) AS ClearingNo
FROM T_shold WITH (NOLOCK)
WHERE chn = @chn", conn)
            {
                CommandTimeout = 30
            };
            cmd.Parameters.AddWithValue("@chn", chn.Trim());

            var pending = new List<(int Acc, int Reg, string Names, string ClearingNo)>();
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    pending.Add((
                        reader.GetInt32(0),
                        Convert.ToInt32(reader.GetValue(1)),
                        reader.IsDBNull(2) ? "" : reader.GetString(2),
                        reader.IsDBNull(3) ? "" : reader.GetString(3)
                    ));
                }
            }

            foreach (var row in pending.GroupBy(x => (x.Acc, x.Reg)).Select(g => g.First()))
            {
                decimal units = 0m;
                try { units = await LiveRegisterUnits(conn, row.Acc, row.Reg); }
                catch { /* keep 0 */ }

                results.Add(new ShareholderStaging
                {
                    AccountNumber = row.Acc,
                    RegisterCode = row.Reg,
                    Names = row.Names,
                    ClearingNo = row.ClearingNo,
                    Holdings = units.ToString()
                });
            }
        }
        catch
        {
            return results;
        }

        return results;
    }

    static async Task<List<ShareholderStaging>> FindLiveRegisterHoldings(
        List<ShareHolding> holdings, FirstReg.Services.DataService data)
    {
        var results = new List<ShareholderStaging>();
        var estockCs = EstockConnectionString(data);
        if (string.IsNullOrWhiteSpace(estockCs) || holdings == null || holdings.Count == 0)
            return results;

        try
        {
            await using var conn = new SqlConnection(estockCs);
            await conn.OpenAsync();

            foreach (var h in holdings)
            {
                if (!int.TryParse(h.AccountNo, out var acc) || h.RegisterId <= 0)
                    continue;

                await using var cmd = new SqlCommand(@"
SELECT TOP 1
    Acctno,
    regcode,
    LTRIM(RTRIM(CONCAT(
        ISNULL(last_nm, ''), ' ',
        ISNULL(first_nm, ''), ' ',
        ISNULL(middle_nm, '')
    ))) AS Names,
    LTRIM(RTRIM(ISNULL(chn, ''))) AS ClearingNo
FROM T_shold WITH (NOLOCK)
WHERE Acctno = @acc AND regcode = @reg", conn)
                {
                    CommandTimeout = 20
                };
                cmd.Parameters.AddWithValue("@acc", acc);
                cmd.Parameters.AddWithValue("@reg", h.RegisterId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    continue;

                var names = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var clearing = reader.IsDBNull(3) ? "" : reader.GetString(3);
                await reader.CloseAsync();

                decimal units = 0m;
                try { units = await LiveRegisterUnits(conn, acc, h.RegisterId); }
                catch { /* keep 0 */ }

                results.Add(new ShareholderStaging
                {
                    AccountNumber = acc,
                    RegisterCode = h.RegisterId,
                    Names = names,
                    ClearingNo = clearing,
                    Holdings = units.ToString()
                });
            }
        }
        catch
        {
            return results;
        }

        return results;
    }
}

public record ImageSize(int Width, int Height);