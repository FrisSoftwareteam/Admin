namespace FirstReg.Mobile;

public class NotFoundException : Exception
{
    public NotFoundException(string message = "Data not found") : base(message) { }

    public static void Throw(string message = "Data not found")
    => throw new NotFoundException(message);
}