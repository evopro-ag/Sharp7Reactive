using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Extensions;

internal static class OperandExtensions
{
    public static S7Area ToArea(this Operand operand) =>
        operand switch
        {
            Operand.Input => S7Area.PE,
            Operand.Output => S7Area.PA,
            Operand.Marker => S7Area.MK,
            Operand.Db => S7Area.DB,
            _ => throw new ArgumentOutOfRangeException(nameof(operand), operand, null)
        };
}
