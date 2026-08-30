using FirstReg.Bson;

namespace FirstReg
{
    public class EStockApiUrl
    {
        private readonly string _root;
        public EStockApiUrl(string root = "") => _root = root;
        public string ActivateShareholder => $"{_root}/activate-shareholder";
        public string GetRegisterShareholder => $"{_root}/regshareholders";
        public string GetRegisterSummary => $"{_root}/register/summary";
        public string GetShareholders => $"{_root}/shareholders";
        public string GetUnits => $"{_root}/units";
        public string GetDividends => $"{_root}/dividends";
    }

    //public class Unit
    //{
    //    public int Id { get; set; }
    //    public int AccountNo { get; set; }
    //    public int RegCode { get; set; }
    //    public int CertNo { get; set; }
    //    public string Date { get; set; }
    //    public string OldCertNo { get; set; }
    //    public string Description { get; set; }
    //    public decimal TotalUnits { get; set; }
    //}

    public class NewShareholderRequest
    {
        public NewShareholderRequest() => Holdings = new();

        public NewShareholderRequest(int id, string clearingNo, string fullName) : this()
        {
            Id = id;
            ClearingNo = clearingNo;
            Name = fullName;
        }

        public NewShareholderRequest(int id, string clearingNo, string fullName, IEnumerable<NewShareholdingRequest> holdings)
        {
            Id = id;
            ClearingNo = clearingNo;
            Name = fullName;
            Holdings = holdings.ToList();
        }

        public int Id { get; set; }
        public string ClearingNo { get; set; }
        public string Name { get; set; }
        public List<NewShareholdingRequest> Holdings { get; set; }
    }

    public class NewShareholdingRequest
    {
        public NewShareholdingRequest() { }
        public NewShareholdingRequest(string accountNo, int regCode)
            : this(accountNo, regCode.ToString())
        { }
        public NewShareholdingRequest(string accountNo, string regCode)
        {
            AccountNo = accountNo;
            RegCode = regCode;
        }
        public string AccountNo { get; set; }
        public string RegCode { get; set; }
    }

    //public class Dividend
    //{
    //    public int Id { get; set; }
    //    public int AccountNo { get; set; }
    //    public int RegCode { get; set; }
    //    public decimal? Gross { get; set; }
    //    public decimal? Total { get; set; }
    //    public decimal? Net { get; set; }
    //    public int DividendNo { get; set; }
    //    public decimal? Tax { get; set; }
    //    public int WarrantNo { get; set; }
    //    public string Type { get; set; }
    //    public string Date { get; set; }
    //}

    public class RegSH
    {
        public RegSH()
        {
            Units = new List<Bson.Unit>();
            Dividends = new List<Bson.Dividend>();
        }
        public RegSH(string[] arrstring) : this()
        {
            Id = Convert.ToInt32(arrstring[0]);
            RegCode = Convert.ToInt32(arrstring[1]);
            AccountNo = Convert.ToInt32(arrstring[2]);
            ClearingNo = arrstring[3];
            LastName = arrstring[4];
            Gender = arrstring[5];
            Phone = arrstring[6];
            Mobile = arrstring[7];
            Email = arrstring[8];
            Address1 = arrstring[9];
            TotalUnits = Convert.ToDecimal(arrstring[10]);
            Register = arrstring[11];
        }
        public int Id { get; set; }
        public int RegCode { get; set; }
        public int AccountNo { get; set; }
        public string ClearingNo { get; set; }

        public string Register { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        private string OtherNames => $"{FirstName} {MiddleName}".Trim();
        public string FullName => $"{OtherNames} {LastName}".Trim();

        public string Gender { get; set; }

        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        private string Address0 => $"{Address1} {Address2}".Trim();
        public string Address => $"{Address0} {City}".Trim();

        public decimal TotalUnits { get; set; }

        public List<Bson.Unit> Units { get; set; }
        public List<Bson.Dividend> Dividends { get; set; }

        public string[] ArrayString => new[]
        {
            Id.ToString(),
            RegCode.ToString(),
            AccountNo.ToString(),
            ClearingNo,
            FullName,
            Gender,
            Phone,
            Mobile,
            Email,
            Address,
            TotalUnits.ToString(),
            Register.ToString()
        };
    }

    public partial class RegHolding
    {
        public RegHolding(RegSH sh)
        {
            Id = sh.Id;
            RegisterId = sh.RegCode;
            AccountNo = sh.AccountNo;
            ClearingNo = sh.ClearingNo;
            Name = sh.FullName;
            Gender = sh.Gender;
            Address = sh.Address;
            Email = sh.Email;
            Phone = sh.Phone;
            Mobile = sh.Mobile;
            Units = sh.TotalUnits;
        }

        public int Id { get; set; }
        public int RegisterId { get; set; }
        public int AccountNo { get; set; }
        public string ClearingNo { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public decimal Units { get; set; }
        public DateTime Date { get; set; }
    }
}