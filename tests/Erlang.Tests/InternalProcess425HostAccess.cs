using Erlang;
namespace Erlang.Tests;

internal static class InternalProcess425HostAccess
{
    internal static ErlangFunction Body(ProcessRuntime runtime, string function, int arity)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(InternalProcess425HostNames.NativeType, true)!;
        object?[] arguments = [runtime, InternalProcess425HostNames.Module, function, arity, null];
        Require((bool)type.GetMethod(InternalProcess425HostNames.Capture, InternalProcess425HostNames.Flags)!.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalProcess425HostNames.Failure);
    }

    internal static async Task<Term> Reply(ProcessContext context, ReferenceTerm reference)
    {
        Term? reply = await context.Mailbox.ReceiveAsync<Term>(
            term => term is TupleTerm { Items: [var id, _] } && id.Equals(reference) ? term : null,
            InternalProcess425HostNames.Deadline,
            context.Cancellation
        );
        Require(reply is TupleTerm);

        return ((TupleTerm)reply!).Items[1];
    }

    internal static async Task<Term> Resolve(ProcessContext context, ValueTask<Term> started)
    {
        Term value = await started;

        return value is ReferenceTerm reference ? await Reply(context, reference) : value;
    }
}
