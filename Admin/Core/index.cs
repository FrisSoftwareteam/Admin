using FirstReg.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        MobileNo = user.PhoneNumber;
        Email = user.Email;
        Username = user.UserName;
        EmailConfirmed = user.EmailConfirmed;
        PhoneConfirmed = user.PhoneNumberConfirmed;
    }

    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public UserType Type { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }

}

public class ShareHolderModel : UserModel
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string SecondaryPhone { get; set; }
    public string PostCode { get; set; }
    public string ClearingNo { get; set; }
    public string Fax { get; set; }
}

public class DashboardModel
{
    public string ThisMonth { get; set; }
    public int Percentage { get; set; }
    public List<decimal> MonthlySales { get; set; }
    public string TotalSales => Tools.Shorten(MonthlySales.Sum(x => x));
    public int Shareholders { get; set; }
    public int StockBrokers { get; set; }
    public int CompanySecs { get; set; }
    public int FRAdmins { get; set; }
    public int SystemAdmins { get; set; }
    public string TotalUsers => Tools.Shorten((double)(Shareholders + StockBrokers + CompanySecs + FRAdmins + SystemAdmins));
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int Never { get; set; }
    public string TotalSubscribers => Tools.Shorten((double)(Active + Inactive + Never));
    public List<DashUser> Users { get; set; }
    public List<DashCert> CertRequests { get; set; }
}

public record DashUser(string Name, string Email, UserType Type)
{
    public string Initial
    {
        get
        {
            if (string.IsNullOrEmpty(Name))
                return "!";
            else
            {
                return Name.Substring(0, 1);
            }
        }
    }
}

public record DashCert(string Description, string Name, string Time);

public class PostPageModel : Post
{
    public PostPageModel() { }
    public PostPageModel(PostType type, IEnumerable<PostCategory> categories, IEnumerable<Author> authors)
    {
        Type = type;
        Categories = new SelectList(categories, "Id", "Description");
        Authors = new SelectList(authors, "Id", "Name");
    }
    public PostPageModel(Post post, IEnumerable<PostCategory> categories, IEnumerable<Author> authors) : this(post.Type, categories, authors)
    {
        Id = post.Id;
        Code = post.Code;
        Type = post.Type;
        Title = post.Title;
        Brief = post.Brief;
        Html = post.Html;
        Thumb = post.Thumb;
        Date = post.Date;
        CategoryId = post.CategoryId;
        AuthorId = post.AuthorId;
        Brief = post.Brief;
        Promoted = post.Promoted;
    }
    public SelectList Categories { get; set; }
    public SelectList Authors { get; set; }
}

public class FaqPageModel : Faq
{
    public FaqPageModel() { }
    public FaqPageModel(IEnumerable<FaqSection> sections)
    {
        Sections = new SelectList(sections, "Id", "Description");
    }
    public FaqPageModel(IEnumerable<FaqSection> sections, Faq faq) : this(sections)
    {
        Id = faq.Id;
        SectionId = faq.SectionId;
        Question = faq.Question;
        Html = faq.Html;
    }
    public SelectList Sections { get; set; }
}

public class SubscribeModel
{
    public string Code { get; set; }
    public DateTime StartDate { get; set; }
    public int Years { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PayRef { get; set; }
    public int ShareholderId { get; set; }
}

public class ProfileModel : UserModel
{
    public string ExPassword { get; set; }
    public string NewPassword { get; set; }
    public string RePassword { get; set; }
}

public class ShareholdersRejectModel
{
    public int Id { get; set; }
    public ShareholderActivationIssue Issue { get; set; }
    public string Comments { get; set; }
}

public record ShareOfferPageModel(
    string FetchUrl,
    string DownloadUrl,
    ShareOffer Offer
);

public record ShareOfferModel
{
    public ShareOfferModel() { }
    public ShareOfferModel(ShareOffer offer)
    {
        Id = offer.Id;
        RegisterId = offer.RegisterId;
        RegisterName = offer.Register.Name;
        UniqueKey = offer.UniqueKey;
        Description = offer.Description;
        StartDate = offer.StartDate;
        EndDate = offer.EndDate;

        AllowPublicOffer = offer.AllowPublicOffer;
        PublicOfferMinimum = offer.PublicOffer.Minimum;
        PublicOfferFactor = offer.PublicOffer.Factor;
        PublicOfferPrice = offer.PublicOffer.Price;

        AllowRightIssue = offer.AllowRightIssue;
        RightIssueUnits = offer.RightIssue.NoOfShares;
        RightIssueRights = offer.RightIssue.Rights;
        RightIssueFactor = offer.RightIssue.Factor;
        RightIssuePrice = offer.RightIssue.Price;
    }
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public string RegisterName { get; set; }
    public string UniqueKey { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool AllowPublicOffer { get; set; }
    public int PublicOfferMinimum { get; set; }
    public int PublicOfferFactor { get; set; }
    public decimal PublicOfferPrice { get; set; }

    public bool AllowRightIssue { get; set; }
    public int RightIssueUnits { get; set; }
    public int RightIssueRights { get; set; }
    public int RightIssueFactor { get; set; }
    public decimal RightIssuePrice { get; set; }
}

public record FormResponseUpdateModel(int Id);