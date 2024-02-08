using DeepEqual.Syntax;
using NUnit.Framework;
using Sharp7.Rx.Enums;
using Shouldly;

namespace Sharp7.Rx.Tests;

[TestFixture]
internal class S7VariableNameParserTests
{
    [TestCaseSource(nameof(ValidTestCases))]
    public void Run(TestCase tc)
    {
        var parser = new S7VariableNameParser();
        var resp = parser.Parse(tc.Input);
        resp.ShouldDeepEqual(tc.Expected);
    }

    [TestCase("DB506.Bit216", TestName = "Bit without Bit")]
    [TestCase("DB506.Bit216.8", TestName = "Bit to high")]
    [TestCase("DB506.String216", TestName = "String without Length")]
    [TestCase("DB506.WString216", TestName = "WString without Length")]

    [TestCase("DB506.Int216.1", TestName = "Int with Length")]
    [TestCase("DB506.UInt216.1", TestName = "UInt with Length")]
    [TestCase("DB506.DInt216.1", TestName = "DInt with Length")]
    [TestCase("DB506.UDInt216.1", TestName = "UDInt with Length")]
    [TestCase("DB506.LInt216.1", TestName = "LInt with Length")]
    [TestCase("DB506.ULInt216.1", TestName = "ULInt with Length")]
    [TestCase("DB506.Real216.1", TestName = "LReal with Length")]
    [TestCase("DB506.LReal216.1", TestName = "LReal with Length")]

    [TestCase("DB506.xx216", TestName = "Invalid type")]
    [TestCase("DB506.216", TestName = "No type")]
    [TestCase("DB506.Int216.", TestName = "Trailing dot")]
    [TestCase("x506.Int216", TestName = "Wrong type")]
    [TestCase("506.Int216", TestName = "No type")]
    [TestCase("", TestName = "empty")]
    [TestCase(" ", TestName = "space")]
    [TestCase(" DB506.Int216", TestName = "leading space")]
    [TestCase("DB506.Int216 ", TestName = "trailing space")]
    [TestCase("DB.Int216 ", TestName = "No db")]
    [TestCase("DB5061234.Int216.1", TestName = "DB too large")]
    public void Invalid(string? input)
    {
        var parser = new S7VariableNameParser();
        Should.Throw<InvalidS7AddressException>(() => parser.Parse(input));
    }

    public static IEnumerable<TestCase> ValidTestCases()
    {
        yield return new TestCase("DB506.Bit216.2", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 1, Bit = 2, Type = DbType.Bit});

        yield return new TestCase("DB506.String216.10", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 10, Type = DbType.String});
        yield return new TestCase("DB506.WString216.10", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 10, Type = DbType.WString});

        yield return new TestCase("DB506.Byte216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 1, Type = DbType.Byte});
        yield return new TestCase("DB506.Byte216.100", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 100, Type = DbType.Byte});
        yield return new TestCase("DB506.Int216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 2, Type = DbType.Int});
        yield return new TestCase("DB506.UInt216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 2, Type = DbType.UInt});
        yield return new TestCase("DB506.DInt216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Type = DbType.DInt});
        yield return new TestCase("DB506.UDInt216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Type = DbType.UDInt});
        yield return new TestCase("DB506.LInt216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Type = DbType.LInt});
        yield return new TestCase("DB506.ULInt216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Type = DbType.ULInt});

        yield return new TestCase("DB506.Real216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Type = DbType.Single});
        yield return new TestCase("DB506.LReal216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Type = DbType.Double});


        // Legacy
        yield return new TestCase("DB13.DBX3.1", new S7VariableAddress {Operand = Operand.Db, DbNr = 13, Start = 3, Length = 1, Bit = 1, Type = DbType.Bit});
        yield return new TestCase("Db403.X5.2", new S7VariableAddress {Operand = Operand.Db, DbNr = 403, Start = 5, Length = 1, Bit = 2, Type = DbType.Bit});
        yield return new TestCase("DB55DBX23.6", new S7VariableAddress {Operand = Operand.Db, DbNr = 55, Start = 23, Length = 1, Bit = 6, Type = DbType.Bit});
        yield return new TestCase("DB1.S255.20", new S7VariableAddress {Operand = Operand.Db, DbNr = 1, Start = 255, Length = 20, Type = DbType.String});
        yield return new TestCase("DB5.String887.20", new S7VariableAddress {Operand = Operand.Db, DbNr = 5, Start = 887, Length = 20, Type = DbType.String});
        yield return new TestCase("DB506.B216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 1, Type = DbType.Byte});
        yield return new TestCase("DB506.DBB216.5", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 5, Type = DbType.Byte});
        yield return new TestCase("DB506.D216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Type = DbType.Single});
        yield return new TestCase("DB506.DINT216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 4, Type = DbType.DInt});
        yield return new TestCase("DB506.INT216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 2, Type = DbType.Int});
        yield return new TestCase("DB506.DBW216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 2, Type = DbType.Int});
        yield return new TestCase("DB506.DUL216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Type = DbType.ULInt});
        yield return new TestCase("DB506.DULINT216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Type = DbType.ULInt});
        yield return new TestCase("DB506.DULONG216", new S7VariableAddress {Operand = Operand.Db, DbNr = 506, Start = 216, Length = 8, Type = DbType.ULInt});
    }

    public record TestCase(string Input, S7VariableAddress Expected)
    {
        public override string ToString() => Input;
    }
}
