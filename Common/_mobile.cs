namespace FirstReg.Bson
{
    public class Shareholder
    {
        public Shareholder() => Holdings = new();

        public int Id { get; set; }
        public string ClearingNo { get; set; }
        public string FullName { get; set; }
        public bool Verified { get; set; }
        public bool HasHoldings { get; set; }

        public List<Holding> Holdings { get; set; }
    }

    public class Holding
    {
        public Holding()
        {
            Units = new();
            Dividends = new();
        }

        public int Id { get; set; }
        public string AccountNo { get; set; }
        public int RegCode { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string ClearingNo { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }

        public List<Unit> Units { get; set; }
        public List<Dividend> Dividends { get; set; }

        public decimal GetTotalUnits() => Units.Sum(x => x.TotalUnits);
        public string GetFullName() => $"{FirstName} {MiddleName} {LastName}".Trim();
    }

    public class RegMaxUnitResponse
    {
        public RegMaxUnitResponse(int maxUnit) => MaxUnit = maxUnit;

        public decimal MaxUnit { get; set; }

        public static implicit operator RegMaxUnitResponse(int maxUnit) => new(maxUnit);
    }

    public class Unit
    {
        public int Id { get; set; }
        public int AccountNo { get; set; }
        public int RegCode { get; set; }
        public int CertNo { get; set; }
        public string Date { get; set; }
        public string OldCertNo { get; set; }
        public string Description { get; set; }
        public string Narration { get; set; }
        public decimal TotalUnits { get; set; }
        public string Status => "Active";
    }

    public class Dividend
    {
        public int Id { get; set; }
        public int AccountNo { get; set; }
        public int RegCode { get; set; }
        public decimal? Gross { get; set; }
        public decimal? Total { get; set; }
        public decimal? Net { get; set; }
        public int DividendNo { get; set; }
        public decimal? Tax { get; set; }
        public int WarrantNo { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
    }

    public class Register
    {
        public Register()
        {
            Dividends = new();
        }

        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        public List<RegDividend> Dividends { get; set; }
    }

    public class RegDividend
    {
        public int Id { get; set; }
        public int RegCode { get; set; }
        public string Description { get; set; }
        public string PaymentNo { get; set; }
        public decimal? AmountDeclared { get; set; }
        public string YearEnd { get; set; }
        public string DatePayable { get; set; }
        public string ClosureDate { get; set; }
    }

    public class RegHolding
    {
        public RegHolding() { }
        //public RegHolding(string arrstring) : this(arrstring.Split(",")) { }
        public RegHolding(string[] arrstring)
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
            Units = Convert.ToDecimal(arrstring[10]);
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

        public decimal Units { get; set; }

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
            Units.ToString(),
            Register.ToString()
        };
    }

    public class RegSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ShareholdersCount { get; set; }
        public int? ShareholdersWithUnitCount { get; set; }
        public decimal? MaxUnit { get; set; }
        public decimal? AverageUnit { get; set; }
        public decimal? TotalUnits { get; set; }
        public double GetShareholdersPercentage() 
        => (double)((ShareholdersCount > 0) ? (ShareholdersWithUnitCount / (double)ShareholdersCount * 100) : 0);
        public decimal GetUnitsPercentage() 
        => (decimal)((MaxUnit > 0) ? (AverageUnit / MaxUnit * 100) : 0);
    }
}