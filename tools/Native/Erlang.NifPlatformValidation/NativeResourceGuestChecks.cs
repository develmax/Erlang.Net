using System.Text.Json;
using System.Runtime.InteropServices;
using Erlang;

internal static class NativeResourceGuestChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(new ProcessRuntimeOptions { OwnedTermsPilot506 = true });
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            ResourceReferenceTerm? atomics = null, counters = null;
            try
            {
                atomics = (ResourceReferenceTerm)await new AtomicsConstructorBinding413(runtime, runtime.Modules).Body(context, [new Integer(2), new Integer(1)]);
                NativeResourceStorageChecks.Require(CountersNative421.TryCapture(
                    runtime,
                    CountersNames421.Module,
                    CountersNames421.New,
                    1,
                    out var createCounters
                ));
                counters = (ResourceReferenceTerm)await createCounters(context, [new Integer(2)]);
                Task atomicsRetired, countersRetired;
                using (var a = runtime.ReferenceResources.AcquireAtomics413(atomics)!)
                    atomicsRetired = a.Value.Array.Retired586;
                using (var c = runtime.ReferenceResources.AcquireCounters421(counters)!)
                    countersRetired = c.Value.Array.Retired596;
                var heap = context.TermHeap506;
                using (var roots = heap.Enter())
                {
                    int aSlot = roots.Reserve(), cSlot = roots.Reserve();
                    roots.Set(aSlot, atomics);
                    roots.Set(cSlot, counters);
                    atomics.AtomicsHost586!.Dispose();
                    counters.CountersHost596!.Dispose();
                    Term a = roots.Read(aSlot), c = roots.Read(cSlot);
                    NativeResourceStorageChecks.Require(heap.PhysicalWordSnapshot526.Blocks == 2 && heap.PhysicalWordSnapshot526.LiveRawNumericWords556 == 4);
                    NativeResourceStorageChecks.Require(AtomicsNative413.TryCapture(
                        runtime,
                        AtomicsNames413.Module,
                        AtomicsNames413.Put,
                        3,
                        out var put
                    ));
                    NativeResourceStorageChecks.Require(AtomicsNative413.TryCapture(
                        runtime,
                        AtomicsNames413.Module,
                        AtomicsNames413.AddGet,
                        3,
                        out var add
                    ));
                    await put(context, [a, new Integer(1), new Integer(long.MaxValue)]);
                    NativeResourceStorageChecks.Require((await add(context, [a, new Integer(1), new Integer(1)])).Equals(new Integer(long.MinValue)));
                    NativeResourceStorageChecks.Require(CountersNative421.TryCapture(
                        runtime,
                        CountersNames421.Module,
                        CountersNames421.Add,
                        3,
                        out var counterAdd
                    ));
                    NativeResourceStorageChecks.Require(CountersNative421.TryCapture(
                        runtime,
                        CountersNames421.Module,
                        CountersNames421.Get,
                        2,
                        out var counterGet
                    ));
                    await counterAdd(context, [c, new Integer(1), new Integer(long.MaxValue)]);
                    heap.CollectGeneration508(false, 2);
                    heap.Collect();
                    NativeResourceStorageChecks.Require((await counterGet(context, [c, new Integer(1)])).Equals(new Integer(long.MaxValue)));
                    roots.Clear(aSlot);
                    roots.Clear(cSlot);
                    heap.Collect();
                }
                await Task.WhenAll(atomicsRetired, countersRetired).WaitAsync(TimeSpan.FromSeconds(30));
                for (int attempt = 0; attempt < 1000 && (runtime.AtomicsStorage413.LiveArrays != 0 || runtime.CountersStorage421.LiveArrays != 0); attempt++)
                    await Task.Delay(5);
                NativeResourceStorageChecks.Require(runtime.AtomicsStorage413.LiveArrays == 0 && runtime.CountersStorage421.LiveArrays == 0);
                done.TrySetResult();
            }
            catch (Exception error)
            {
                done.TrySetException(error);
            }
            finally
            {
                atomics?.AtomicsHost586?.Dispose();
                counters?.CountersHost596?.Dispose();
            }

            return Nil.Value;
        });
        await done.Task.WaitAsync(TimeSpan.FromSeconds(60));
        await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        Console.WriteLine(JsonSerializer.Serialize(new { Passed = true, Architecture = RuntimeInformation.ProcessArchitecture.ToString(), PointerBytes = IntPtr.Size, ActualAtomicsCountersPrimitives = true, MagicHeapRoots = true, MinorMajorGc = true, PhysicalRetirementCompleted = true }));
    }
}
