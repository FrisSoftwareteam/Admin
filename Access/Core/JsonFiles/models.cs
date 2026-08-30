namespace FirstReg.OnlineAccess.JsonFiles;

public record ShareholderInformationUpdate
{
    public record ListNotSent
    {
        public string SerialNo { get; set; }
        public string AccountNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string CertificateNo { get; set; }
        public string Units { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class ListFailed
    {
        public string SerialNo { get; set; }
        public string AccountNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string CertificateNo { get; set; }
        public string Units { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FailedReason { get; set; }
        public string ClearingNo { get; set; }
        public string Broker { get; set; }
    }
}