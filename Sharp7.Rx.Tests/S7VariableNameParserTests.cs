using DeepEqual.Syntax;
using NUnit.Framework;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Tests;

[TestFixture]
internal class S7VariableNameParserTests
{
    [TestCaseSource(nameof(GetTestCases))]
    public void Run(TestCase tc)
    {
        var parser = new S7VariableNameParser();
        var resp = parser.Parse(tc.Input);
        resp.ShouldDeepEqual(tc.Expected);
    }

    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase("DB13.DBX3.1", new S7VariableAddress {Operand = Operand.Db, DbNr = 13, Start = 3, Length = 1, Bit = 1, Type = DbType.Bit});
        yield return new TestCase("Db403.X5.2", new S7VariableAddress {Operand = Operand.Db, DbNr = 403, Start = 5, Length = 1, Bit = 2, Type = DbType.Bit});
        yield return new TestCase("DB55DBX23.6", new S7VariableAddress {Operand = Operand.Db, DbNr = 55, Start = 23, Length = 1, Bit = 6, Type = DbType.Bit});
        yield return new TestCase("DB1.S255", new S7VariableAddress {Operand = Operand.Db, DbNr = 1, Start = 255, Length = 0, Bit = 0, Type = DbType.String});
        yield return new TestCase("DB1.S255.20", new S7VariableAddress {Operand = Operand.Db, DbNr = 1, Start = 255, Length = 20, Bit = 0, Type = DbType.String});
        yield return new TestCase("DB5.String887.20", new S7VariableAddress {Operand = Operand.Db, DbNr = 5, Start = 887, Length = 20, Bit = 0, Type = DbType.String});
        yield return new TestCase("DB506.B216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 1, Bit = 0, Type = DbType.Byte});
        yield return new TestCase("DB506.DBB216.5", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 5, Bit = 0, Type = DbType.Byte});
        yield return new TestCase("DB506.D216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Bit = 0, Type = DbType.Double});
        yield return new TestCase("DB506.DINT216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Bit = 0, Type = DbType.DInteger});
        yield return new TestCase("DB506.INT216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 2, Bit = 0, Type = DbType.Integer});
        yield return new TestCase("DB506.DBW216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 2, Bit = 0, Type = DbType.Integer});
        yield return new TestCase("DB506.DUL216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Bit = 0, Type = DbType.ULong});
        yield return new TestCase("DB506.DULINT216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Bit = 0, Type = DbType.ULong});
        yield return new TestCase("DB506.DULONG216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Bit = 0, Type = DbType.ULong});
    }

    public record TestCase(string Input, S7VariableAddress Expected)
    {
        public override string ToString() => Input;
    }
}
