using Clear;
using FirstReg.Core;
using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
[Route("admin")]
public class FRAdminController(ILogger<FRAdminController> logger, Service service, IApiClient apiClient, EStockApiUrl apiUrl, Mongo mongo)
        : BaseController(service, AuditLogSection.FrAdmin)
{
        private async Task<RegSH> GetShareholderDetailsModel(int regid, int accno)
        {
                var holdingsQuery = service.Data.GetAsQueryable<ShareHolding>()
                        .Include(x => x.Register)
                        .Include(x => x.Shareholder)
                        .ThenInclude(x => x.User)
                        .Where(x => x.RegisterId == regid);

                var holding = await holdingsQuery
                        .FirstOrDefaultAsync(x => x.AccountNo == accno.ToString());

                // Some account numbers are stored with leading zeroes while the UI passes them as numbers.
                holding ??= holdingsQuery
                        .AsEnumerable()
                        .FirstOrDefault(x => int.TryParse(x.AccountNo, out var dbAccNo) && dbAccNo == accno);

                if (holding == null)
                        throw new InvalidOperationException("The selected shareholder was not found.");

                var model = new RegSH
                {
                        Id = holding.Id,
                        RegCode = holding.RegisterId,
                        Register = holding.Register?.Name ?? "",
                        AccountNo = int.TryParse(holding.AccountNo, out var parsedAccNo) ? parsedAccNo : accno,
                        ClearingNo = holding.Shareholder?.ClearingNo ?? "",
                        Gender = "",
                        Phone = holding.Shareholder?.SecondaryPhone ?? "",
                        Mobile = holding.Shareholder?.PrimaryPhone ?? "",
                        Email = holding.Shareholder?.User?.Email ?? "",
                        Address1 = holding.Shareholder?.Street ?? "",
                        Address2 = holding.Shareholder?.State ?? "",
                        City = holding.Shareholder?.City ?? "",
                        TotalUnits = holding.Units,
                        Units = new List<Bson.Unit>(),
                        Dividends = new List<Bson.Dividend>()
                };

                var fullName = holding.AccountName?.Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                        fullName = holding.Shareholder?.FullName?.Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length == 1)
                        {
                                model.LastName = nameParts[0];
                        }
                        else
                        {
                                model.LastName = nameParts[^1];
                                model.FirstName = nameParts[0];
                                model.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                        }
                }

                if (holding.ShareHolderId > 0)
                {
                        var bsonShareholder = mongo.Find<Bson.Shareholder, int>(holding.ShareHolderId, MongoTables.Shareholders)
                                .FirstOrDefault();

                        var bsonHolding = bsonShareholder?.Holdings?.FirstOrDefault(x =>
                                x.RegCode == regid &&
                                string.Equals(x.AccountNo, holding.AccountNo, StringComparison.OrdinalIgnoreCase));

                        if (bsonHolding != null)
                        {
                                model.ClearingNo = string.IsNullOrWhiteSpace(model.ClearingNo) ? bsonHolding.ClearingNo : model.ClearingNo;
                                model.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? bsonHolding.FirstName : model.FirstName;
                                model.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? bsonHolding.MiddleName : model.MiddleName;
                                model.LastName = string.IsNullOrWhiteSpace(model.LastName) ? bsonHolding.LastName : model.LastName;
                                model.Address1 = string.IsNullOrWhiteSpace(model.Address1) ? bsonHolding.Address1 : model.Address1;
                                model.Address2 = string.IsNullOrWhiteSpace(model.Address2) ? bsonHolding.Address2 : model.Address2;
                                model.Units = bsonHolding.Units ?? new List<Bson.Unit>();
                                model.Dividends = bsonHolding.Dividends ?? new List<Bson.Dividend>();
                                model.TotalUnits = model.Units.Any() ? model.Units.Sum(x => x.TotalUnits) : holding.Units;
                        }
                }

                if (!model.Units.Any())
                {
                        model.Units.Add(new Bson.Unit
                        {
                                Id = holding.Id,
                                AccountNo = model.AccountNo,
                                RegCode = regid,
                                CertNo = 0,
                                Date = holding.Date.ToString("dd-MMM-yyyy"),
                                OldCertNo = "",
                                Description = "Opening balance",
                                Narration = "Opening balance",
                                TotalUnits = holding.Units
                        });
                }

                return model;
        }

        public IActionResult Index()
        {
                return RedirectToAction(nameof(Shareholders));
        }


        [Route("shareholders")]
        public async Task<IActionResult> Shareholders(int regid, string global,
                string name, string addr, string cscs, string acc, string oldacc)
        {
                try
                {
                        await LogAuditAction(AuditLogType.Search,
                                $"{User.Identity.Name} searched with these parameters: RegCode={regid}, Global={global}, " +
                                $"Name={name}, Address={addr}, CSCS={cscs}, Account={acc}, OldAccount={oldacc}");

                        return View(new FRRegisterSHSumm()
                        {
                                RegId = regid,
                                Global = global,
                                Name = name,
                                Address = addr,
                                ClearingNo = cscs,
                                AccountNo = acc,
                                OldAccountNo = oldacc,
                                ListUrl = Url.Action(nameof(GetShareholderLists), new { regid, global, name, addr, cscs, acc, oldacc }),
                                ExportUrl = "",
                                DetailsUrl = Url.Action(nameof(GetShareholderDetails)),
                                DividendsUrl = Url.Action(nameof(GetShareholderDetails))
                        });
                }
                catch (Exception ex)
                {
                        logger.LogError(ex.ToString());
                        TempData["error"] = ex.Message;
                        return View(new FRRegisterSHSumm());
                }
        }

        [HttpGet("shareholders-list")]
        public async Task<IActionResult> GetShareholderLists(
                int regid, string global, string name, string addr, string cscs, string acc, string oldacc)
        {
                return await GetShareholderListz(regid, global, name, addr, cscs, acc, oldacc);
        }

        [HttpGet("shareholders-list/l")]
        public async Task<IActionResult> GetShareholderListz(int regid, string global,
                string name, string addr, string cscs, string acc, string oldacc)
        {
                try
                {
                        IQueryable<ShareHolding> query = service.Data.GetAsQueryable<ShareHolding>()
                                .Where(x => x.AccountName != null && x.AccountName != "");

                        if (regid > 0)
                                query = query.Where(x => x.RegisterId == regid);
                        if (!string.IsNullOrWhiteSpace(name))
                                query = query.Where(x => x.AccountName.Contains(name));
                        if (!string.IsNullOrWhiteSpace(acc))
                                query = query.Where(x => x.AccountNo.Contains(acc));
                        if (!string.IsNullOrWhiteSpace(global))
                                query = query.Where(x => x.AccountName.Contains(global) || x.AccountNo.Contains(global));
                        if (!string.IsNullOrWhiteSpace(cscs))
                                query = query.Where(x => x.Shareholder.ClearingNo.Contains(cscs));
                        if (!string.IsNullOrWhiteSpace(addr))
                                query = query.Where(x => x.Shareholder.Street.Contains(addr) || x.Shareholder.City.Contains(addr));

                        var rows = await query
                                .OrderBy(x => x.AccountName)
                                .Take(5000)
                                .Select(x => new
                                {
                                        x.Id,
                                        x.RegisterId,
                                        x.AccountNo,
                                        x.AccountName,
                                        x.Units,
                                        RegisterName = x.Register.Name,
                                        ClearingNo = x.Shareholder.ClearingNo,
                                        Phone = x.Shareholder.PrimaryPhone
                                })
                                .ToListAsync();

                        return Ok(new
                        {
                                data = rows.Select(x => new[]
                                {
                                        string.IsNullOrEmpty(x.ClearingNo) ? x.AccountNo : $"{x.AccountNo}<br>{x.ClearingNo}",
                                        string.IsNullOrEmpty(x.Phone) ? x.AccountName : $"{x.AccountName}<br/><span class=\"fs-7 fw-normal\">{x.Phone}</span>",
                                        x.Units.ToString("N0"),
                                        x.RegisterName ?? "",
                                        x.AccountNo,
                                        x.Id.ToString(),
                                        x.RegisterId.ToString(),
                                        x.RegisterName ?? ""
                                }).ToList()
                        });
                }
                catch (Exception ex)
                {
                        logger.LogError($"Error fetching shareholders from DB: {Clear.Tools.GetAllExceptionMessage(ex)}");
                        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
                }
        }

        [HttpGet("shareholder/{regid?}/{accno?}")]
        public async Task<IActionResult> GetShareholderDetails(int regid, int accno)
        {
                try
                {
                        var sh = await GetShareholderDetailsModel(regid, accno);

                        await LogAuditAction(AuditLogType.ViewShareholder,
                                $"{User.Identity.Name} viewed the details of this account: RegCode={regid}, " +
                                $"Account={accno}, Name={sh.FullName}, CHN={sh.ClearingNo}, Units={sh.TotalUnits}");

                        return Ok(new RegisterHolderModel(sh));
                }
                catch (InvalidOperationException ex)
                {
                        logger.LogWarning($"Shareholder details lookup failed: {Clear.Tools.GetAllExceptionMessage(ex)};");
                        return NotFound(ex.Message);
                }
                catch (Exception ex)
                {
                        logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
                }
        }

        [HttpGet("shareholder/{regid?}/{accno?}/download")]
        public async Task<IActionResult> DownloadShareholderDetails(int regid, int accno)
        {
                try
                {
                        var sh = await GetShareholderDetailsModel(regid, accno);

                        await LogAuditAction(AuditLogType.DownloadShareholder,
                                $"{User.Identity.Name} downloaded the details of this account: RegCode={regid}, " +
                                $"Account={accno}, Name={sh.FullName}, CHN={sh.ClearingNo}, Units={sh.TotalUnits}");

                        var model = new RegisterHolderModel(sh);
                        var pdfBytes = GenerateCertificatePdf(model);

                        return File(pdfBytes, "application/pdf", $"certificate-{accno}-{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }
                catch (InvalidOperationException ex)
                {
                        logger.LogWarning($"Shareholder details download lookup failed: {Clear.Tools.GetAllExceptionMessage(ex)};");
                        return NotFound(ex.Message);
                }
                catch (Exception ex)
                {
                        logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                        return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
                }
        }

        private static byte[] GenerateCertificatePdf(RegisterHolderModel model)
        {
                var navy = "#1B3A6B";
                var gold = "#C49A2A";
                var lightGray = "#F5F7FA";
                var midGray = "#E2E8F0";
                var textGray = "#64748B";

                var doc = Document.Create(container =>
                {
                        container.Page(page =>
                        {
                                page.Size(PageSizes.A4);
                                page.Margin(0);
                                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor("#1A1A2E"));

                                page.Content().Column(col =>
                                {
                                        // ── HEADER ──────────────────────────────────────────
                                        col.Item().Background(navy).Padding(28).Row(row =>
                                        {
                                                row.RelativeItem().Column(c =>
                                                {
                                                        c.Item().Text("First Registrars & Investor Services")
                                                                .FontSize(16).Bold().FontColor(Colors.White);
                                                        c.Item().PaddingTop(4).Text("CERTIFICATE OF SHAREHOLDING")
                                                                .FontSize(11).FontColor(gold).Bold().LetterSpacing(0.05f);
                                                });
                                                row.ConstantItem(120).AlignRight().AlignMiddle().Column(c =>
                                                {
                                                        c.Item().Text($"Date Issued")
                                                                .FontSize(8).FontColor("#A0AEC0");
                                                        c.Item().Text(DateTime.Now.ToString("dd MMM yyyy"))
                                                                .FontSize(11).Bold().FontColor(Colors.White);
                                                });
                                        });

                                        // ── GOLD DIVIDER ─────────────────────────────────────
                                        col.Item().Height(4).Background(gold);

                                        // ── SHAREHOLDER INFO ─────────────────────────────────
                                        col.Item().Background(lightGray).Padding(24).Column(inner =>
                                        {
                                                inner.Item().PaddingBottom(12).Text("Shareholder Information")
                                                        .FontSize(10).Bold().FontColor(navy);

                                                inner.Item().Table(t =>
                                                {
                                                        t.ColumnsDefinition(c =>
                                                        {
                                                                c.RelativeColumn(1.2f);
                                                                c.RelativeColumn(2f);
                                                                c.RelativeColumn(1.2f);
                                                                c.RelativeColumn(2f);
                                                        });

                                                        void InfoCell(string label, string value)
                                                        {
                                                                t.Cell().PaddingBottom(8).Column(c =>
                                                                {
                                                                        c.Item().Text(label).FontSize(7.5f).FontColor(textGray).Bold();
                                                                        c.Item().PaddingTop(2).Text(value ?? "-").FontSize(9.5f).Bold().FontColor("#1A1A2E");
                                                                });
                                                        }

                                                        InfoCell("Shareholder Name", model.Name?.ToUpper());
                                                        InfoCell("Register", model.Register?.ToUpper());
                                                        InfoCell("Account Number", model.AccountNo.ToString());
                                                        InfoCell("CSCS / CHN Number", string.IsNullOrWhiteSpace(model.ClearingNo) ? "-" : model.ClearingNo);
                                                        InfoCell("Email Address", string.IsNullOrWhiteSpace(model.Email) ? "-" : model.Email);
                                                        InfoCell("Phone", string.IsNullOrWhiteSpace(model.Phone) ? (string.IsNullOrWhiteSpace(model.Mobile) ? "-" : model.Mobile) : model.Phone);
                                                        InfoCell("Address", string.IsNullOrWhiteSpace(model.Address) ? "-" : model.Address);
                                                        InfoCell("Total Balance (Units)", model.TotalUnits.ToString("N0"));
                                                });
                                        });

                                        // ── SECTION HEADER ───────────────────────────────────
                                        col.Item().PaddingHorizontal(24).PaddingVertical(12).Row(row =>
                                        {
                                                row.RelativeItem().Text("Transaction History")
                                                        .FontSize(10).Bold().FontColor(navy);
                                                row.ConstantItem(200).AlignRight()
                                                        .Text($"{model.Units.Count} record(s)")
                                                        .FontSize(8.5f).FontColor(textGray);
                                        });

                                        // ── TABLE ────────────────────────────────────────────
                                        col.Item().PaddingHorizontal(24).Table(t =>
                                        {
                                                t.ColumnsDefinition(c =>
                                                {
                                                        c.ConstantColumn(28);  // S/N
                                                        c.RelativeColumn(1f);  // Cert No
                                                        c.RelativeColumn(1f);  // Old Cert
                                                        c.RelativeColumn(1.4f); // Date
                                                        c.RelativeColumn(2.5f); // Narration
                                                        c.RelativeColumn(1.1f); // Buy
                                                        c.RelativeColumn(1.1f); // Sell
                                                        c.RelativeColumn(1.2f); // Balance
                                                });

                                                // Header row
                                                void HeaderCell(string text, bool right = false)
                                                {
                                                        var cell = t.Cell().Background(navy).Padding(6);
                                                        var aligned = right ? cell.AlignRight() : cell.AlignLeft();
                                                        aligned.Text(text).FontSize(8).Bold().FontColor("#FFFFFF");
                                                }

                                                HeaderCell("S/N");
                                                HeaderCell("Cert. No.");
                                                HeaderCell("Old Cert. No.");
                                                HeaderCell("Trans. Date");
                                                HeaderCell("Narration");
                                                HeaderCell("Buy", true);
                                                HeaderCell("Sell", true);
                                                HeaderCell("Balance", true);

                                                // Data rows
                                                int sn = 0;
                                                decimal balance = 0;
                                                var units = model.Units.OrderBy(x => x.Id).ToList();

                                                for (int i = 0; i < units.Count; i++)
                                                {
                                                        var unit = units[i];
                                                        sn++;
                                                        decimal credit = unit.TotalUnits > 0 ? unit.TotalUnits : 0;
                                                        decimal debit = unit.TotalUnits < 0 ? Math.Abs(unit.TotalUnits) : 0;
                                                        balance += credit - debit;

                                                        var bg = i % 2 == 0 ? "#FFFFFF" : lightGray;

                                                        void DataCell(string val, bool right = false, bool bold = false)
                                                        {
                                                                var cell = t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(midGray).Padding(5);
                                                                var aligned = right ? cell.AlignRight() : cell.AlignLeft();
                                                                var txt = aligned.Text(val ?? "-").FontSize(8);
                                                                if (bold) txt.Bold();
                                                        }

                                                        DataCell(sn.ToString());
                                                        DataCell(unit.CertNo > 0 ? unit.CertNo.ToString() : "-");
                                                        DataCell(string.IsNullOrWhiteSpace(unit.OldCertNo) ? "-" : unit.OldCertNo);
                                                        DataCell(unit.Date ?? "-");
                                                        DataCell(unit.Narration ?? unit.Description ?? "-");
                                                        DataCell(credit > 0 ? credit.ToString("N0") : "-", right: true);
                                                        DataCell(debit > 0 ? debit.ToString("N0") : "-", right: true);
                                                        DataCell(balance.ToString("N0"), right: true, bold: true);
                                                }

                                                // Total row
                                                t.Cell().ColumnSpan(7).Background(navy).Padding(6)
                                                        .AlignRight().Text("Total Balance:").FontSize(8.5f).Bold().FontColor(Colors.White);
                                                t.Cell().Background(gold).Padding(6)
                                                        .AlignRight().Text(model.TotalUnits.ToString("N0")).FontSize(8.5f).Bold().FontColor(Colors.White);
                                        });

                                        // ── FOOTER ───────────────────────────────────────────
                                        col.Item().PaddingTop(30).PaddingHorizontal(24).Column(footer =>
                                        {
                                                footer.Item().BorderTop(1).BorderColor(midGray).PaddingTop(16).Row(row =>
                                                {
                                                        row.RelativeItem().Column(c =>
                                                        {
                                                                c.Item().Text("Authorised Signature").FontSize(8).FontColor(textGray);
                                                                c.Item().PaddingTop(20).Text("_______________________________").FontColor(midGray);
                                                                c.Item().PaddingTop(4).Text("Registrar, First Registrars & Investor Services").FontSize(8).FontColor(textGray);
                                                        });
                                                        row.ConstantItem(180).AlignRight().Column(c =>
                                                        {
                                                                c.Item().Text("Official Stamp").FontSize(8).FontColor(textGray);
                                                                c.Item().PaddingTop(4).Border(1).BorderColor(midGray)
                                                                        .Height(50).Width(120)
                                                                        .AlignCenter().AlignMiddle()
                                                                        .Text("").FontSize(8).FontColor(midGray);
                                                        });
                                                });

                                                footer.Item().PaddingTop(20).Background(lightGray).Padding(10)
                                                        .Text("This certificate is computer-generated and issued by First Registrars & Investor Services Limited. " +
                                                              "It is valid as at the date of issue and subject to the records maintained by the registrar.")
                                                        .FontSize(7.5f).FontColor(textGray).Italic();
                                        });
                                });
                        });
                });

                return doc.GeneratePdf();
        }

        #region profile

        [Route("profile")]
        public async Task<IActionResult> Profile() => View(new UserModel(
                await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower())));

        [Route("profile/update")]
        public async Task<IActionResult> UpdateProfile(UserModel model)
        {
                try
                {
                        var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

                        await LogAuditAction(AuditLogType.ProfileUpdate,
                                $"{User.Identity.Name} Updated their profile");

                        user.FullName = model.FullName.Trim();
                        //user.UserName = model.Email.Trim();
                        //user.Email = model.Email.Trim();
                        user.PhoneNumber = model.MobileNo.Trim();
                        user.StockBroker.Street = model.Street.Trim();
                        user.StockBroker.City = model.City.Trim();
                        user.StockBroker.State = model.State.Trim();
                        //user.StockBroker.Country = model.Country.Trim();
                        user.StockBroker.SecondaryPhone = model.SecondaryPhone.Trim();
                        user.StockBroker.Fax = model.PostCode.Trim();

                        await service.Data.UpdateAsync(user);

                        TempData["success"] = "Your profile was updated";

                        return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                        TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                }
                return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
        }

        #endregion
}
