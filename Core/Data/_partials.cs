using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;

namespace FirstReg.Data
{
    public partial class User
    {
        public string Initial => FullName.ToCharArray().First().ToString().ToUpper();
        public string LastName => FullName.Split(' ').Last();
        public string FirstName => FullName.Replace(LastName, "").Trim();
        public MailAddress MailAddress => new(Email, FullName);

        public string RolesString => string.Join(",", AccessRoles.Select(x => (int)x.Role));

        public decimal GetSubscriptionPrice(List<SubscriptionPlan> plans)
        {
            decimal amount = 0;

            switch (Type)
            {
                case UserType.Shareholder:
                    foreach (var sh in Shareholders.Where(x => x.IsSubscribed == false))
                    {
                        amount += sh.IsCompany
                            ? plans.First(x => x.Id == SubscriptionType.CorporateShareholder).Price
                            : plans.First(x => x.Id == SubscriptionType.IndividualShareholder).Price;
                    }
                    break;
                case UserType.StockBroker:
                    amount = plans.First(x => x.Id == SubscriptionType.StockBroker).Price;
                    break;
            }

            return amount;
        }

        public bool HasAccess(params Roles[] roles)
        {
            foreach (var role in roles)
            {
                if (!AccessRoles.Any(x => x.Role == role)) return false;
            }

            return true;
        }

        public bool IsSuper() =>
            UserName.Equals("host", StringComparison.CurrentCultureIgnoreCase) ||
            UserName.Equals("admin", StringComparison.CurrentCultureIgnoreCase);
    }

    public partial class ShareholderView
    {
        public bool IsSubscribed => ExpiryDate > Tools.Now;
    }

    public partial class Shareholder
    {
        public string LastName
        {
            get
            {
                try
                {
                    return FullName.Split(' ').Last();
                }
                catch
                {
                    return "";
                }
            }
        }

        public string FirstName
        {
            get
            {
                try
                {
                    return FullName.Replace(LastName, "").Trim();
                }
                catch
                {
                    return "";
                }
            }
        }

        public string Address => $"{Street} {City} {State} {PostCode}".Trim();
        public int SecurityCount => Holdings.Count;
        public int Portfolio => Holdings.Select(x => x.Register).Distinct().Count();
        public decimal TotalUnit => Holdings.Sum(x => x.Units);
        public bool IsSubscribed => ExpiryDate > Tools.Now;
        public int TotalDays => IsSubscribed ? (int)ExpiryDate?.Subtract((DateTime)StartDate).TotalDays : 0;
        public int DaysSpent => (int)Math.Ceiling(Tools.Now.Subtract((DateTime)StartDate).TotalDays);
        public int DaysLeft => (int)ExpiryDate?.Subtract(Tools.Now).TotalDays;
        public double Percentage => DaysSpent * 100 / TotalDays;
    }

    public partial class StockBroker
    {
        public bool IsSubscribed => ExpiryDate > Tools.Now;
    }

    public partial class Register
    {
    }

    public partial class Post
    {
        [NotMapped]
        public Clear.EditorJS.Content EditorContent =>
            string.IsNullOrEmpty(Html) ? null :
            System.Text.Json.JsonSerializer.Deserialize<Clear.EditorJS.Content>(Html);

        [NotMapped]
        public List<Clear.EditorJS.Block> Blocks => EditorContent.blocks?.ToList();

        [NotMapped]
        public string Content => EditorContent == null ? "" :
            Clear.Tools.StringUtility.ParseEditorJS(EditorContent);

        [NotMapped]
        public string BlocksJSON => Blocks == null ? "" :
            System.Text.Json.JsonSerializer.Serialize(Blocks);
    }

    public partial class Faq
    {
        [NotMapped]
        public string Content
        {
            get => Clear.Tools.StringUtility.CreateParagraphsFromReturns(Html);
            set => Html = Clear.Tools.StringUtility.CreateReturnsFromParagraphs(value);
        }
    }

    public partial class Payment
    {
        [NotMapped]
        public PayStack.Response PayStackResponse => (Gateway == PaymentGateway.PayStack) ?
            JsonSerializer.Deserialize<PayStack.Response>(Response) : null;

        [NotMapped]
        public BankPayModel BankPaymentDetails => (Gateway == PaymentGateway.Bank) ?
            JsonSerializer.Deserialize<BankPayModel>(Response) : null;

        [NotMapped]
        public string PaidTo => Gateway switch
        {
            PaymentGateway.Bank => $"Bank - {BankPaymentDetails.Account}",
            PaymentGateway.PayStack => $"PayStack - {PayStackResponse.data.channel}",
            _ => "Unknown"
        };

        [NotMapped]
        public string PayRef => Gateway switch
        {
            PaymentGateway.Bank => BankPaymentDetails.Reference,
            PaymentGateway.PayStack => PayStackResponse.data.reference,
            _ => ""
        };

        [NotMapped]
        public int GatewayAmount => (int)(Amount * 100);

        [NotMapped]
        public bool NeedsConfimation => Gateway == PaymentGateway.Bank && Status == PaymentStatus.pending;

        [NotMapped]
        public bool Complete => Status == PaymentStatus.successful;

        [NotMapped]
        public string CssColorClass => Status switch
        {
            PaymentStatus.successful => "success",
            PaymentStatus.failed => "danger",
            PaymentStatus.cancelled => "warning",
            _ => "light"
        };
    }

    public partial class ECertRequest
    {
        [NotMapped]
        public string Color
        {
            get
            {
                return Status switch
                {
                    ECertStatus.Pending => "warning",
                    ECertStatus.Downloaded => "info",
                    ECertStatus.Completed => "success",
                    _ => "default",
                };
            }
        }
    }

    public partial class Ticket
    {
        [NotMapped]
        public string Message => Messages.Count > 0 ? Messages.First().Body : "";
    }

    public partial class ShareOffer
    {
        public decimal? GetPrice(ShareSubscriptionType type) => type switch
        {
            ShareSubscriptionType.PublicOffer => PublicOffer.Price,
            ShareSubscriptionType.RightIssue => RightIssue.Price,
            _ => null
        };
    }

    public partial class ShareSubscription
    {
        [NotMapped]
        public string Code => Response?.UniqueKey;
        
        [NotMapped]
        public decimal Amount => NoOfShares * Offer?.GetPrice(Type) ?? 0;

        [NotMapped]
        public string TypeDescription => Type switch
        {
            ShareSubscriptionType.PublicOffer => "Public offer",
            ShareSubscriptionType.RightIssue => "Right Issue",
            _ => throw new NotImplementedException("Unrecognized subscription type")
        };
    }
}
