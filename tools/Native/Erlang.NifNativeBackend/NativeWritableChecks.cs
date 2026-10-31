using Erlang.Differential;

namespace Erlang.NifNativeBackend;

public static class NativeWritableChecks
{
    public static async Task SourceLifecycle()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(NativeWritableCaseNames.Source, NativeWritableCaseNames.File);
            var runtime = NativeOwned516Profile.Create(owned);
            var snapshots = new Dictionary<int, NativeWritableObservation>();
            compiled.Definition.Register(runtime.Modules);
            runtime.Modules.Register(
                NativeWritableCaseNames.Probe,
                NativeWritableCaseNames.Checkpoint,
                1,
                (context, arguments) =>
            {
                int phase = checked((int)((Integer)arguments[0]).Value);
                snapshots.Add(phase, NativeWritableTestAccess.Capture(context, phase is 5 or 6));

                return ValueTask.FromResult<Term>(Term.A(NativeWritableCaseNames.Ok));
            }
            );
            try
            {
                Term result = await CpuStatisticsAccess.Actor<Term>(runtime, async context =>
                    await runtime.Modules.Call(context, NativeWritableCaseNames.Module, NativeWritableCaseNames.Run));
                NativeWritableTestAccess.Require(result is TupleTerm { Items.Count: 4 });
                var tuple = (TupleTerm)result;
                NativeWritableTestAccess.Require(((BitString)tuple.Items[0]).ToArray().SequenceEqual(new byte[] { 7 })
                    && ((BitString)tuple.Items[1]).ToArray().SequenceEqual(new byte[] { 7, 8 })
                    && ((BitString)tuple.Items[2]).ToArray().SequenceEqual(new byte[] { 7, 99 }));
                byte[] grown = ((BitString)tuple.Items[3]).ToArray();
                NativeWritableTestAccess.Require(grown.Length == 258 && grown[0] == 7 && grown[1] == 8 && grown.Skip(2).All(value => value == 0));
                CheckSnapshots(snapshots);
            }
            catch
            {
                foreach (var pair in snapshots) NativeWritableTestAccess.Dump(pair.Key, pair.Value);
                throw;
            }
            finally
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(NativeWritableCaseNames.Timeout);
            }
            foreach (OrdinaryBinaryBacking owner in snapshots.Values.SelectMany(snapshot => snapshot.Owners).Select(item => item.Owner).Distinct())
                await owner.Retired.WaitAsync(NativeWritableCaseNames.Timeout);
        }
    }

    private static void CheckSnapshots(IReadOnlyDictionary<int, NativeWritableObservation> snapshots)
    {
        var first = snapshots[1];
        var second = snapshots[2];
        var alias = snapshots[3];
        var growth = snapshots[4];
        NativeWritableTestAccess.Require(first.Owners.Count == 1 && first.Headers.Count == 1 && first.PhysicalWords == 8
            && first.Regular.Count == 0 && first.Writable.Count == 1);
        var original = first.Owners.Single();
        NativeWritableTestAccess.Require(original.Flags == 12 && original.Apparent == 1 && original.Capacity == 256 && original.References == 1
            && (first.Headers[0].Base & 3) == 3 && first.Headers[0].Start == 0 && first.Headers[0].End == 8);
        NativeWritableTestAccess.Require(second.Owners.Count == 1 && second.Headers.Count == 2 && second.PhysicalWords == 13
            && second.Owners[0].BinRef == original.BinRef && ReferenceEquals(second.Owners[0].Owner, original.Owner)
            && second.Owners[0].Capacity == 256 && second.Owners[0].Apparent == 2
            && (second.Headers[0].Base & 3) == 2 && (second.Headers[1].Base & 3) == 3);
        NativeWritableTestAccess.Require(alias.Owners.Count == 2 && alias.Headers.Count == 3 && alias.PhysicalWords == 21
            && alias.Writable.Count == 2 && alias.Regular.Count == 0);
        var moved = growth.Owners.Single(item => ReferenceEquals(item.Owner, original.Owner));
        NativeWritableTestAccess.Require(growth.Owners.Count == 2 && growth.Headers.Count == 4 && growth.PhysicalWords == 26
            && moved.Data != original.Data && moved.Capacity == 516 && moved.Apparent == 258 && moved.Flags == 12
            && (growth.Headers[0].Base & ~(nuint)3) != (nuint)moved.Data);
        var active = snapshots[5].Owners.Single(item => ReferenceEquals(item.Owner, original.Owner));
        var inactive = snapshots[6].Owners.Single(item => ReferenceEquals(item.Owner, original.Owner));
        NativeWritableTestAccess.Require(active.Flags == 4 && active.Capacity == 516 && inactive.Flags == 4 && inactive.Capacity == 283
            && inactive.Data != active.Data && snapshots[6].Writable.Count == 2 && snapshots[6].Regular.Count == 0);
    }

    public static async Task PartialByte()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(NativeWritableCaseNames.Source, NativeWritableCaseNames.File);
            await using var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            NativeWritableObservation? snapshot = null;
            runtime.Modules.Register(
                NativeWritableCaseNames.Probe,
                NativeWritableCaseNames.Checkpoint,
                1,
                (context, _) =>
            {
                snapshot = NativeWritableTestAccess.Capture(context, false);

                return ValueTask.FromResult<Term>(Term.A(NativeWritableCaseNames.Ok));
            }
            );
            Term result = await CpuStatisticsAccess.Actor<Term>(runtime, async context =>
                await runtime.Modules.Call(context, NativeWritableCaseNames.Module, NativeWritableCaseNames.BitRun));
            if (snapshot is not null) NativeWritableTestAccess.Dump(8, snapshot);
            Console.WriteLine(result);
            NativeWritableTestAccess.Require(snapshot is not null && snapshot.Owners.Count == 1 && snapshot.Headers.Count == 2 && snapshot.PhysicalWords == 13
                && snapshot.Owners[0].Capacity == 256 && snapshot.Owners[0].Apparent == 2 && snapshot.Headers[1].End == 9);
            NativeWritableTestAccess.Require(result is TupleTerm { Items.Count: 2 } tuple && tuple.Items[1] is BitString bits && bits.BitLength == 9
                && bits.ToArray().SequenceEqual(new byte[] { 7, 128 }));
        }
    }

    public static async Task CloseWaitingSource()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(NativeWritableCaseNames.Source, NativeWritableCaseNames.File);
            var runtime = NativeOwned516Profile.Create(owned);
            compiled.Definition.Register(runtime.Modules);
            var ready = new TaskCompletionSource<NativeWritableObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task? unwound = null;
            runtime.Modules.Register(
                NativeWritableCaseNames.Probe,
                NativeWritableCaseNames.Checkpoint,
                1,
                (context, _) =>
            {
                unwound = context.Unwound.Task;
                ready.TrySetResult(NativeWritableTestAccess.Capture(context, false));

                return ValueTask.FromResult<Term>(Term.A(NativeWritableCaseNames.Ok));
            }
            );
            ProcessHandle actor = runtime.Spawn(async context =>
                await runtime.Modules.Call(context, NativeWritableCaseNames.Module, NativeWritableCaseNames.Wait));
            try
            {
                var actual = await ready.Task.WaitAsync(NativeWritableCaseNames.Timeout);
                NativeWritableTestAccess.Require(actual.Owners.Count == 1 && !actual.Owners[0].Owner.Retired.IsCompleted);
                await runtime.DisposeAsync().AsTask().WaitAsync(NativeWritableCaseNames.Timeout);
                await actor.Completion.WaitAsync(NativeWritableCaseNames.Timeout);
                await unwound!.WaitAsync(NativeWritableCaseNames.Timeout);
                await actual.Owners[0].Owner.Retired.WaitAsync(NativeWritableCaseNames.Timeout);
            }
            finally
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(NativeWritableCaseNames.Timeout);
            }
        }
    }

    // External original collector supplies its EXACT fresh unchanged worker BEAM
    // and COMPLETE outcome. No historical bytes/expected values are fabricated.
    public static async Task<Term> ReplayOriginalBeam(byte[] beam, bool owned)
    {
        await using var runtime = NativeOwned516Profile.Create(owned);
        var image = runtime.Modules.PrepareBeam(beam);
        NativeWritableTestAccess.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);

        return await CpuStatisticsAccess.Actor<Term>(
            runtime,
            async context =>
            await runtime.Modules.Call(context, NativeWritableCaseNames.ProtocolModule, NativeWritableCaseNames.Run)
        );
    }
}
