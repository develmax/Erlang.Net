using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class DriverExternal455Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var observed = new TaskCompletionSource<(Term Value, byte[] Local)>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Term value = Term.Tuple(context.Self, runtime.MakeReference());
                var encoded = (BitString)await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    DriverExternal455Names.Encode,
                    value,
                    Term.List(Term.A(DriverExternal455Names.Local))
                );
                observed.TrySetResult((value, encoded.ToArray()));

                return Nil.Value;
            }
            catch (Exception failure)
            {
                observed.TrySetException(failure);
                throw;
            }
        });
        var (value, local) = await observed.Task.WaitAsync(DriverExternal455Names.Deadline);
        _ = await process.Completion.WaitAsync(DriverExternal455Names.Deadline);
        MethodInfo decode = typeof(ProcessRuntime).GetMethod(DriverExternal455Names.Decode,
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        byte[] ordinary = (byte[])decode.Invoke(runtime, [local])!;
        if (!ExternalTermFormat.Decode(ordinary).Equals(value)) throw new InvalidOperationException(DriverExternal455Names.Failure);
        byte[] trailing = [.. local, 0];
        byte[] withSuffix = (byte[])decode.Invoke(runtime, [trailing])!;
        if (!ExternalTermFormat.Decode(withSuffix).Equals(value)) throw new InvalidOperationException(DriverExternal455Names.Failure);
        byte[] damaged = local.ToArray();
        damaged[^1] ^= 1;
        foreach (byte[] invalid in new byte[][] { damaged, local[..^1] })
        {
            try
            {
                _ = decode.Invoke(runtime, [invalid]);
                throw new InvalidOperationException(DriverExternal455Names.Failure);
            }
            catch (TargetInvocationException failure) when (failure.InnerException is ErlangException guest
                && guest.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            { }
        }
    }
}
