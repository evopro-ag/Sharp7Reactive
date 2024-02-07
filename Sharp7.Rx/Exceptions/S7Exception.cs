namespace Sharp7.Rx;

public abstract class S7Exception : Exception
{
    protected S7Exception(string message) : base(message)
    {
    }

    protected S7Exception(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class InvalidS7AddressException : S7Exception
{
    public InvalidS7AddressException(string message, string input) : base(message)
    {
        Input = input;
    }

    public InvalidS7AddressException(string message, Exception innerException, string input) : base(message, innerException)
    {
        Input = input;
    }

    public string Input { get; private set; }
}
