namespace Assesment.Domain;

public class SecureException : Exception
{
    public SecureException(string message) : base(message)
    {
    }

    public SecureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
