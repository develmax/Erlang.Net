using System.Reflection;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class ReferenceResourceHostChecks
{
    public static async Task Run()
    {
        await LiveCopiesAndPrepared();
        await FactoryIdentityChecks();
        await EncodedBytesDoNotPin();
        await CopyOwnsAndClosureRace();
        await ShutdownCloses();
    }

    private static async Task FactoryIdentityChecks()
    {
        await using var runtime = new ProcessRuntime();
        ReferenceTerm original = runtime.MakeReference();
        byte[] bytes = ExternalTermFormat.Encode(original);
        ReferenceTerm plain = (ReferenceTerm)ExternalTermFormat.Decode(bytes);
        Require(plain is not ResourceReferenceTerm && plain.Equals(original));
        Term fallback = ExternalTermFormat.DecodeOne(bytes, new EtfDecodeOptions { ReferenceFactory = _ => null! }).Term;
        Require(fallback is ReferenceTerm and not ResourceReferenceTerm && fallback.Equals(original));
        Func<ReferenceTerm, ReferenceTerm>[] factories =
        [
            reference => new(reference.Node, reference.Words.Select((word, index) => index == 0 ? word ^ 1u : word), reference.Creation),
            reference => new(ReferenceResourceHostNames.OtherNode, reference.Words, reference.Creation),
            reference => new(reference.Node, reference.Words, reference.Creation + 1)
        ];
        foreach (var factory in factories)
        {
            bool rejected = false;
            try
            {
                ExternalTermFormat.DecodeOne(bytes, new EtfDecodeOptions { ReferenceFactory = factory });
            }
            catch (ErlangException error)
            {
                Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                rejected = true;
            }
            Require(rejected);
        }
    }

    private static async Task LiveCopiesAndPrepared()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(Resource(BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension));
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreatePrepared(image);
        Require(reference.ResourceKind == ReferenceResourceKind.PreparedBeam);
        Require(runtime.ReferenceResources.AcquirePrepared(new ReferenceTerm(reference.Node, reference.Words, reference.Creation)) is null);
        Require(foreign.ReferenceResources.AcquirePrepared(reference) is null);
        Require(runtime.ReferenceResources.AcquireBinary(reference) is null);
        Require(runtime.ReferenceResources.AcquirePrepared(runtime.MakeReference()) is null);
        Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(reference.Node, reference.Words, reference.Creation)) is not ResourceReferenceTerm);
        byte[] bytes = ExternalTermFormat.Encode(Term.Tuple(reference, Term.List(reference)));
        var options = new EtfDecodeOptions
        {
            LocalNode = runtime.Node,
            ReferenceFactory = runtime.ReferenceResources.Rehydrate
        };
        var plain = (TupleTerm)ExternalTermFormat.Decode(bytes);
        Require(plain.Items[0] is ReferenceTerm and not ResourceReferenceTerm);
        var tuple = (TupleTerm)ExternalTermFormat.DecodeOne(bytes, options).Term;
        Require(tuple.Items[0] is ResourceReferenceTerm copy && copy.Equals(reference) && copy.GetHashCode() == reference.GetHashCode());
        using (var lease = runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)tuple.Items[0]))
            Require(lease is not null && ReferenceEquals(lease.Value, image));
        var external = (TupleTerm)ExternalTermFormat.DecodeOne(bytes, options with { ReferenceFactory = foreign.ReferenceResources.Rehydrate }).Term;
        Require(external.Items[0] is ReferenceTerm and not ResourceReferenceTerm);
        Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(reference.Node, reference.Words, reference.Creation + 1)) is not ResourceReferenceTerm);
        Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(reference.Node, reference.Words.Concat(new uint[] { 0 }), reference.Creation)) is not ResourceReferenceTerm);
        await Task.WhenAll(Enumerable.Range(0, ReferenceResourceHostNames.ConcurrentCopies).Select(_ => Task.Run(() =>
        {
            var restored = (TupleTerm)ExternalTermFormat.DecodeOne(bytes, options).Term;
            using var lease = runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)restored.Items[0]);
            Require(lease is not null && ReferenceEquals(lease.Value, image));
        })));
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                ResourceReferenceTerm textOnly = runtime.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary);
                Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(textOnly.Node, textOnly.Words, textOnly.Creation)) is not ResourceReferenceTerm);
                Term onlyText = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ReferenceTextNamesForHost.ToList,
                    textOnly
                );
                Term fromOnlyText = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ReferenceTextNamesForHost.FromList,
                    onlyText
                );
                using var textLease = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)fromOnlyText);
                Require(fromOnlyText is ResourceReferenceTerm && textLease is not null && textLease.Value.ToArray().SequenceEqual(ReferenceResourceHostNames.Binary));
                Term text = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ReferenceTextNamesForHost.ToList,
                    reference
                );
                Term restored = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ReferenceTextNamesForHost.FromList,
                    text
                );
                Require(restored is ResourceReferenceTerm);
                Term binary = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ReferenceTextNamesForHost.ToBinary,
                    reference
                );
                Term decoded = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ReferenceTextNamesForHost.FromBinary,
                    binary
                );
                Require(decoded is ResourceReferenceTerm && decoded.Equals(reference));
                using var lease = runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)restored);
                Require(lease is not null);
                Require(runtime.Modules.PublishPreparedBeam(lease!.Value).Status == BeamPublicationStatus.Published);
                Term actual = await runtime.Modules.Call(context, image.Name, ReferenceResourceHostNames.Entry);
                Require(ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamPreparedHostNames.Result), actual)).SequenceEqual(Resource(image.Name + BeamPreparedHostNames.ExpectedExtension)));
                Require(runtime.ReferenceResources.AcquirePrepared(reference) is null && runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)decoded) is null);
                Reject<InvalidOperationException>(() => _ = lease.Value);
                GC.KeepAlive(textOnly);

                return Term.A(ErlangBooleanAtoms.True);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                throw;
            }
        });
        Require((await actor.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
        ResourceReferenceTerm binaryReference = runtime.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary);
        using var binaryLease = runtime.ReferenceResources.AcquireBinary(binaryReference);
        Require(binaryLease is not null && binaryLease.Value.ToArray().SequenceEqual(ReferenceResourceHostNames.Binary));
        Require(runtime.ReferenceResources.AcquirePrepared(binaryReference) is null);
        binaryLease!.Dispose();
        Reject<ObjectDisposedException>(() => _ = binaryLease.Value);
        GC.KeepAlive(reference);
    }

    private static async Task EncodedBytesDoNotPin()
    {
        await using var runtime = new ProcessRuntime();
        var abandoned = EncodeOnly(runtime);
        for (int attempt = 0; attempt < ReferenceResourceHostNames.GarbageCollectionAttempts; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Require(!abandoned.Weak.IsAlive && runtime.ReferenceResources.LiveResources == 0);
        ReferenceTerm decoded = (ReferenceTerm)ExternalTermFormat.DecodeOne(abandoned.Bytes, new EtfDecodeOptions { ReferenceFactory = runtime.ReferenceResources.Rehydrate }).Term;
        Require(decoded is not ResourceReferenceTerm && runtime.ReferenceResources.AcquireBinary(decoded) is null);
        Require(runtime.ReferenceResources.Rehydrate(runtime.MakeReference()) is not ResourceReferenceTerm);
        GC.KeepAlive(abandoned.Bytes);
    }

    private static async Task CopyOwnsAndClosureRace()
    {
        await using var runtime = new ProcessRuntime();
        var copied = MakeCopy(runtime);
        for (int attempt = 0; attempt < ReferenceResourceHostNames.GarbageCollectionAttempts; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Require(!copied.Original.IsAlive && runtime.ReferenceResources.LiveResources == 1);
        using var lease = runtime.ReferenceResources.AcquireBinary(copied.Copy);
        Require(lease is not null && lease.Value.ToArray().SequenceEqual(ReferenceResourceHostNames.Binary));
        using var obtained = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<ReferenceTerm> restoration = Task.Run(() => (ReferenceTerm)ExternalTermFormat.DecodeOne(
            copied.Bytes,
            new EtfDecodeOptions
            {
                ReferenceFactory = reference =>
                {
                    ReferenceTerm result = runtime.ReferenceResources.Rehydrate(reference);
                    obtained.Set();
                    Require(release.Wait(ReferenceResourceHostNames.BarrierTimeout));

                    return result;
                }
            }
        ).Term);
        try
        {
            Require(obtained.Wait(ReferenceResourceHostNames.BarrierTimeout));
            await runtime.RequestImmediateHalt(0);
        }
        finally
        {
            release.Set();
        }
        ReferenceTerm restored = await restoration;
        Require(restored is ResourceReferenceTerm && runtime.ReferenceResources.AcquireBinary(restored) is null);
        Reject<ObjectDisposedException>(() => _ = lease!.Value);
        GC.KeepAlive(copied.Copy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ResourceReferenceTerm Copy, WeakReference Original, byte[] Bytes) MakeCopy(ProcessRuntime runtime)
    {
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary);
        byte[] bytes = ExternalTermFormat.Encode(reference);
        var copy = (ResourceReferenceTerm)ExternalTermFormat.DecodeOne(bytes, new EtfDecodeOptions { ReferenceFactory = runtime.ReferenceResources.Rehydrate }).Term;

        return (copy, new WeakReference(reference), bytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (byte[] Bytes, WeakReference Weak) EncodeOnly(ProcessRuntime runtime)
    {
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary);

        return (ExternalTermFormat.Encode(reference), new WeakReference(reference));
    }

    private static async Task ShutdownCloses()
    {
        var runtime = new ProcessRuntime();
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary);
        byte[] encoded = ExternalTermFormat.Encode(reference);
        using var lease = runtime.ReferenceResources.AcquireBinary(reference);
        await runtime.RequestImmediateHalt(0);
        Require(runtime.ReferenceResources.LiveResources == 0 && runtime.ReferenceResources.AcquireBinary(reference) is null);
        Reject<ObjectDisposedException>(() => _ = lease!.Value);
        Require(ExternalTermFormat.DecodeOne(encoded, new EtfDecodeOptions { ReferenceFactory = runtime.ReferenceResources.Rehydrate }).Term is not ResourceReferenceTerm);
        Reject<ObjectDisposedException>(() => runtime.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary));
        await runtime.DisposeAsync();
        var disposed = new ProcessRuntime();
        ResourceReferenceTerm other = disposed.ReferenceResources.CreateBinary(ReferenceResourceHostNames.Binary);
        await disposed.DisposeAsync();
        Require(disposed.ReferenceResources.AcquireBinary(other) is null);
    }

    private static byte[] Resource(string name)
    {
        Assembly assembly = typeof(ReferenceResourceHostChecks).Assembly;
        string path = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamPreparedHostNames.ResourceSeparator + name, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(path)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(ReferenceResourceHostNames.Failure);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(ReferenceResourceHostNames.Failure);
    }
}
