using Sharp7.Rx.Enums;

namespace Sharp7.Rx
{
    public class S7VariableAddress
    {
        public Operand Operand { get; set; }
        public ushort DbNr { get; set; }
        public ushort Start { get; set; }
        public ushort Length { get; set; }
        public byte Bit { get; set; }
        public DbType Type { get; set; }
    }
}