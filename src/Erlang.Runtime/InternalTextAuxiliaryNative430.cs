namespace Erlang;

internal static class InternalTextAuxiliaryNative430
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        bool text = function == InternalTextAuxiliaryNames430.Text && arity == 2;
        if (owner is null || module != InternalTextAuxiliaryNames430.Module || (!text && !(function == InternalTextAuxiliaryNames430.Auxiliary && arity == 0))) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity) throw new InvalidOperationException(InternalTextAuxiliaryNames430.Foreign);
            owner.RequireInternalPortActor416(context);
            if (!text) return ValueTask.FromResult<Term>(Term.I(owner.AuxiliaryParticipants430(context)));
            int precision;
            if (arguments[1] is Atom { Name: InternalTextAuxiliaryNames430.Undefined }) precision = int.MaxValue;
            else if (arguments[1] is Integer number && number.Value >= InternalTextAuxiliaryNames430.MinimumPrecision && number.Value < int.MaxValue) precision = (int)number.Value;
            else throw context.CaptureNativeFailure(
                new ErlangException(ErlangErrorReasons.BadArgument),
                module,
                function,
                arguments
            );
            byte[] bytes = new TermDisplayWriter(owner).WriteLimited430(arguments[0], precision, context.Cancellation);

            return ValueTask.FromResult(Cons.From(bytes.Select(value => Term.I(value))));
        };

        return true;
    }
}
