using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstReg.Data;

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}
public class ResetModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string RePassword { get; set; }
    public string Key { get; set; }
}
public class UserResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string MobileNo { get; set; }
    public string Token { get; set; }
}
public class AccountItemModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string AccountNo { get; set; }
    public int RegisterId { get; set; }
    public int Units { get; set; }
    public decimal Amount { get; set; }
}
public class CertRequestModel
{
    public int Id { get; set; }
    public string Description { get; set; }
    public string Brief { get; set; }
}
public class CertRequestHolderModel
{
    public int RequestId { get; set; }
    public string FullName { get; set; }
    public string[] Description { get; set; }
}
public class SignatureModel
{
    public SignatureModel() { }
    public SignatureModel(int id, string signature)
    {
        Id = id;
        Signature = signature;
    }

    public int Id { get; set; }
    public string Signature { get; set; }
}

public record PaymentSettings(string PublicKey, string SecretKey, string InlineUrl, string QueryUrl);

public class TicketModel
{
    public int UserId { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
}

public class MessageModel
{
    public int UserId { get; set; }
    public int TicketId { get; set; }
    public string Message { get; set; }
}

public class BankPayModel
{
    public string Id { get; set; }
    public string Payee { get; set; }
    public string Reference { get; set; }
    public string Account { get; set; }
    public string User { get; set; }
    public DateTime Date { get; set; }
    public int Years { get; set; }
    public decimal Amount { get; set; }
    public string AccountIds { get; set; }
}

public class RegisterHolderModel
{
    public RegisterHolderModel(RegSH sh)
    {

        Id = sh.Id;
        RegisterId = sh.RegCode;
        Register = sh.Register;
        AccountNo = sh.AccountNo;
        ClearingNo = sh.ClearingNo;
        Name = sh.FullName;
        Gender = sh.Gender;
        Address = sh.Address;
        Email = sh.Email;
        Phone = sh.Phone;
        Mobile = sh.Mobile;
        TotalUnits = sh.TotalUnits;
        Date = DateTime.Now;

        Units = sh.Units.OrderBy(x => x.Id).ToList();
        Dividends = sh.Dividends.OrderBy(x => x.Id).ToList();
    }

    public int Id { get; }
    public int RegisterId { get; }
    public string Register { get; }
    public int AccountNo { get; }
    public string ClearingNo { get; }
    public string Name { get; }
    public string Gender { get; }
    public string Address { get; }
    public string Email { get; }
    public string Phone { get; }
    public string Mobile { get; }
    public decimal TotalUnits { get; }
    public DateTime Date { get; }

    public List<Bson.Unit> Units { get; }
    public List<Bson.Dividend> Dividends { get; }
}

public record AccountNameModel(
    string AccountNo, string Name, string Address, string PhoneNo
);

#region forms

public interface IFormModel
{
    string Id { get; set; }
    string Signature { get; set; }
    string FullName { get; }
    string Phone { get; set; }
    string Email { get; set; }
    string ClearingNo { get; set; }
    List<DataHoldingModel> Holdings { get; set; }

    bool RequiresSignature { get; }
    bool RequiresHoldings { get; }
}

public abstract class BaseFormModel : IFormModel
{
    protected BaseFormModel()
    {
        RequiresSignature = true;
        RequiresHoldings = true;
    }

    protected BaseFormModel(bool requiresSignature, bool requiresHoldings)
    {
        RequiresSignature = requiresSignature;
        RequiresHoldings = requiresHoldings;
    }

    public bool RequiresSignature { get; }
    public bool RequiresHoldings { get; }

    public string Id { get; set; }
    public string LastName { get; set; }
    public string OtherName { get; set; }
    public string ClearingNo { get; set; }
    public string FullName => $"{OtherName} {LastName}".Trim();
    public string Signature { get; set; }

    public string Phone { get; set; }
    public string Email { get; set; }
    public List<DataHoldingModel> Holdings { get; set; }
}

public class DataUpdateModel : BaseFormModel
{
    public Gender Gender { get; set; }
    public AgeRange AgeRange { get; set; }
    public DataAddressModel NewAddress { get; set; }
    public DataAddressModel PreviousAddress { get; set; }
    public string Mobile { get; set; }
    public string Nationality { get; set; }
    public string NIN { get; set; }
    public string TIN { get; set; }
    public string StateOfOrigin { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string NextOfKin { get; set; }
    public string NextOfKinPhone { get; set; }
    public string CompanySeal { get; set; }
}

public class EDividendModel : BaseFormModel
{
    public BankAccountType BankAccountType { get; set; }
    public DataAddressModel NewAddress { get; set; }
    public DataAddressModel PreviousAddress { get; set; }
    public string Mobile { get; set; }
    public string BVN { get; set; }
    public string BankName { get; set; }
    public string BankBranch { get; set; }
    public string BankAddress { get; set; }
    public string BankAccountNumber { get; set; }
    public DateTime BankAccountOpenedOn { get; set; }
    public string CompanySeal { get; set; }
}

public class DataOtherApplicantModel
{
    public string Title { get; set; }
    public string LastName { get; set; }
    public string OtherName { get; set; }
}

public class ChangeOfAddressModel : BaseFormModel
{
    public DataAddressModel NewAddress { get; set; }
    public DataAddressModel PreviousAddress { get; set; }
    public string Mobile { get; set; }
    public string CompanySeal { get; set; }
}

public class ShareholderUpdateModel : BaseFormModel
{
    public ShareholderUpdateModel() : base(false, false) { }

    public string StockBroker { get; set; }
    public string AccountNo { get; set; }
}

public class DataAddressModel
{
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
}

public class DataHoldingModel
{
    public DataHoldingModel(int regCode, string accountNo)
    {
        RegCode = regCode;
        AccountNo = accountNo;
    }
    public int RegCode { get; set; }
    public string AccountNo { get; set; }
}

public interface IShareFormModel
{
    string Id { get; set; }
    string FullName { get; }
    string Phone { get; set; }
    string Email { get; set; }
    int OfferId { get; set; }
    int NoOfShares { get; set; }
    int Rights { get; set; }
    string Signature { get; set; }
    string LastName { get; set; }
    string OtherName { get; set; }
    string NextOfKin { get; set; }
    string NextOfKinPhone { get; set; }
    string ClearingNo { get; set; }
    string CSCSNumber { get; set; }
    string StockBroker { get; set; }
    string MemberCode { get; set; }
    string BankName { get; set; }
    string BankAccountNumber { get; set; }
    string BankBranch { get; set; }
    string BankCity { get; set; }
    string BankState { get; set; }
    string BVN { get; set; }
    string BVN2 { get; set; }
}

public record PublicOfferModel : IShareFormModel
{
    public string Id { get; set; }

    public required int OfferId { get; set; }
    public int Rights { get; set; }
    public int NoOfShares { get; set; }
    public int Minimum { get; set; }
    public int Factor { get; set; }

    public string Title { get; set; }
    public string LastName { get; set; }
    public string OtherName { get; set; }
    public string FullName => $"{OtherName} {LastName}".Trim();
    public DateOnly DateOfBirth { get; set; }
    public DataAddressModel PostalAddress { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string NextOfKin { get; set; }
    public string NextOfKinPhone { get; set; }
    public string ClearingNo { get; set; }
    public string CSCSNumber { get; set; }
    public string StockBroker { get; set; }
    public string MemberCode { get; set; }
    public DataOtherApplicantModel OtherApplicant { get; set; }
    public string BankName { get; set; }
    public string BankAccountNumber { get; set; }
    public string BVN { get; set; }
    public string BVN2 { get; set; }
    public string BankBranch { get; set; }
    public string BankCity { get; set; }
    public string BankState { get; set; }

    public string Signature { get; set; }
    public string CompanySeal { get; set; }
    public string RCNumber { get; set; }

    public static PublicOfferModel Create(int offerId, int minimum, int factor) => new()
    {
        OfferId = offerId,
        Minimum = minimum,
        Factor = factor,
    };
}

public record RightIssueModel : IShareFormModel
{
    public string Id { get; set; }

    public required int OfferId { get; set; }
    public required int Rights { get; set; }
    public int NoOfShares { get; set; }

    public string LastName { get; set; }
    public string OtherName { get; set; }
    public string FullName => $"{OtherName} {LastName}".Trim();
    public string Phone { get; set; }
    public string Email { get; set; }
    public string NextOfKin { get; set; }
    public string NextOfKinPhone { get; set; }
    public string ClearingNo { get; set; }
    public string CSCSNumber { get; set; }
    public string StockBroker { get; set; }
    public string MemberCode { get; set; }
    public string BankName { get; set; }
    public string BankAccountNumber { get; set; }
    public string BVN { get; set; }
    public string BVN2 { get; set; }
    public string BankBranch { get; set; }
    public string BankCity { get; set; }
    public string BankState { get; set; }
    public string Signature { get; set; }
    public decimal TotalUnits { get; set; }
}

public class FindShareSubscriptionModel
{
    public string Query { get; set; }
    //public string Phone { get; set; }
    //public string Email { get; set; }
}

#endregion