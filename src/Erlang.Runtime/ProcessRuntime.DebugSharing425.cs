using System.Numerics;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term ExecuteDebugSharing425(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugSharingNames425.Foreign);
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
        int arity = function == DebugSharingNames425.Copy ? 2 : 1;
        if (arguments.Count != arity) throw new ArgumentException(DebugSharingNames425.Arity);
        try
        {
            if (function == DebugSharingNames425.Copy)
            {
                if (arguments[1] is not Atom flag || flag.Name is not (ErlangBooleanAtoms.True or ErlangBooleanAtoms.False))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return DebugSharingCopy425.Copy(this, arguments[0], flag.Name == ErlangBooleanAtoms.True);
            }
            ulong words = function == DebugSharingNames425.Flat
                ? DebugSharingSize425.Flat(arguments[0]) : DebugSharingSize425.Shared(this, arguments[0]);

            return new Integer(new BigInteger(words));
        }
        catch (ErlangException failure)
        {
            throw context.CaptureNativeFailure(
                failure,
                DebugSharingNames425.Module,
                function,
                arguments
            );
        }
    }
}
