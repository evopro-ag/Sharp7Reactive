using JetBrains.Annotations;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

[NoReorder]
internal record VariableAddress(Operand Operand, ushort DbNo, DbType Type, ushort Start, ushort Length, byte? Bit = null)
{
    public Operand Operand { get;  } = Operand;
    public ushort DbNo { get;  } = DbNo;
    public ushort Start { get; } = Start;
    public ushort Length { get; } = Length;
    public byte? Bit { get; } = Bit;
    public DbType Type { get; } = Type;

    public ushort BufferLength => Type switch
    {
        DbType.String => (ushort) (Length + 2),
        DbType.WString => (ushort) (Length * 2 + 4),
        _ => Length
    };

    public override string ToString() =>
        Type switch
        {
            DbType.Bit => $"{Operand}{DbNo}.{Type}{Start}.{Bit}",
            DbType.String => $"{Operand}{DbNo}.{Type}{Start}.{Length}",
            DbType.WString => $"{Operand}{DbNo}.{Type}{Start}.{Length}",
            DbType.Byte => Length == 1 ? $"{Operand}{DbNo}.{Type}{Start}" : $"{Operand}{DbNo}.{Type}{Start}.{Length}",
            _ => $"{Operand}{DbNo}.{Type}{Start}",
        };
}
