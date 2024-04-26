using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Extensions;

internal static class VariableAddressExtensions
{
    private static readonly Dictionary<Type, Func<VariableAddress, bool>> supportedTypeMap = new()
    {
        {typeof(bool), a => a.Type == DbType.Bit},
        {typeof(string), a => a.Type is DbType.String or DbType.WString or DbType.Byte},
        {typeof(byte), a => a.Type == DbType.Byte && a.Length == 1},
        {typeof(short), a => a.Type == DbType.Int},
        {typeof(ushort), a => a.Type == DbType.UInt},
        {typeof(int), a => a.Type == DbType.DInt},
        {typeof(uint), a => a.Type == DbType.UDInt},
        {typeof(long), a => a.Type == DbType.LInt},
        {typeof(ulong), a => a.Type == DbType.ULInt},
        {typeof(float), a => a.Type == DbType.Single},
        {typeof(double), a => a.Type == DbType.Double},
        {typeof(byte[]), a => a.Type == DbType.Byte},
    };

    public static bool MatchesType(this VariableAddress address, Type type) =>
        supportedTypeMap.TryGetValue(type, out var map) && map(address);

    public static Type GetClrType(this VariableAddress address) =>
        address.Type switch
        {
            DbType.Bit => typeof(bool),
            DbType.String => typeof(string),
            DbType.WString => typeof(string),
            DbType.Byte => address.Length == 1 ? typeof(byte) : typeof(byte[]),
            DbType.Int => typeof(short),
            DbType.UInt => typeof(ushort),
            DbType.DInt => typeof(int),
            DbType.UDInt => typeof(uint),
            DbType.LInt => typeof(long),
            DbType.ULInt => typeof(ulong),
            DbType.Single => typeof(float),
            DbType.Double => typeof(double),
            _ => throw new ArgumentOutOfRangeException(nameof(address))
        };

}
