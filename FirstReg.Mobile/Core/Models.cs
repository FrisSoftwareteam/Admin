using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FirstReg.Mobile.Core.Models;

public class TokenParameters
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public SymmetricSecurityKey GetKey()
    {
        return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Secret));
    }
}