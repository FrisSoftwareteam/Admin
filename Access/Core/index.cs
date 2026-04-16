using FirstReg.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstReg;

public class ErrorViewModel
{
    public string RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class UserModel
{
    public UserModel() { }
    public UserModel(User user)
    {
        Id = user.Id;
        FirstName = user.FirstName;
        LastName = user.LastName;
        //FullName = user.FullName;
        MobileNo = user.PhoneNumber;
        Email = user.Email;
        Username = user.UserName;
        EmailConfirmed = user.EmailConfirmed;
        PhoneConfirmed = user.PhoneNumberConfirmed;

        if (user.Shareholders.Count > 0)
        {
            Street = user.Shareholders.First().Street;
            City = user.Shareholders.First().City;
            State = user.Shareholders.First().State;
            Country = user.Shareholders.First().Country;
            SecondaryPhone = user.Shareholders.First().SecondaryPhone;
            PostCode = user.Shareholders.First().PostCode;
            Signature = user.Shareholders.First().Signature;
        }

        if (user.StockBroker != null)
        {
            Street = user.StockBroker.Street;
            City = user.StockBroker.City;
            State = user.StockBroker.State;
            SecondaryPhone = user.StockBroker.SecondaryPhone;
            Fax = user.StockBroker.Fax;
        }
    }

    public int Id { get; set; }
    public UserType Type { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string SecondaryPhone { get; set; }
    public string PostCode { get; set; }
    public string ClearingNo { get; set; }
    public string Fax { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }

    public string Signature { get; set; }
}

public class ClearingNoModel
{
    public int Id { get; set; }
    public string ClearingNo { get; set; }
}

public class ShareholderModel
{
    public ShareholderModel() { }
    public ShareholderModel(Shareholder shareholder)
    {
        Id = shareholder.Id;
        UserId = (int)shareholder.UserId;
        FirstName = shareholder.FirstName;
        LastName = shareholder.LastName;
        //FullName = shareholder.FullName;
        Email = shareholder.User.Email;
        Username = shareholder.User.UserName;
        EmailConfirmed = shareholder.User.EmailConfirmed;
        PhoneConfirmed = shareholder.User.PhoneNumberConfirmed;

        Code = shareholder.Code;
        ClearingNo = shareholder.ClearingNo;
        Street = shareholder.Street;
        City = shareholder.City;
        State = shareholder.State;
        Country = shareholder.Country;
        MobileNo = shareholder.PrimaryPhone;
        SecondaryPhone = shareholder.SecondaryPhone;
        PostCode = shareholder.PostCode;
        Signature = shareholder.Signature;

        IsGroup = shareholder.User.AllowGroup;

        ActionRequired = shareholder.ActionRequired;
        TicketId = shareholder.TicketId;

        IsVerified = shareholder.Verified;
        VerifiedOn = shareholder.VerifiedOn;
        VerifiedBy = shareholder.VerifiedBy;

        StartDate = shareholder.StartDate;
        ExpiryDate = shareholder.ExpiryDate;
        IsSubscribed = shareholder.IsSubscribed;

        Securities = shareholder.Holdings.Select(x => new SecurityModel(x)).ToList();

        LastUpdate = shareholder.LastUpdate;

        ActivationSteps =
        [
            new ActivationStep("Complete Registration", "Register by completing the sin-up form with your basic and contact details, and choose a password.", true, false, "", ""),
            new ActivationStep("Confirm your Account", "Validate your email exits using an automatically generated validation code.", shareholder.User.EmailConfirmed, false, "Confirm", ""),
            new ActivationStep("Update your Clearing number", "Add your Clearing number to your profile as part of your verification process.", !string.IsNullOrEmpty(shareholder.ClearingNo), true, "", "chn"),
            new ActivationStep("Add your Signature", "Add your signature to your profile either by signing online or uploading one.", !string.IsNullOrEmpty(shareholder.Signature), false, "Sign", ""),
        ];
        ActivationSteps.AddRange([
            new ActivationStep("Wait for Verification", "Wait for your account to be verified by one of our specialists.", shareholder.Verified, false, "", "", true, (ActivationSteps[2].Status && ActivationSteps[3].Status && !shareholder.Verified)),
            new ActivationStep("Subscribe to Service", "Subscribe to start using your account after verification is complete.", shareholder.IsSubscribed, false, "Subscribe", "", !shareholder.Verified)
        ]);
    }

    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    //public string FullName { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public string Code { get; set; }
    public string ClearingNo { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string SecondaryPhone { get; set; }
    public string PostCode { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public string Signature { get; set; }

    public bool IsGroup { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public string VerifiedBy { get; set; }

    public bool IsSubscribed { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public bool ActionRequired { get; set; }
    public int TicketId { get; set; }

    public string DisabledCSS => IsSubscribed ? "" : "disabled";

    public int TotalDays => IsSubscribed ? (int)ExpiryDate?.Subtract((DateTime)StartDate).TotalDays : 0;
    public int DaysSpent => (int)Math.Ceiling(Tools.Now.Subtract((DateTime)StartDate).TotalDays);
    public int DaysLeft => (int)ExpiryDate?.Subtract(Tools.Now).TotalDays;
    public double Percentage => DaysSpent * 100 / TotalDays;

    public DateTime? LastUpdate { get; set; }

    public Ticket Ticket { get; set; }
    public List<SecurityModel> Securities { get; set; }
    public List<ActivationStep> ActivationSteps { get; set; }
}

public record ActivationStep(
    string Title,
    string Description,
    bool Status,
    bool OpenModal,
    string Action,
    string RouteCode,
    bool NoButton = false,
    bool IsLoading = false,
    string ButtonText = ""
)
{
    public string DisabledCSS => Status ? "disabled" : "";
}

public class StockBrokerModel
{
    public StockBrokerModel() { }
    public StockBrokerModel(StockBroker broker)
    {
        Id = broker.Id;
        FirstName = broker.User.FirstName;
        LastName = broker.User.LastName;
        FullName = broker.User.FullName;
        MobileNo = broker.User.PhoneNumber;
        Email = broker.User.Email;
        Username = broker.User.UserName;
        EmailConfirmed = broker.User.EmailConfirmed;
        PhoneConfirmed = broker.User.PhoneNumberConfirmed;

        Code = broker.Code;
        Street = broker.Street;
        City = broker.City;
        State = broker.State;
        Country = "Nigeria";
        SecondaryPhone = broker.SecondaryPhone;
        Fax = broker.Fax;

        StartDate = broker.StartDate;
        ExpiryDate = broker.ExpiryDate;
        IsSubscribed = broker.IsSubscribed;
    }

    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }

    public string Code { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string SecondaryPhone { get; set; }
    public string Fax { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsSubscribed { get; set; }

    public int TotalDays => IsSubscribed ? (int)ExpiryDate?.Subtract((DateTime)StartDate).TotalDays : 0;
    public int DaysSpent => (int)Math.Ceiling(Tools.Now.Subtract((DateTime)StartDate).TotalDays);
    public int DaysLeft => (int)ExpiryDate?.Subtract(Tools.Now).TotalDays;
    public double Percentage => DaysSpent * 100 / TotalDays;
}

public class GroupModel
{
    public GroupModel() { }
    public GroupModel(User user)
    {
        Id = user.Id;
        FirstName = user.FirstName;
        LastName = user.LastName;
        FullName = user.FullName;
        MobileNo = user.PhoneNumber;
        Email = user.Email;
        Username = user.UserName;
        EmailConfirmed = user.EmailConfirmed;
        PhoneConfirmed = user.PhoneNumberConfirmed;

        if (user.Shareholders.Count > 0)
            Shareholders = user.Shareholders.Select(x => new ShareholderModel(x)).ToList();
    }

    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }

    public string Address { get; set; }
    public DateTime Date { get; set; }

    public List<ShareholderModel> Shareholders { get; set; }
}

public class RegisterModel : UserModel
{
    public string Password { get; set; }
    public string RePassword { get; set; }
    public string ReturnUrl { get; set; }
    public string CheckEmailUrl { get; set; }
    public string ValidateEmailUrl { get; set; }
    public string GenerateValidateEmailUrl { get; set; }
}

public class PasswordModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string ExPassword { get; set; }
    public string Password { get; set; }
    public string RePassword { get; set; }
}

public class ShareHolderDashboardModel
{
    public ShareHolderDashboardModel(User user)
    {
        User = user;

        Shareholders = user.Shareholders.Select(x => new ShareholderModel(x)).ToList();

        var holdings = Shareholders.SelectMany(x => x.Securities).ToList();

        SecurityCount = holdings.Count;
        Portfolio = holdings.Select(x => x.Company).Distinct().Count();
        TotalUnit = holdings.Sum(x => x.Units);
        Securities = holdings;

        IsGroup = user.AllowGroup;
    }

    public bool IsGroup { get; set; }
    public decimal Portfolio { get; set; } = 0;
    public int SecurityCount { get; set; } = 0;
    public decimal TotalUnit { get; set; } = 0;
    public List<SecurityModel> Securities { get; set; } = new();
    public User User { get; set; } = new();
    public List<ShareholderModel> Shareholders { get; set; } = new();
}

public class StockBrokerDashboardModel
{
    public StockBrokerDashboardModel(User user)
    {
        User = user;

        var holdings = user.StockBroker?.ECertRequests.ToList();

        if (holdings != null)
        {
            Pending = holdings.Count(x => x.Status == ECertStatus.Pending);
            Downloaded = holdings.Count(x => x.Status == ECertStatus.Downloaded);
            Completed = holdings.Count(x => x.Status == ECertStatus.Completed);
            Total = holdings.Count;
            Requests = holdings;
        }
    }

    public User User { get; set; }
    public int Pending { get; set; } = 0;
    public int Downloaded { get; set; } = 0;
    public int Completed { get; set; } = 0;
    public int Total { get; set; } = 0;
    public double PercentComplete => (Total > 0) ? (Completed / (double)Total * 100) : 0;
    public List<ECertRequest> Requests { get; set; } = new();
}

public class CompSecDashboardModel
{
    public CompSecDashboardModel(Register register)
    {
        Register = register;
    }

    public Register Register { get; set; }
}

public class FRAdminDashboardModel
{
    public FRAdminDashboardModel(User user)
    {
        User = user;
    }

    public User User { get; set; }
}

public class SecurityModel
{
    public SecurityModel() { }
    public SecurityModel(ShareHolding holding)
    {
        ShareholderCode = holding.Shareholder.Code;
        Company = holding.Register.Name;
        AccountNo = holding.AccountNo;
        Holder = holding.AccountName;
        Status = holding.Status;
        Units = holding.Units;
    }

    public string ShareholderCode { get; set; }
    public string Company { get; set; }
    public string AccountNo { get; set; }
    public string Holder { get; set; }
    public ShareHoldingStatus Status { get; set; }
    public decimal Units { get; set; }
}
public class SecurityDetailsModel : SecurityModel
{
    public SecurityDetailsModel()
    {
        Transactions = new();
    }

    public SecurityDetailsModel(ShareHolding holding, Bson.Holding bholding) : base(holding)
    {
        decimal initial = 0;

        Transactions = new();

        foreach (var item in bholding.Units.OrderBy(x => Convert.ToDateTime(x.Date)))
        {
            initial += item.TotalUnits;
            Transactions.Add(new()
            {
                AccountNo = item.AccountNo.ToString(),
                Date = Convert.ToDateTime(item.Date),
                Transaction = item.Description,
                Reference = item.Description,
                UnitsIn = item.TotalUnits > 0 ? item.TotalUnits : 0,
                UnitsOut = item.TotalUnits < 0 ? Math.Abs(item.TotalUnits) : 0,
                Balance = initial,
                Status = "Active"
            });
        }

        Dividends = bholding.Dividends.OrderBy(x => Convert.ToDateTime(x.Date)).Select(x => new DividendHistoryItem(
            x.DividendNo.ToString(), x.Type, x.WarrantNo.ToString(), Convert.ToDateTime(x.Date),
            (decimal)x.Total, (decimal)x.Gross, (decimal)x.Tax, (decimal)x.Net)).ToList();
    }

    public List<SecurityDetailsItem> Transactions { get; set; }
    public List<DividendHistoryItem> Dividends { get; set; }

    public decimal TotalnitsIn => Transactions.Sum(x => x.UnitsIn);
    public decimal TotalnitsOut => Transactions.Sum(x => x.UnitsOut);
    public decimal UnitsBalance => TotalnitsIn - TotalnitsOut;
    public decimal TotalDividend => Dividends.Sum(x => x.Gross);

    public string TotalnitsInString => Tools.Shorten(TotalnitsIn);
    public string TotalnitsOutString => Tools.Shorten(TotalnitsOut);
    public string UnitsBalanceString => Tools.Shorten(UnitsBalance);
    public string TotalDividendString => Tools.Shorten(TotalDividend);
}
public class SecurityDetailsItem
{
    public string AccountNo { get; set; }
    public string MergeAccount { get; set; }
    public string Transaction { get; set; }
    public string Reference { get; set; }
    public string Status { get; set; }
    public DateTime Date { get; set; }
    public decimal UnitsIn { get; set; }
    public decimal UnitsOut { get; set; }
    public decimal Units => UnitsIn - UnitsOut;
    public string UnitsString => UnitsIn > UnitsOut ? $"+{Units:N0}" : Units.ToString("N0");
    public string Description => UnitsIn > UnitsOut ? "Bought" : "Sold";
    public string UnitsCSS => UnitsIn > UnitsOut ? "success" : "danger";
    public decimal Balance { get; set; }
}

public record DividendHistoryItem(string DivNo, string Type, string WarrantNo,
    DateTime Date, decimal Holding, decimal Gross, decimal Tax, decimal Net);

public record ComapnyModel(int Id, string Code, string Name, string Email,
    string Phone, string Address, string RCNo, string Website,
    string Description, string Symbol)
{
    public ComapnyModel(Register register) : this(register.Id, register.Code,
        register.Name, register.Email, register.Phone, register.Address,
        register.RCNo, register.Website, register.Description, register.Symbol)
    {
        Register = register;
        Items = register.Dividends.Select(x =>
            new ComapnyHistoryItem(x.PaymentNo, x.YearEnd, x.DatePayable, x.AmountDeclared, x.ClosureDate)).ToList();
    }
    public Register Register { get; }
    public List<ComapnyHistoryItem> Items { get; }
}

public record ComapnyHistoryItem(string PaymentNo,
    DateTime YearEnd, DateTime Date, decimal Amount, DateTime Closure);

public class SubscribeModel
{
    public User User { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; }
    public PaymentSettings PaymentSettings { get; set; }
    public List<int> AccountIds { get; set; }
}

public record ShareSubscriptionPageModel
{
    public ShareSubscriptionPageModel(ShareSubscription subscription)
    {
        bool isPaid = !string.IsNullOrEmpty(subscription.PaymentId);

        Subscription = subscription;
        Steps = [
            new ActivationStep(
                "Fill the form",
                $"Completed {subscription.TypeDescription} application for {subscription.NoOfShares} units in {subscription.Offer.Description}",
                true, false, "", "", true, false
            ),
            new ActivationStep(
                "Complete Payment",
                $"Continue to make the payment of ₦{subscription.Amount:N2} to complete your {subscription.TypeDescription} subscription",
                subscription.Payment?.Complete ?? false, false, "Pay", subscription.Response.UniqueKey, 
                isPaid, subscription.Payment?.NeedsConfimation ?? false, "Pay Now"
            )
        ];
    }

    public ShareSubscription Subscription { get; }
    public List<ActivationStep> Steps { get; }
}

public record PaymentModel
{
    public PaymentItem PaymentItem { get; private set; }
    public string Name { get; private set; }
    public string EmailAddress { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public string Reference { get; private set; }
    public string PreviousPayment { get; private set; }
    public bool HasPreviousPayment => !string.IsNullOrEmpty(PreviousPayment);
    public PaymentSettings PaymentSettings { get; private set; }

    public static PaymentModel Create(PaymentItem paymentItem, string name, string emailAddress, string phoneNumber,
        string description, decimal amount, string reference, PaymentSettings settings, string previousPayment)
    {
        return new PaymentModel
        {
            PaymentItem = paymentItem,
            Name = name,
            EmailAddress = emailAddress,
            PhoneNumber = phoneNumber,
            Description = description,
            Amount = amount,
            Reference = reference,
            PaymentSettings = settings,
            PreviousPayment = previousPayment
        };
    }
}