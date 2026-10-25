using System.Globalization;
using System.Text;

namespace Erlang;

internal static class ProcessDisplayText
{
    private const string ProcessPrefix = "Process: ";
    private const string CurrentPrefix = "Current function: ";
    private const string CallerPrefix = "Called from: ";
    private const string ModuleSeparator = ":";
    private const string AritySeparator = "/";
    private const string LineEnd = "\n";

    public static string Format(Pid owner, IReadOnlyList<ErlangExecutionFrame> frames)
    {
        var text = new StringBuilder(ProcessPrefix).Append(owner).Append(LineEnd);
        for (int index = 0; index < frames.Count; index++)
        {
            ErlangExecutionFrame frame = frames[index];
            text.Append(index == 0 ? CurrentPrefix : CallerPrefix)
                .Append(Term.A(frame.Module))
                .Append(ModuleSeparator)
                .Append(Term.A(frame.Function))
                .Append(AritySeparator)
                .Append(frame.Arity.ToString(CultureInfo.InvariantCulture))
                .Append(LineEnd);
        }

        return text.ToString();
    }
}
