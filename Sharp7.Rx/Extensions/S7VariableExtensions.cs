using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Extensions;

internal static class S7VariableAddressExtensions
{
    private static readonly Dictionary<Type, Func<S7VariableAddress, bool>> supportedTypeMap = new()
    {
        {typeof(bool), a => a.Type == DbType.Bit},
        {typeof(string), a => a.Type is DbType.String or DbType.WString or DbType.Byte },
        {typeof(byte), a => a.Type==DbType.Byte && a.Length == 1},
        {typeof(short), a => a.Type==DbType.Int},
        {typeof(ushort), a => a.Type==DbType.UInt},
        {typeof(int), a => a.Type==DbType.DInt},
        {typeof(uint), a => a.Type==DbType.UDInt},
        {typeof(long), a => a.Type==DbType.LInt},
        {typeof(ulong), a => a.Type==DbType.ULInt},
        {typeof(float), a => a.Type==DbType.Single},
        {typeof(double), a => a.Type==DbType.Double},
        {typeof(byte[]), a => a.Type==DbType.Byte},
    };

    public static bool MatchesType(this S7VariableAddress address, Type type) =>
        supportedTypeMap.TryGetValue(type, out var map) && map(address);
}
