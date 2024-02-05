using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx
{
    internal class S7VariableNameParser : IS7VariableNameParser
    {
        private static readonly Regex regex = new Regex(@"^(?<operand>db{1})(?<dbNr>\d{1,4})\.?(?<type>dbx|x|s|string|b|dbb|d|int|dbw|w|dint|dul|dulint|dulong|){1}(?<start>\d+)(\.(?<bitOrLength>\d+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly Dictionary<string, DbType> types = new Dictionary<string, DbType>
        {
            {"x", DbType.Bit},
            {"dbx", DbType.Bit},
            {"s", DbType.String},
            {"string", DbType.String},
            {"b", DbType.Byte},
            {"dbb", DbType.Byte},
            {"d", DbType.Double},
            {"int", DbType.Integer},
            {"dint", DbType.DInteger},
            {"w", DbType.Integer},
            {"dbw", DbType.Integer},
            {"dul", DbType.ULong },
            {"dulint", DbType.ULong },
            {"dulong", DbType.ULong }
        };


        public S7VariableAddress Parse(string input)
        {
            var match = regex.Match(input);
            if (match.Success)
            {
                var operand = (Operand)Enum.Parse(typeof(Operand), match.Groups["operand"].Value, true);
                var dbNr = ushort.Parse(match.Groups["dbNr"].Value, NumberStyles.Integer);
                var start = ushort.Parse(match.Groups["start"].Value, NumberStyles.Integer);
                var type = ParseType(match.Groups["type"].Value);

                var s7VariableAddress = new S7VariableAddress
                {
                    Operand = operand,
                    DbNr = dbNr,
                    Start = start,
                    Type = type,
                };

                if (type == DbType.Bit)
                {
                    s7VariableAddress.Length = 1;
                    s7VariableAddress.Bit = byte.Parse(match.Groups["bitOrLength"].Value);
                }
                else if (type == DbType.Byte)
                {
                    s7VariableAddress.Length = match.Groups["bitOrLength"].Success ? ushort.Parse(match.Groups["bitOrLength"].Value) : (ushort)1;
                }
                else if (type == DbType.String)
                {
                    s7VariableAddress.Length = match.Groups["bitOrLength"].Success ? ushort.Parse(match.Groups["bitOrLength"].Value) : (ushort)0;
                }
                else if (type == DbType.Integer)
                {
                    s7VariableAddress.Length = 2;
                }
                else if (type == DbType.DInteger)
                {
                    s7VariableAddress.Length = 4;
                }
                else if (type == DbType.ULong)
                {
                    s7VariableAddress.Length = 8;
                }
                else if (type == DbType.Double)
                {
                    s7VariableAddress.Length = 4;
                }

                return s7VariableAddress;
            }

            return null;
        }

        private DbType ParseType(string value)
        {
            return types
                .Where(pair => pair.Key.Equals(value, StringComparison.InvariantCultureIgnoreCase))
                .Select(pair => pair.Value)
                .FirstOrDefault();
        }
    }
}