namespace FirstReg.API.MobileContract;

public record GenericResponse<T>
{
    public bool Success { get; private set; }
    public string Message { get; private set; }
    public T Data { get; private set; }

    public static GenericResponse<T> CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static GenericResponse<T> CreateSuccess(T data = default, string message = "") => new()
    {
        Success = true,
        Data = data,
        Message = message,
    };
}

public record VerifyPhoneNumberRequest
{
    /// <summary>
    /// User's phone number
    /// </summary>
    public string PhoneNumber { get; set; }
}

public record VerifyEmailRequest
{
    /// <summary>
    /// User email addresss
    /// </summary>
    public string EmailAddress { get; set; }
}

public record VerifyTokenRequest
{
    public string Token { get; set; }
    public string PhoneNumber { get; set; }
}

public record AccountRequest
{
    public string RegisterCode { get; set; }
    public string AccountNumber { get; set; }
}

public record BalanceRequest : AccountRequest { }
public record DividendRequest : AccountRequest { }
public record BankMandateRequest : AccountRequest { }

public record AccountResponse
{
    public string RegisterCode { get; set; }
    public string AccountNumber { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string EmailAddress { get; set; }
    public string Address { get; set; }
    public string PostalAddress { get; set; }
    public string BankMandate { get; set; }
    public int Balance { get; set; }
}