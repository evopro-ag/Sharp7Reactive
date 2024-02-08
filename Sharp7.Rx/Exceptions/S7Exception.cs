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

public class S7CommunicationException : S7Exception
{
    public S7CommunicationException(string message, int s7ErrorCode, string s7ErrorText) : base(message)
    {
        S7ErrorCode = s7ErrorCode;
        S7ErrorText = s7ErrorText;
    }

    public S7CommunicationException(string message, Exception innerException, int s7ErrorCode, string s7ErrorText) : base(message, innerException)
    {
        S7ErrorCode = s7ErrorCode;
        S7ErrorText = s7ErrorText;
    }

    public int S7ErrorCode { get; }
    public string S7ErrorText { get; }
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
