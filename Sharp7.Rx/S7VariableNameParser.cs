using System.Globalization;
using System.Text.RegularExpressions;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx;

internal class S7VariableNameParser : IS7VariableNameParser
{
    private static readonly Regex regex = new Regex(@"^(?<operand>db)(?<dbNo>\d+)\.?(?<type>[a-z]+)(?<start>\d+)(\.(?<bitOrLength>\d+))?$",
                                                    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, DbType> types = new Dictionary<string, DbType>(StringComparer.OrdinalIgnoreCase)
    {
        {"bit", DbType.Bit},

        {"string", DbType.String},
        {"wstring", DbType.WString},

        {"byte", DbType.Byte},
        {"int", DbType.Int},
        {"uint", DbType.UInt},
        {"dint", DbType.DInt},
        {"udint", DbType.UDInt},
        {"lint", DbType.LInt},
        {"ulint", DbType.ULInt},

        {"real", DbType.Single},
        {"lreal", DbType.Double},

        // used for legacy compatability
        {"b", DbType.Byte},
        {"d", DbType.Single},
        {"dbb", DbType.Byte},
        {"dbw", DbType.Int},
        {"dbx", DbType.Bit},
        {"dul", DbType.ULInt},
        {"dulint", DbType.ULInt},
        {"dulong", DbType.ULInt},
        {"s", DbType.String},
        {"w", DbType.Int},
        {"x", DbType.Bit},
    };

    public S7VariableAddress Parse(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var match = regex.Match(input);
        if (!match.Success)
            throw new InvalidS7AddressException($"Invalid S7 address: \"{input}\"", input);

        var operand = (Operand) Enum.Parse(typeof(Operand), match.Groups["operand"].Value, true);

        if (!ushort.TryParse(match.Groups["dbNo"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dbNr))
            throw new InvalidS7AddressException($"\"{match.Groups["dbNo"].Value}\" is an invalid DB number in \"{input}\"", input);

        if (!ushort.TryParse(match.Groups["start"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
            throw new InvalidS7AddressException($"\"{match.Groups["start"].Value}\" is an invalid start bit in \"{input}\"", input);

        if (!types.TryGetValue(match.Groups["type"].Value, out var type))
            throw new InvalidS7AddressException($"\"{match.Groups["type"].Value}\" is an invalid type in \"{input}\"", input);

        ushort length = type switch
        {
            DbType.Bit => 1,

            DbType.String => GetLength(),
            DbType.WString => GetLength(),

            DbType.Byte => GetLength(1),

            DbType.Int => 2,
            DbType.DInt => 4,
            DbType.ULInt => 8,
            DbType.UInt => 2,
            DbType.UDInt => 4,
            DbType.LInt => 8,

            DbType.Single => 4,
            DbType.Double => 8,
            _ => throw new ArgumentOutOfRangeException($"DbType {type} is not supported")
        };

        switch (type)
        {
            case DbType.Bit:
            case DbType.String:
            case DbType.WString:
            case DbType.Byte:
                break;
            case DbType.Int:
            case DbType.UInt:
            case DbType.DInt:
            case DbType.UDInt:
            case DbType.LInt:
            case DbType.ULInt:
            case DbType.Single:
            case DbType.Double:
            default:
                if (match.Groups["bitOrLength"].Success)
                    throw new InvalidS7AddressException($"{type} address must not have a length: \"{input}\"", input);
                break;
        }

        byte? bit = type == DbType.Bit ? GetBit() : null;


        var s7VariableAddress = new S7VariableAddress
        {
            Operand = operand,
            DbNr = dbNr,
            Start = start,
            Type = type,
            Length = length,
            Bit = bit
        };

        return s7VariableAddress;

        ushort GetLength(ushort? defaultValue = null)
        {
            if (!match.Groups["bitOrLength"].Success)
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                throw new InvalidS7AddressException($"Variable of type {type} must have a length set \"{input}\"", input);
            }

            if (!ushort.TryParse(match.Groups["bitOrLength"].Value, out var result))
                throw new InvalidS7AddressException($"\"{match.Groups["bitOrLength"].Value}\" is an invalid length in \"{input}\"", input);

            return result;
        }

        byte GetBit()
        {
            if (!match.Groups["bitOrLength"].Success)
                throw new InvalidS7AddressException($"Variable of type {type} must have a bit number set \"{input}\"", input);

            if (!byte.TryParse(match.Groups["bitOrLength"].Value, out var result))
                throw new InvalidS7AddressException($"\"{match.Groups["bitOrLength"].Value}\" is an invalid bit number in \"{input}\"", input);

            return result;
        }
    }
}
