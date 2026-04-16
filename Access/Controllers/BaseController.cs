using FirstReg.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

public abstract class BaseController(Service service, AuditLogSection auditLogSection) : Controller
{
    protected static string GetSignature(string signatureString, IFormFile signatureFile)
    {
        if (!string.IsNullOrEmpty(signatureString))
        {
            return signatureString;
        }

        if (signatureFile is not null)
        {
            return Tools.GetBase64String(signatureFile);
        }

        throw new InvalidOperationException("You need to either upload or draw your signature");
    }

    protected async Task SendProofOfPayment(IFormFile file, Payment payment, MailAddress address)
    {
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            await service.Email.SendBankPayEmailAsync(payment, ms, address);
        }
        catch
        {
            TempData["info"] = $"Email could not be sent";
        }
    }

    protected string GetReferrerUrl()
    {
        var referrer = Request.Headers["Referer"].ToString();
        return string.IsNullOrEmpty(referrer) ? "/" : referrer;
    }

    protected async Task LogAuditAction(AuditLogType type, string logDescription, int? userId = null)
    {
        userId ??= (await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower()))?.Id;

        await service.Data.SaveAsync(AuditLog.Create(auditLogSection, type,
            userId ?? throw new InvalidOperationException($"Could not log action because user id not found: {User.Identity.Name}"),
            logDescription, DateTime.Now));
    }
}