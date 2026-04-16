namespace FirstReg.Data;

public enum Gender
{
    Male = 1,
    Female = 2
}

public enum AgeRange
{
    Age18To30,
    Age31To45,
    Age46To60,
    Above60
}

public enum BankAccountType
{
    Savings,
    Current
}

public enum Forms
{
    DataUpload,
    ChangeOfAddress,
    DividendCard,
    EDividend,
    PublicOffer,
    RightIssue,
    ShareholderUpdate // handle shareholder update for registers
}

public enum ShareSubscriptionType
{
    PublicOffer,
    RightIssue
}

public enum AuditLogType
{
    General,
    Login,
    Search,
    ViewShareholder,
    DownloadShareholder,
	ProfileUpdate,
	ForgotPassword,
	Register,
	ResetPassword,
	ConfirmedEmail,
	ChangedPassword
}

public enum AuditLogSection
{
    All,
    Admin,
    FrAdmin,
    Company,
    Shareholder,
    Forms,
    ShareOffer
}