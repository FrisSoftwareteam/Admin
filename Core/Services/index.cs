using FirstReg.Data;
using FirstReg.Services;

namespace FirstReg
{
    public class Service(AppDB db)
    {
        public DataService Data { get; } = new DataService(db);
        public EmailService Email { get; } = new EmailService();
    }
}