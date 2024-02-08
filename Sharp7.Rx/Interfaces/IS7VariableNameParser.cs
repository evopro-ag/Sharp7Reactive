#nullable enable
namespace Sharp7.Rx.Interfaces;

internal interface IS7VariableNameParser
{
    S7VariableAddress Parse(string input);
}
