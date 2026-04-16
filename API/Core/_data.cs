namespace FirstReg.API
{
    public class RegHolding
    {
        public RegHolding() { }
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
        }
        public int Id { get; set; }
        public int RegCode { get; set; }
        public int AccountNo { get; set; }
        public string ClearingNo { get; set; }

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
            Units.ToString()
        };
    }
}
