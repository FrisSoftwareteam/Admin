using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.Mail;

namespace FirstReg.Data
{
    public partial class RegisterSHSumm
    {
        public int Id { get; set; }

        public int Count { get; set; }
        public decimal MinUnits { get; set; }
        public decimal MaxUnits { get; set; }
        public string ListUrl { get; set; }

        public string S { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public string ExportUrl { get; set; }
        public string DetailsUrl { get; set; }
        public int RegId { get; set; }
    }

    public partial class FRRegisterSHSumm
    {
        public int Id { get; set; }

        public int Count { get; set; }

        public int RegId { get; set; }
        public string Global { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ClearingNo { get; set; }
        public string OldAccountNo { get; set; }
        public string AccountNo { get; set; }

        public string ListUrl { get; set; }
        public string DetailsUrl { get; set; }
        public string ExportUrl { get; set; }
        public string DividendsUrl { get; set; }
        public string ExportDividendsUrl { get; set; }
    }

    public partial class ShareholderView
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PrimaryPhone { get; set; }
        public string SecondaryPhone { get; set; }
        public string PostCode { get; set; }
        public string ClearingNo { get; set; }
        public bool Verified { get; set; }
        public bool AllowGroup { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }


    public partial class RegisterView
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string RCNo { get; set; }
        public bool Active { get; set; }
        public string Symbol { get; set; }
    }

    public partial record RegisterIdModel
    {
        public int Id { get; set; }
    }

    public partial class ShareholdingView
    {
        public int Id { get; set; }
        public int RegisterId { get; set; }
        public string RegisterCode { get; set; }
        public string Register { get; set; }
        public int ShareHolderId { get; set; }
        public string ShareholderCode { get; set; }
        public string Shareholder { get; set; }
        public string ClearingNo { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public float Units { get; set; }
        public float Value { get; set; }
        public int Status { get; set; }
        public DateTime Date { get; set; }
    }

    public class AuditLogView
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public AuditLogSection Section { get; set; }
        public AuditLogType Type { get; set; }
        public int UserId { get; set; }
        public UserType UserType { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }
    }

}
