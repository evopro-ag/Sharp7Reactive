using JetBrains.Annotations;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

[NoReorder]
internal class VariableAddress
{
    public Operand Operand { get; set; }
    public ushort DbNr { get; set; }
    public ushort Start { get; set; }
    public ushort Length { get; set; }
    public byte? Bit { get; set; }
    public DbType Type { get; set; }

    public ushort BufferLength => Type switch
    {
        DbType.String => (ushort) (Length + 2),
        DbType.WString => (ushort) (Length * 2 + 4),
        _ => Length
    };

    public override string ToString() =>
        Type switch
        {
            DbType.Bit => $"{Operand}{DbNr}.{Type}{Start}.{Bit}",
            DbType.String => $"{Operand}{DbNr}.{Type}{Start}.{Length}",
            DbType.WString => $"{Operand}{DbNr}.{Type}{Start}.{Length}",
            DbType.Byte => Length == 1 ? $"{Operand}{DbNr}.{Type}{Start}" : $"{Operand}{DbNr}.{Type}{Start}.{Length}",
            _ => $"{Operand}{DbNr}.{Type}{Start}",
        };
}
