using FirstReg.Data;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text;

namespace FirstReg.Services
{
    public interface IEmailSender
    {
        Task SendValidationEmailAsync(string email, string name, string code);
        Task<SendResponse> SendWelcomeEmailAsync(string email, string name);
        Task<SendResponse> SendResetPasswordEmailAsync(string email, string name, string link);
        Task<SendResponse> SendAccountDeletedEmailAsync(string email, string name, string reason);
    }

    //=====================

    public record MailAttachment(
        Stream Stream,
        string ContentType,
        string Name
    );

    public class EmailService : IEmailSender
    {
        private string GetTemplate(string name)
        {
            var templatesPath = Path.Combine("wwwroot", "templates");
            var wrapper = File.ReadAllText(Path.Combine(templatesPath, "_layout.html"));

            wrapper = wrapper.Replace("@RenderBody()", File.ReadAllText(Path.Combine(templatesPath, $"{name}.html")));

            wrapper = wrapper.Replace("[site_url]", "https://firstregistrarsnigeria.com");
            wrapper = wrapper.Replace("[year]", DateTime.Now.Year.ToString());
            wrapper = wrapper.Replace("[company_name]", "First Registrars & Investor Services Limited");
            wrapper = wrapper.Replace("[company_address]", "No. 2, Abebe Village Road, Iganmu, Lagos.");
            wrapper = wrapper.Replace("[company_phone]", "+234-1-27010780");
            wrapper = wrapper.Replace("[company_email]", "info@firstregistrarsnigeria.com");

            return wrapper;
        }

        //private string LocalURL(string url) =>
        //    $"{_httpContext.HttpContext.Request.Scheme}://{_httpContext.HttpContext.Request.Host}" +
        //    $"{_httpContext.HttpContext.Request.PathBase}/{url.TrimStart('/')}";

        private MailAddress SenderAddress =>
            new("friscomms@firstregistrarsnigeria.com", "First Registrars & Investor Services Limited");
        //new("friscomms@firstregistrarsnigeria.com", "First Registrars & Investor Services Limited");

        public async Task SendValidationEmailAsync(string email, string name, string code) =>
            await SendEmailAsync(new MailAddress(email, name), "Confirm your Email", "confirm", new { name, code });

        public async Task SendReValidationEmailAsync(string email, string name, string code, string link) =>
            await SendEmailAsync(new MailAddress(email, name), "Confirm your Email", "reconfirm", new { name, code, link });

        public async Task<SendResponse> SendWelcomeEmailAsync(string email, string name) =>
            await SendEmailAsync(new MailAddress(email, name), "Welcome to First Registrars & Investor Services Limited", "welcome", new
            {
                name,
                link = "https://firstregistrarsnigeria.com/access/login"
            });

        public async Task<SendResponse> SendResetPasswordEmailAsync(string email, string name, string link) =>
           await SendEmailAsync(new MailAddress(email, name), "Reset Password", "reset", new { name, link });

        public async Task<SendResponse> SendAccountDeletedEmailAsync(string email, string name, string reason) =>
           await SendEmailAsync(new MailAddress(email, name), "Account Deleted", "accountdeleted", new { name, reason });

        public async Task<SendResponse> SendPasswordEmailAsync(string email, string name, string link) =>
           await SendEmailAsync(new MailAddress(email, name), "Password Changed", "newpassword", new { name, link });

        public async Task<SendResponse> SendSubscrptionEmailAsync(Payment payment) =>
           await SendEmailAsync(payment.User.MailAddress, "You are Subscribed", "subscription", payment);

        public async Task<SendResponse> SendFailedEmailAsync(Payment payment) =>
           await SendEmailAsync(payment.User.MailAddress, $"Your Payment - {payment.Id} Failed", "paymentfailed", payment);

        public async Task SendTicketEmailAsync(string email, string Name, Ticket ticket)
        {
            await SendEmailAsync(new MailAddress(email), $"A new ticket created - #{ticket.Code}", "newticketuser", new { Name, ticket.Code });
            await SendEmailAsync(SenderAddress, $"A new ticket created - #{ticket.Code}", "newticketadmin", new { Name, ticket.Code });
        }

        public async Task SendTicketReplyEmailAsync(Ticket ticket)
        {
            await SendEmailAsync(ticket.User.MailAddress, $"The ticket - #{ticket.Code} has been updated", "ticketreplyuser", ticket);
            await SendEmailAsync(SenderAddress, $"The ticket - #{ticket.Code} has been updated", "ticketreplyadmin", ticket);
        }

        public async Task SendTicketClosedEmailAsync(Ticket ticket)
        {
            await SendEmailAsync(ticket.User.MailAddress, $"The ticket - #{ticket.Code} has been updated", "ticketreplyuser", ticket);
            await SendEmailAsync(SenderAddress, $"The ticket - #{ticket.Code} has been updated", "ticketreplyadmin", ticket);
        }

        public async Task SendBankPayEmailAsync(Payment model, MemoryStream stream, MailAddress address)
        {
            await SendEmailAsync(SenderAddress, $"Kindly confirm my payment - #{model.PayRef}", "bankpayrequest", new
            {
                model.Description,
                model.Date,
                model.Amount,
                model.Currency,
                model.PaidTo,
                model.PayRef,
                Image64 = Convert.ToBase64String(stream.ToArray())
            }, cc: new List<Address>() 
            { 
                new Address("ehgodson@hotmail.com", "Godwin Eh"),
                new Address("oludele.gbenro@firstregistrarsnigeria.com", "Oludele Gbenro"),
                new Address("omoduni.bolorunduro@firstregistrarsnigeria.com", "Omoduni Bolorunduro")
            });

            await SendEmailAsync(
                address, $"Payment Request - #{model.Id} has been updated", "bankpayrecieved", 
                new { Name = address.DisplayName }
            );
        }

        public async Task SendBankPayConfirmEmailAsync(Payment model, MailAddress address) =>
            await SendEmailAsync(address, $"Payment Request - #{model.Id} has been updated", "bankpayconfirmed", model);

		public async Task SendShareOfferCompleted(IShareFormModel model, ShareSubscriptionType type, DateTime date)
		{
            var emailModel = new
            {
                model.Id,
                model.FullName,
                model.NoOfShares,
                model.Rights,
                model.Phone,
                model.Email,
                Date = date,
                Type = type == ShareSubscriptionType.PublicOffer ? "Public offer" : "Right issue",
            };

			await SendEmailAsync(SenderAddress, $"{emailModel.Type} subscription - #{model.Id}", "shareofferadmin", emailModel, cc:
			[
				new Address("ehgodson@hotmail.com", "Godwin Eh"),
                new Address("oludele.gbenro@firstregistrarsnigeria.com", "Oludele Gbenro"),
                new Address("omoduni.bolorunduro@firstregistrarsnigeria.com", "Omoduni Bolorunduro")
            ]);

            await SendEmailAsync(new MailAddress(model.Email, model.FullName), 
                $"Your {emailModel.Type} subscription - #{model.Id}", "shareofferuser", emailModel);
		}

        public async Task SendDataUpdateEmailAsync(DataUpdateModel model)
        {
            var body = BuildDataUpdateEmailBody(model);

            using var client = new SmtpClient("smtp.office365.com")
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential("friscomms@firstregistrarsnigeria.com", "#P1M2B3C?"),
                Port = 587,
            };

            using var message = new MailMessage
            {
                From = SenderAddress,
                Subject = $"Data Update Form Submission - {model.FullName}",
                Body = body,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress("info@firstregistrarsnigeria.com", "First Registrars & Investor Services Limited"));

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                message.ReplyToList.Add(new MailAddress(model.Email, model.FullName));
            }

            await client.SendMailAsync(message);
        }

        private static string BuildDataUpdateEmailBody(DataUpdateModel model)
        {
            static string EncodeText(string value) =>
                WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "Not provided" : value);

            static string EncodeDate(DateOnly value) =>
                WebUtility.HtmlEncode(value == default ? "Not provided" : value.ToString("dd MMM yyyy"));

            static string EncodeEnum<TEnum>(TEnum value) where TEnum : struct, Enum =>
                WebUtility.HtmlEncode(value.ToString());

            static string Address(DataAddressModel value)
            {
                if (value is null)
                {
                    return "Not provided";
                }

                var parts = new[]
                {
                    value.Address,
                    value.City,
                    value.State,
                    value.Country
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(WebUtility.HtmlEncode);

                var rendered = string.Join("<br />", parts);
                return string.IsNullOrWhiteSpace(rendered) ? "Not provided" : rendered;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<html><body style=\"font-family: Arial, sans-serif; color: #1f2937;\">");
            sb.AppendLine("<h2>Data Update Form Submission</h2>");
            sb.AppendLine("<p>A new data update form was submitted from the website.</p>");
            sb.AppendLine("<table cellpadding=\"8\" cellspacing=\"0\" border=\"1\" style=\"border-collapse: collapse; border-color: #d1d5db; width: 100%;\">");

            void Row(string label, string value)
            {
                sb.AppendLine($"<tr><td><strong>{WebUtility.HtmlEncode(label)}</strong></td><td>{value}</td></tr>");
            }

            Row("Surname", EncodeText(model.LastName));
            Row("Other names", EncodeText(model.OtherName));
            Row("Gender", EncodeEnum(model.Gender));
            Row("Age range", EncodeEnum(model.AgeRange));
            Row("New Address", Address(model.NewAddress));
            Row("Previous Address", Address(model.PreviousAddress));
            Row("Primary Number", EncodeText(model.Phone));
            Row("Secondary Number", EncodeText(model.Mobile));
            Row("Email address", EncodeText(model.Email));
            Row("Clearing No", EncodeText(model.ClearingNo));
            Row("NIN", EncodeText(model.NIN));
            Row("TIN", EncodeText(model.TIN));
            Row("Nationality", EncodeText(model.Nationality));
            Row("State of Origin", EncodeText(model.StateOfOrigin));
            Row("Date of Birth", EncodeDate(model.DateOfBirth));
            Row("Next of kin", EncodeText(model.NextOfKin));
            Row("Next of kin phone", EncodeText(model.NextOfKinPhone));
            Row("Signature", string.IsNullOrWhiteSpace(model.Signature) ? "Not provided" : "Provided");

            sb.AppendLine("</table>");
            sb.AppendLine("<h3 style=\"margin-top: 24px;\">Certificates</h3>");
            sb.AppendLine("<ul>");

            if (model.Holdings?.Any() == true)
            {
                foreach (var holding in model.Holdings)
                {
                    sb.AppendLine($"<li>Security ID: {WebUtility.HtmlEncode(holding.RegCode.ToString())}, Account No: {WebUtility.HtmlEncode(holding.AccountNo)}</li>");
                }
            }
            else
            {
                sb.AppendLine("<li>Not provided</li>");
            }

            sb.AppendLine("</ul>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

		// ================================ >>

		public async Task<SendResponse> SendEmailAsync<T>(MailAddress reci, string subject, string template, T model,
            List<Address> cc = null, List<Address> bcc = null, List<MailAttachment> attachments = null)
        {
            Email.DefaultRenderer = new RazorRenderer();
            Email.DefaultSender = new SmtpSender(() => new SmtpClient("smtp.office365.com")
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential("friscomms@firstregistrarsnigeria.com", "#P1M2B3C?"),
                Port = 587,
            });

            var email = Email
                .From(SenderAddress.Address, SenderAddress.DisplayName)
                .To(reci.Address, reci.DisplayName)
                .ReplyTo(reci.Address, reci.DisplayName)
                .Subject(subject)
                .UsingTemplate(GetTemplate(template), model);

            if (cc != null) email.CC(cc);
            if (bcc != null) email.CC(bcc);

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    email.Attach(new FluentEmail.Core.Models.Attachment
                    {
                        ContentType = attachment.ContentType,
                        Data = attachment.Stream,
                        ContentId = attachment.Name
                    });
                }
            }

            return await email.SendAsync();
        }
	}
}
