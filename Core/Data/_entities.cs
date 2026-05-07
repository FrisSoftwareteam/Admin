using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstReg.Data;

#region identity
public partial class User : IdentityUser<int>
{
    public User()
    {
        Shareholders = new HashSet<Shareholder>();
        Payments = new HashSet<Payment>();
        AccessRoles = new HashSet<AccessRole>();
        Subscriptions = new HashSet<Subscription>();
    }

    public UserType Type { get; set; }
    public string FullName { get; set; }
    public bool AllowGroup { get; set; }

    public virtual Register Register { get; set; }
    public virtual StockBroker StockBroker { get; set; }

    public virtual ICollection<Shareholder> Shareholders { get; set; }
    public virtual ICollection<Payment> Payments { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; }
    public virtual ICollection<Message> Messages { get; set; }
    public virtual ICollection<AccessRole> AccessRoles { get; set; }
    public virtual ICollection<Subscription> Subscriptions { get; set; }
}
public partial class Role : IdentityRole<int>
{
    //public virtual ICollection<UserRole> UserRoles { get; set; }
}
public partial class UserRole : IdentityUserRole<int>
{
}
#endregion

#region real deal

public partial class Shareholder
{
    public Shareholder()
    {
        Holdings = new HashSet<ShareHolding>();
    }
    public int Id { get; set; }
    public int? UserId { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string Code { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string FullName { get; set; }

    [Required]
    [Column(TypeName = "varchar(350)")]
    public string Street { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string City { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string State { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Country { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string PrimaryPhone { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string SecondaryPhone { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string PostCode { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpiryDate { get; set; }


    [Column(TypeName = "varchar(MAX)")]
    public string Signature { get; set; }

    public bool Verified { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string ClearingNo { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? VerifiedOn { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string VerifiedBy { get; set; }

    public bool Downloaded { get; set; } = false;

    public bool IsCompany { get; set; }

    public bool ActionRequired { get; set; } = false;
    public int TicketId { get; set; } = 0;


    public int? LegacyAccId { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string LegacyId { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string LegacyUsername { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string MAccessPin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedOn { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string CardId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdate { get; set; }

    public virtual User User { get; set; }

    public virtual ICollection<ShareHolding> Holdings { get; set; }
}

public partial class Register
{
    public Register()
    {
        Holdings = new HashSet<ShareHolding>();
        Dividends = new HashSet<Dividend>();
    }
    public int Id { get; set; } // prc_no
    public int? UserId { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string Code { get; set; } // Reg_Code
    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Name { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Email { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Phone { get; set; }

    //[Column(TypeName = "varchar(50)")]
    //public string Fax { get; set; }

    [Required]
    [Column(TypeName = "varchar(350)")]
    public string Address { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string RCNo { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Website { get; set; }

    //[Required]
    //[Column(TypeName = "varchar(30)")]
    //public string Type { get; set; } // prc_types
    //[Column(TypeName = "datetime")]
    //public DateTime? IncorporatedOn { get; set; }

    //[Column(TypeName = "datetime")]
    //public DateTime? ListedOn { get; set; }

    //[Required]
    //[Column(TypeName = "datetime")]
    //public DateTime CreatedOn { get; set; }

    //[Required]
    //[Column(TypeName = "varchar(50)")]
    //public string CreatedBy { get; set; }

    //[Required]
    //[Column(TypeName = "datetime")]
    //public DateTime UpdatedOn { get; set; }

    //[Required]
    //[Column(TypeName = "varchar(50)")]
    //public string UpdatedBy { get; set; }

    [Column(TypeName = "varchar(300)")]
    public string Description { get; set; } // register_desc
    //public int? NomValue { get; set; } // nomvalue
    //[Column(TypeName = "varchar(25)")]
    //public string SecurityType { get; set; }
    //public double ActualShares { get; set; }

    //[Column(TypeName = "varchar(250)")]
    //public string Caution { get; set; }
    public bool Active { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string Symbol { get; set; }
    //public bool Fraction { get; set; }

    //[Column(TypeName = "varchar(500)")]
    //public string Narration { get; set; } // fund_cert_narr

    //public int? StatusId { get; set; }
    //public int? Decimal { get; set; } // nDecimal

    public virtual User User { get; set; }
    public virtual ICollection<ShareHolding> Holdings { get; set; }
    public virtual ICollection<Dividend> Dividends { get; set; }
    public virtual ICollection<ECertHolding> ECertHoldings { get; set; }
}

public partial class ShareHolding
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public int ShareHolderId { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string AccountNo { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string AccountName { get; set; }
    public decimal Units { get; set; }

    [Column(TypeName = "money")]
    public decimal Value { get; set; }
    public ShareHoldingStatus Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public virtual Shareholder Shareholder { get; set; }
    public virtual Register Register { get; set; }
}

public partial class PendingShareHolding
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public int ShareHolderId { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string AccountNo { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string AccountName { get; set; }
    public decimal Units { get; set; }

    public ShareHoldingStatus Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public virtual Shareholder Shareholder { get; set; }
    public virtual Register Register { get; set; }
}

public partial class Dividend
{
    public int Id { get; set; }
    public int RegisterId { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string PaymentNo { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string Description { get; set; }

    [Column(TypeName = "money")]
    public decimal AmountDeclared { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime YearEnd { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DatePayable { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ClosureDate { get; set; }

    public virtual Register Register { get; set; }
}

#endregion

#region company data

public partial class RegisterHolding
{
    public int Id { get; set; }
    public int RegisterId { get; set; }

    public int AccountNo { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string ClearingNo { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Name { get; set; }

    [Required]
    [Column(TypeName = "varchar(6)")]
    public string Gender { get; set; }

    [Required]
    [Column(TypeName = "varchar(750)")]
    public string Address { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Email { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Phone { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Mobile { get; set; }

    public decimal Units { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }
}

#endregion

#region payments

public partial class Payment
{
    public Payment()
    {
        //SubscriptionPayments = new HashSet<SubscriptionPayment>();
    }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal Amount { get; set; }

    public Currency Currency { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentGateway Gateway { get; set; }
    public PaymentItem Item { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Description { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Response { get; set; }

    [Required]
    [Column(TypeName = "varchar(500)")]
    public string Remarks { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Updated { get; set; }

    public virtual User User { get; set; }

    public static Payment CreateForSubscription(BankPayModel model, User user, DateTime date)
    {
        return CreatePayment(PaymentItem.Subscription, model.Id, model.Amount, date, user.Id,
            $"Bank payment #{model.Id} for Online Access Subscription for {user.FullName}", System.Text.Json.JsonSerializer.Serialize(model));
    }

    public static Payment CreateForShareOffer(BankPayModel model, ShareSubscriptionType type, string offerDescription, string name, DateTime date)
    {
        return CreatePayment(PaymentItem.Subscription, model.Id, model.Amount, date, Tools.AdminId,
            $"Bank payment #{model.Id} for {offerDescription} {type}", System.Text.Json.JsonSerializer.Serialize(model));
    }

    private static Payment CreatePayment(PaymentItem payItem, string id, 
        decimal amount, DateTime date, int userId, string description, string jsonResponse)
    {
        return new Payment
        {
            Id = id,
            Currency = Currency.NGN,
            Date = date,
            Updated = date,
            Gateway = PaymentGateway.Bank,
            Status = PaymentStatus.pending,
            UserId = userId,
            Description = description,
            Response = jsonResponse,
            Item = payItem,
            Amount = amount,
            Remarks = "Awaiting Confirmation"
        };
    }
}

#endregion

#region posts
public partial class Author
{
    public Author() => Posts = new HashSet<Post>();

    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Name { get; set; }

    [Required]
    [Column(TypeName = "varchar(500)")]
    public string Bio { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Avatar { get; set; }

    public virtual ICollection<Post> Posts { get; set; }
}
public partial class PostCategory
{
    public PostCategory() => Posts = new HashSet<Post>();

    public int Id { get; set; }
    public PostType Type { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Code { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Description { get; set; }

    public virtual ICollection<Post> Posts { get; set; }
}
public partial class Post
{
    public int Id { get; set; }
    public PostType Type { get; set; }

    public int CategoryId { get; set; }
    public int AuthorId { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Code { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Title { get; set; }

    [Required]
    [Column(TypeName = "varchar(750)")]
    public string Brief { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Html { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Thumb { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public int Views { get; set; } = 0;
    public bool Promoted { get; set; }

    public virtual PostCategory Category { get; set; }
    public virtual Author Author { get; set; }
}
#endregion

#region faq
public partial class FaqSection
{
    public FaqSection() => Faqs = new HashSet<Faq>();

    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Description { get; set; }

    public virtual ICollection<Faq> Faqs { get; set; }
}
public partial class Faq
{
    public int Id { get; set; }

    public int SectionId { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Question { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Html { get; set; }

    public virtual FaqSection Section { get; set; }
}
#endregion

#region tickets

public partial class Ticket
{
    public Ticket()
    {
        Messages = new HashSet<Message>();
    }

    public int Id { get; set; }
    public int UserId { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Code { get; set; }

    [Required]
    [Column(TypeName = "varchar(500)")]
    public string Subject { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public bool Resolved { get; set; }

    public virtual User User { get; set; }
    public virtual ICollection<Message> Messages { get; set; }
}

public partial class Message
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int TicketId { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Code { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Body { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public bool IsRead { get; set; }

    public virtual User Sender { get; set; }
    public virtual Ticket Ticket { get; set; }
}

#endregion

#region others

public partial class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string Code { get; set; }

    public SubscriptionType Type { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime EndDate { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal AmountPaid { get; set; }
    public PaymentType PaymentType { get; set; }

    public int AccountId { get; set; }
    public bool Confirmed { get; set; }

    public virtual User User { get; set; }
}

public partial class AnnualReport
{
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(150)")]
    public string Description { get; set; }

    [Required]
    [Column(TypeName = "varchar(150)")]
    public string FileName { get; set; }
}

public partial class SubscriptionPlan
{
    public SubscriptionType Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(150)")]
    public string Description { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal Price { get; set; }
}

public partial class AccessRole
{
    public int UserId { get; set; }
    public Roles Role { get; set; }

    public virtual User User { get; set; }
}

public partial class LastId
{
    public int Id { get; set; }
    public int RegisterHolding { get; set; } = 0;
    public int ShareHolders { get; set; } = 0;
}

#endregion

#region Certificate requests

public partial class StockBroker
{
    public StockBroker()
    {
        ECertRequests = new HashSet<ECertRequest>();
    }
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string Code { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string SecondaryPhone { get; set; }

    [Required]
    [Column(TypeName = "varchar(350)")]
    public string Street { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string City { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string State { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Fax { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpiryDate { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime CreatedOn { get; set; }

    public virtual User User { get; set; }
    public virtual ICollection<ECertRequest> ECertRequests { get; set; }
}

public partial class ECertRequest
{
    public ECertRequest() => ECertHolders = new HashSet<ECertHolder>();

    public int Id { get; set; }
    public int StockBrokerId { get; set; }

    [Required]
    [Column(TypeName = "varchar(25)")]
    public string Code { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string Description { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Brief { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string AuthLetterFileName { get; set; }

    public ECertStatus Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public virtual StockBroker StockBroker { get; set; }
    public virtual ICollection<ECertHolder> ECertHolders { get; set; }
}

public partial class ECertHolder
{
    public ECertHolder()
    {
        ECertHoldings = new HashSet<ECertHolding>();
    }

    public int Id { get; set; }
    public int RequestId { get; set; }

    [Required]
    [Column(TypeName = "varchar(250)")]
    public string FullName { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string IdFileName { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string PhotoFileName { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string Signature { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public virtual ECertRequest ECertRequest { get; set; }
    public virtual ICollection<ECertHolding> ECertHoldings { get; set; }
}

public partial class ECertHolding
{
    public int Id { get; set; }
    public int HolderId { get; set; }
    public int RegisterId { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string ClearingNo { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string CertificateNo { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string AccountNo { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal Units { get; set; }

    //[Required]
    //[Column(TypeName = "varchar(100)")]
    //public string CertFileName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public virtual ECertHolder ECertHolder { get; set; }
    public virtual Register Register { get; set; }
}

#endregion

public partial class Contact
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public partial class ShareOffer
{
    public int Id { get; set; }

    public int RegisterId { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string UniqueKey { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EndDate { get; set; }

    public bool AllowPublicOffer { get; set; }
    public bool AllowRightIssue { get; set; }

    [Required]
    public ShareOfferPublic PublicOffer { get; set; }

    [Required]
    public ShareOfferRights RightIssue { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public virtual Register Register { get; set; }
    public virtual ICollection<ShareSubscription> Subscriptions { get; set; }
}

[Owned]
public partial class ShareOfferRights
{
    public int NoOfShares { get; set; }
    public int Rights { get; set; }
    public int Factor { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal Price { get; set; }
}

[Owned]
public partial class ShareOfferPublic
{
    public int Minimum { get; set; }
    public int Factor { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal Price { get; set; }
}

public partial class ShareSubscription
{
    public int Id { get; set; }

    public ShareSubscriptionType Type { get; set; }
    public int ShareOfferId { get; set; }
    public int FormResponseId { get; set; }

    public int NoOfShares { get; set; }
    public int Rights { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string PaymentId { get; set; }

    public virtual ShareOffer Offer { get; set; }
    public virtual FormResponse Response { get; set; }
    public virtual Payment Payment { get; set; }

    public static ShareSubscription Create(int offerId, ShareSubscriptionType subscriptionType, 
        int noOfShares, int rights, FormResponse formResponse)
    {
        return new ShareSubscription 
        { 
            ShareOfferId = offerId,
            Type = subscriptionType,
            Rights = rights,
            NoOfShares = noOfShares,
            Response = formResponse
        };
    }
}

public partial class FormResponse
{
    public int Id { get; set; }
    public Forms Type { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string UniqueKey { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string ValidationCode { get; set; }

    [Required]
    [Column(TypeName = "varchar(500)")]
    public string FullName { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string EmailAddress { get; set; }

    [Required]
    [Column(TypeName = "varchar(100)")]
    public string PhoneNumber { get; set; }

    [Required]
    [Column(TypeName = "varchar(MAX)")]
    public string JsonData { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public int DownloadCount { get; set; }
    public bool Processed { get; set; }

    public static FormResponse Create(string key, Forms type, string jsonData, 
        string name, string phoneNumber, string emailAddress, DateTime date)
    {
        return new FormResponse
        {
            UniqueKey = key,
            Type = type,
            JsonData = jsonData,
            FullName = name,
            PhoneNumber = phoneNumber,
            EmailAddress = emailAddress,
            Date = date
        };
    }

    public T GetData<T>() where T : IFormModel
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(JsonData);
    }
}

public partial class AuditLog
{
    private AuditLog(AuditLogSection section, AuditLogType type, 
        int userId, string description, DateTime date)
    {
        Section = section;
        Type = type;
        UserId = userId;
        Description = description;
        Date = date;
    }

    public int Id { get; private set; }
    public AuditLogSection Section { get; private set; }
    public AuditLogType Type { get; private set; }
    public int UserId { get; private set; }

    [Required]
    [Column(TypeName = "varchar(1000)")]
    public string Description { get; private set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; private set; }


    public static AuditLog Create(AuditLogSection section, AuditLogType type, 
        int userId, string description, DateTime date)
    {
        return new(section, type, userId, description, date);
    }
}