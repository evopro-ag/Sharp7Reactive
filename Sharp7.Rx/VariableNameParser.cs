﻿#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx;

internal class VariableNameParser : IVariableNameParser
{
    private static readonly Regex regex = new(@"^(?<operand>db)(?<dbNo>\d+)\.?(?<type>[a-z]+)(?<start>\d+)(\.(?<bitOrLength>\d+))?$",
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

        // S7 notation
        {"dbb", DbType.Byte},
        {"dbw", DbType.Int},
        {"dbx", DbType.Bit},
        {"dbd", DbType.DInt},

        // used for legacy compatability
        {"b", DbType.Byte},
        {"d", DbType.Single},
        {"dul", DbType.ULInt},
        {"dulint", DbType.ULInt},
        {"dulong", DbType.ULInt},
        {"s", DbType.String},
        {"w", DbType.Int},
        {"x", DbType.Bit},
    };

    public VariableAddress Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var match = regex.Match(input);
        if (!match.Success)
            throw new InvalidS7AddressException($"Invalid S7 address \"{input}\". Expect format \"DB<dbNo>.<type><startByte>(.<length>)\".", input);

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


        var s7VariableAddress = new VariableAddress(Operand: operand, DbNo: dbNr, Type: type, Start: start, Length: length, Bit: bit);

        return s7VariableAddress;

        ushort GetLength(ushort? defaultValue = null)
        {
            if (!match.Groups["bitOrLength"].Success)
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                throw new InvalidS7AddressException($"Variable of type {type} must have a length set. Example \"db12.byte10.3\", found \"{input}\"", input);
            }

            if (!ushort.TryParse(match.Groups["bitOrLength"].Value, out var result))
                throw new InvalidS7AddressException($"\"{match.Groups["bitOrLength"].Value}\" is an invalid length in \"{input}\"", input);

            return result;
        }

        byte GetBit()
        {
            if (!match.Groups["bitOrLength"].Success)
                throw new InvalidS7AddressException($"Variable of type {type} must have a bit number set. Example \"db12.bit10.3\", found \"{input}\"", input);

            if (!byte.TryParse(match.Groups["bitOrLength"].Value, out var result))
                throw new InvalidS7AddressException($"\"{match.Groups["bitOrLength"].Value}\" is an invalid bit number in \"{input}\"", input);

            if (result > 7)
                throw new InvalidS7AddressException($"Bit must be between 0 and 7 but is {result} in \"{input}\"", input);

            return result;
        }
    }
}
