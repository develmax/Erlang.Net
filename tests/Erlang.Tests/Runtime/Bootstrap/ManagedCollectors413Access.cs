using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class ManagedCollectors413Access
{
    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(ManagedCollectors413Names.Failure);
    }

    internal static object? Invoke(
        object? owner,
        Type type,
        string name,
        params object?[] arguments
    )
    {
        try
        {
            return type.GetMethod(name, ManagedCollectors413Names.Members)!.Invoke(owner, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static object? Field(object owner, string name)
        => owner.GetType().GetField(name, ManagedCollectors413Names.Members)!.GetValue(owner);

    internal static object? Property(object owner, string name)
        => owner.GetType().GetProperty(name, ManagedCollectors413Names.Members)?.GetValue(owner)
            ?? owner.GetType().GetField(name, ManagedCollectors413Names.Members)?.GetValue(owner);

    internal static ErlangFunction Capture(
        ProcessRuntime runtime,
        string module,
        string function,
        int arity
    )
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(ManagedCollectors413Names.Native, true)!;
        object?[] args = [runtime, module, function, arity, null];
        Require((bool)Invoke(
            null,
            type,
            ManagedCollectors413Names.Capture,
            args
        )!);

        return (ErlangFunction)args[4]!;
    }

    internal static byte[] Resource(string name)
    {
        using Stream stream = typeof(ManagedCollectors413Access).Assembly.GetManifestResourceStream(
            ManagedCollectors413Names.ResourcePrefix + name) ?? throw new InvalidDataException(ManagedCollectors413Names.Failure);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }

    internal static async Task Initialize(ProcessRuntime runtime)
    {
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Resource(ManagedCollectors413Names.Manifest))!;
        Require(hashes.Count == 2);
        await runtime.InitializeModulesAsync(registry =>
        {
            foreach (string module in new[] { ManagedCollectors413Names.Literal, ManagedCollectors413Names.Trace })
            {
                Require(!registry.HasExport(module, ManagedCollectors413Names.Start, 0));
                byte[] bytes = Resource(module + ManagedCollectors413Names.Beam);
                Require(Convert.ToHexString(SHA256.HashData(bytes)) == hashes[module + ManagedCollectors413Names.Beam]);
                PreparedBeamImage image = registry.PrepareBeam(bytes, new BeamDependencySelection([ManagedCollectors413Names.Erlang, ManagedCollectors413Names.ErtsInternal]));
                Require(image.NativePrimitiveExports.Count == 2);
                Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            }
        });
    }

    internal static ProcessHandle Spawn(ProcessRuntime runtime, ProcessContext parent, string module)
    {
        object?[] args = [parent, module, ManagedCollectors413Names.Start, Array.Empty<Term>(), null];
        Require((bool)Invoke(
            runtime,
            typeof(ProcessRuntime),
            ManagedCollectors413Names.Spawn,
            args
        )!);

        return (ProcessHandle)args[4]!;
    }

    internal static async Task<ProcessContext> Role(ProcessRuntime runtime, string slotName)
    {
        DateTime deadline = DateTime.UtcNow + ManagedCollectors413Names.Deadline;
        while (true)
        {
            object slot = Field(runtime, slotName)!;
            if (Property(slot, ManagedCollectors413Names.Actor) is ProcessContext actor) return actor;
            Require(DateTime.UtcNow < deadline);
            await Task.Yield();
        }
    }

    internal static object[] Requests(ProcessRuntime runtime)
        => ((System.Collections.IEnumerable)Field(runtime, ManagedCollectors413Names.Requests)!).Cast<object>().ToArray();

    internal static IReadOnlyList<Term> Snapshot(ProcessContext context)
        => (IReadOnlyList<Term>)Invoke(context.Mailbox, typeof(Mailbox), ManagedCollectors413Names.Snapshot)!;

    internal static void Cancel(ProcessContext context)
        => ((CancellationTokenSource)Property(context, ManagedCollectors413Names.Stop)!).Cancel();
}
