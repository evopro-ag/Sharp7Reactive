#nullable enable
namespace Sharp7.Rx.Interfaces;

internal interface IVariableNameParser
{
    VariableAddress Parse(string input);
}
