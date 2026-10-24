using Erlang;
namespace Erlang.Tests;

internal static class InternalSpawnRequest429HostAccess
{
    internal static ErlangFunction Body(ProcessRuntime runtime)
    {
        var type = typeof(ProcessRuntime).Assembly.GetType(InternalSpawnRequest429HostNames.NativeType, true)!;
        object?[] args = [runtime, InternalSpawnRequest429HostNames.Module, InternalSpawnRequest429HostNames.Function, 4, null];
        Require((bool)type.GetMethod(InternalSpawnRequest429HostNames.Capture, InternalSpawnRequest429HostNames.Flags)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalSpawnRequest429HostNames.Failure);
    }

    internal static Term[] Arguments(Term options) => [Term.A(InternalSpawnRequest429HostNames.TargetModule), Term.A(InternalSpawnRequest429HostNames.Target), Nil.Value, options];

    internal static async Task<Term> ErrorReply(ProcessContext context, ReferenceTerm reference)
    {
        Term? value = await context.Mailbox.ReceiveAsync<Term>(
            term => term is TupleTerm { Items: [_, var id, _, _] } && id.Equals(reference) ? term : null,
            InternalSpawnRequest429HostNames.Deadline,
            context.Cancellation
        );
        Require(value is TupleTerm);

        return value!;
    }
}
