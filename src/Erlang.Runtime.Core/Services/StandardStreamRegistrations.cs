using System.ComponentModel;

namespace Erlang;

internal static class StandardStreamRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 1, 2 })
            registry.Register(
                ErlangModuleNames.Module,
                ErlangStandardStreamNames.DisplayString,
                arity,
                Display
            );
    }

    private static ValueTask<Term> Display(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            string device = arguments.Count == 1 ? ErlangStandardStreamNames.StandardError :
                arguments[0] is Atom atom ? atom.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);
            context.Runtime.StandardStreams.Write(device, arguments[^1]);

            return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            throw InvalidArguments(context, arguments, null);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or NotSupportedException or UnauthorizedAccessException or Win32Exception)
        {
            throw InvalidArguments(context, arguments, exception.Message);
        }
    }

    private static ErlangException InvalidArguments(ProcessContext context, IReadOnlyList<Term> arguments, string? cause)
    {
        var info = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(StandardStreamErrorNames.ModuleKey), Term.A(StandardStreamErrorNames.Formatter))
        };
        if (cause is not null)
            info.Add(new(Term.A(StandardStreamErrorNames.CauseKey), Term.String(cause)));

        return new(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(info), ErlangStandardStreamNames.DisplayString)
        };
    }
}
