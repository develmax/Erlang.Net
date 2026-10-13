using System.Numerics;

namespace Erlang;

public static class ErlangOperatorValues
{
    public static Term Unary(string operation, Term value) => operation switch
    {
        ErlangOperators.Plus when value is Integer or FloatTerm => value,
        ErlangOperators.Minus when value is FloatTerm number => new FloatTerm(-number.Value),
        ErlangOperators.Minus => CoreModules.Arithmetic(operation, Term.I(0), value),
        ErlangOperators.Not => CoreModules.Boolean(!CoreModules.Bool(value)),
        ErlangOperators.BitwiseNot when value is Integer number => new Integer(~number.Value),
        _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
    };

    public static Term Binary(
        string operation,
        Term first,
        Term second,
        ProcessContext? context
    ) => operation switch
    {
        ErlangOperators.And => CoreModules.Boolean(CoreModules.Bool(first) & CoreModules.Bool(second)),
        ErlangOperators.Or => CoreModules.Boolean(CoreModules.Bool(first) | CoreModules.Bool(second)),
        ErlangOperators.Xor => CoreModules.Boolean(CoreModules.Bool(first) ^ CoreModules.Bool(second)),
        ErlangOperators.NumericEqual => CoreModules.Boolean(first.NumericEquals(second)),
        ErlangOperators.NumericNotEqual => CoreModules.Boolean(!first.NumericEquals(second)),
        ErlangOperators.ExactEqual => CoreModules.Boolean(first.Equals(second)),
        ErlangOperators.ExactNotEqual => CoreModules.Boolean(!first.Equals(second)),
        ErlangOperators.Less => CoreModules.Boolean(first.CompareTo(second) < 0),
        ErlangOperators.Greater => CoreModules.Boolean(first.CompareTo(second) > 0),
        ErlangOperators.LessOrEqual => CoreModules.Boolean(first.CompareTo(second) <= 0),
        ErlangOperators.GreaterOrEqual => CoreModules.Boolean(first.CompareTo(second) >= 0),
        ErlangOperators.Append => Cons.From(Cons.Items(first), second),
        ErlangOperators.SubtractList => ListBasicOperations.Subtract(first, second),
        ErlangOperators.Send when context is not null => Send(context, first, second),
        _ => CoreModules.Arithmetic(operation, first, second)
    };

    public static async ValueTask<bool> TrySendAsync(
        ProcessContext context,
        Term target,
        Term message,
        bool priority = false,
        bool noSuspend = false
    )
    {
        if (target is PortTerm || target is Atom name && context.Runtime.WhereIs(name.Name) is PortTerm)
        {
            context.SavedCalls.Sent();

            return await context.Runtime.Ports.SendAsync(
                target,
                message,
                noSuspend,
                context.Cancellation
            );
        }
        Send(
            context,
            target,
            message,
            priority
        );

        return true;
    }

    public static Term Send(
        ProcessContext context,
        Term target,
        Term message,
        bool priority = false
    )
    {
        if (target is PortTerm || target is Atom portName && context.Runtime.WhereIs(portName.Name) is PortTerm)
        {
            context.SavedCalls.Sent();

            return context.Runtime.Ports.Send(target, message);
        }
        if (target is ReferenceTerm reference)
        {
            context.SavedCalls.Sent();

            return context.Runtime.SendAlias(
                reference,
                message,
                priority,
                context
            );
        }
        if (target is TupleTerm { Items.Count: 2 } address && address.Items[0] is Atom nameAtom && address.Items[1] is Atom nodeAtom)
        {
            context.SavedCalls.Sent();
            if (nodeAtom.Name == context.Runtime.Node && context.Runtime.WhereIs(nameAtom.Name) is Pid namedPid)
                context.Runtime.SendTraced(
                    context,
                    namedPid,
                    message,
                    address
                );

            return message;
        }
        Pid recipient = target is Pid pid ? pid : target is Atom name && context.Runtime.WhereIs(name.Name) is Pid registered ? registered : throw new ErlangException(ErlangErrorReasons.BadArgument);
        context.SavedCalls.Sent();

        return context.Runtime.SendTraced(context, recipient, message);
    }
}
