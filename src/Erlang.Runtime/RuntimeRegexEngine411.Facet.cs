using System.Collections.Frozen;

namespace Erlang;

public sealed partial class RuntimeRegexEngine411
{
    internal IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> CreateBodies()
    {
        var result = new Dictionary<(string Function, int Arity), ErlangFunction>();
        foreach (var signature in ReFacetNames411.Signatures)
        {
            var binding = new ReNativeBinding413(this, signature);
            result.Add(signature, binding.Body);
        }

        return result.ToFrozenDictionary();
    }

    internal async ValueTask<Term> Invoke(ProcessContext context, (string Function, int Arity) signature, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count != signature.Arity) throw new ArgumentException(ReNames411.Foreign);
        try
        {
            return signature switch
            {
                (ReFacetNames411.Compile, 1) => await CompileAsync(context, arguments[0], Nil.Value).ConfigureAwait(false),
                (ReFacetNames411.Compile, 2) => await CompileAsync(context, arguments[0], arguments[1]).ConfigureAwait(false),
                (ReFacetNames411.Run, 2) => await RunAsync(
                    context,
                    arguments[0],
                    arguments[1],
                    Nil.Value
                ).ConfigureAwait(false),
                (ReFacetNames411.Run, 3) => await RunAsync(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                ).ConfigureAwait(false),
                (ReFacetNames411.Version, 0) => Version(context),
                (ReExtendedNames413.Inspect, 2) => Inspect413(context, arguments[0], arguments[1]),
                (ReExtendedNames413.Import, 1) => await Import413Async(context, arguments[0]).ConfigureAwait(false),
                (ReExtendedNames413.InternalRun, 4) => await InternalRun413Async(context, arguments).ConfigureAwait(false),
                _ => throw new ArgumentException(ReNames411.Foreign)
            };
        }
        catch (ReBadOption411)
        {
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                ReNames411.Module,
                signature.Function,
                arguments,
                new MapTerm(new Dictionary<Term, Term>
                {
                    [Term.A(RuntimeExceptionNames.Module)] = Term.A(ReNames411.Formatter),
                    [Term.A(ReNames411.Cause)] = Term.A(ReNames411.BadOption)
                })
            );
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                ReNames411.Module,
                signature.Function,
                arguments
            );
        }
    }
}
