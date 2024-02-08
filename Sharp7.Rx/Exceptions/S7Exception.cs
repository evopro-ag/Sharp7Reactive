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

public class DataTypeMissmatchException : S7Exception
{
    internal DataTypeMissmatchException(string message, Type type, S7VariableAddress address) : base(message)
    {
        Type = type;
        Address = address.ToString();
    }

    internal DataTypeMissmatchException(string message, Exception innerException, Type type, S7VariableAddress address) : base(message, innerException)
    {
        Type = type;
        Address = address.ToString();
    }

    public string Address { get; }

    public Type Type { get; }
}

public class UnsupportedS7TypeException : S7Exception
{
    internal UnsupportedS7TypeException(string message, Type type, S7VariableAddress address) : base(message)
    {
        Type = type;
        Address = address.ToString();
    }

    internal UnsupportedS7TypeException(string message, Exception innerException, Type type, S7VariableAddress address) : base(message, innerException)
    {
        Type = type;
        Address = address.ToString();
    }

    public string Address { get; }

    public Type Type { get; }
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

    public string Input { get; }
}
