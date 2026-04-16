using FirstReg.Data;

namespace FirstReg.Mobile.Core.Contracts;

public record GenericResponse<T>
{
    public bool Success { get; private set; }
    public string? Message { get; private set; }
    public T? Data { get; private set; }

    public static GenericResponse<T> CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static GenericResponse<T> CreateSuccess(string message = "") => new()
    {
        Success = true,
        Message = message,
    };

    public static GenericResponse<T> CreateSuccess(T data, string message = "") => new()
    {
        Success = true,
        Data = data,
        Message = message,
    };
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string FullName);

public record ForgotRequest(string Email);

public record GetCodeRequest(string Email);

public record ValidateEmailRequest(string Email, string Code);

public record PasswordRequest(string OldPassword, string NewPassword);

public record RegisterRequest
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string SecondaryPhone { get; set; } = null!;
    public string PostCode { get; set; } = null!;
    public string ClearingNo { get; set; } = null!;
    public string? Fax { get; set; }

    public string Password { get; set; } = null!;

    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
}

public record VerifyPhoneNumberRequest
{
    /// <summary>
    /// User's phone number
    /// </summary>
    public string PhoneNumber { get; set; } = null!;
}

public record VerifyEmailRequest
{
    /// <summary>
    /// User email addresss
    /// </summary>
    public string EmailAddress { get; set; } = null!;
}

public record VerifyTokenRequest
{
    public string Token { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
}

public record AccountRequest
{
    public string RegisterCode { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
}

public record BalanceRequest : AccountRequest { }
public record DividendRequest : AccountRequest { }
public record BankMandateRequest : AccountRequest { }

public record AccountResponse
{
    public string RegisterCode { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string PostalAddress { get; set; } = null!;
    public string BankMandate { get; set; } = null!;
    public int Balance { get; set; }
}