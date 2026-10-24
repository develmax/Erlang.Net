using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class BeamStoredFunction406Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BeamStoredFunction406Names.Failure);
    }

    internal static object Property(object target, string name)
        => target.GetType().GetProperty(name, BeamStoredFunction406Names.Members)!.GetValue(target)!;

    internal static object CallFactory(string name, params object[] arguments)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(BeamStoredFunction406Names.FactoryType, true)!
                .GetMethod(name, BeamStoredFunction406Names.Static)!.Invoke(null, arguments)!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }

    internal static byte[] Bytes()
    {
        Assembly assembly = typeof(BeamStoredFunction406Access).Assembly;
        string suffix = BeamClosureHostNames.ResourceSeparator + BeamClosureHostNames.Module + BeamClosureHostNames.BeamExtension;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamClosureHostNames.Hash);

        return bytes;
    }

    internal static PreparedBeamImage Publish(ProcessRuntime runtime)
    {
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(Bytes());
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);

        return prepared;
    }

    internal static int Active(ProcessContext context, ModuleCodeImage image)
        => ((Dictionary<ModuleCodeImage, int>)typeof(ProcessContext)
            .GetField(BeamStoredFunction406Names.ActiveCode, BeamStoredFunction406Names.Members)!
            .GetValue(context)!).GetValueOrDefault(image);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool Alive(WeakReference<object> weak) => weak.TryGetTarget(out _);

    internal static void RequireCollected(WeakReference<object> weak)
    {
        for (int index = 0; index < BeamStoredFunction406Names.CollectionAttempts && Alive(weak); index++)
            EtsFoundation405Access.Collect();
        Require(!Alive(weak));
    }

    internal static FunctionTerm Read(
        object store,
        ProcessContext context,
        Term table,
        long slot
    )
        => (FunctionTerm)((TupleTerm)EtsFoundation405Access.Read(
            store,
            context,
            table,
            slot
        )).Items[0];
}
