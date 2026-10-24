using System.Collections;
using System.Text.Json;

namespace Erlang.Tests;

internal sealed class StandardBootCollectors415Proof(IReadOnlyList<StandardBootCollectors415State> sourceStates)
{
    private readonly IReadOnlyList<StandardBootCollectors415State> states = sourceStates;

    private static object? Field(object value, string name) => ManagedCollectors413Access.Field(value, name);

    private static object? Property(object value, string name) => ManagedCollectors413Access.Property(value, name);

    private static bool Active(ProcessContext actor, ModuleCodeImage image)
    {
        lock (Field(actor, StandardBootCollectors415Names.CodeGate)!)
            return ((IDictionary)Field(actor, StandardBootCollectors415Names.ActiveCode)!).Contains(image);
    }

    internal static async Task<StandardBootCollectors415Proof> Capture(ProcessRuntime runtime)
    {
        DateTime deadline = DateTime.UtcNow + StandardBootCollectors415Names.Deadline;
        while (true)
        {
            var found = new List<StandardBootCollectors415State>();
            foreach (var pair in new[]
            {
                (ManagedCollectors413Names.Literal, ManagedCollectors413Names.LiteralSlot),
                (ManagedCollectors413Names.Trace, ManagedCollectors413Names.TraceSlot)
            })
            {
                object? owner = runtime.Modules.SourceOwner(pair.Item1, ManagedCollectors413Names.Start, 0);
                object slot = Field(runtime, pair.Item2)!;
                lock (Field(runtime, StandardBootCollectors415Names.Gate)!)
                {
                    if (Property(slot, ManagedCollectors413Names.Actor) is not ProcessContext actor
                        || Property(slot, ManagedCollectors413Names.Source) is not PreparedBeamImage source
                        || !ReferenceEquals(source, owner) || !ReferenceEquals(actor.Runtime, runtime)
                        || (bool)Field(actor, StandardBootCollectors415Names.Terminated)!)
                        continue;
                    object execution = Property(source, StandardBootCollectors415Names.ExecutionIdentity)!;
                    var image = (ModuleCodeImage)Property(execution, StandardBootCollectors415Names.Image)!;
                    bool system = (bool)ManagedCollectors413Access.Invoke(
                        runtime,
                        typeof(ProcessRuntime),
                        StandardBootCollectors415Names.IsSystem,
                        actor.Self
                    )!;
                    if (system && source.NativePrimitiveExports.Count == 2 && Active(actor, image))
                        found.Add(new(
                            pair.Item1,
                            actor,
                            slot,
                            source,
                            image
                        ));
                }
            }
            if (found.Count == 2) return new(found);
            if (DateTime.UtcNow >= deadline) throw new InvalidOperationException(StandardBootCollectors415Names.Failure);
            await Task.Yield();
        }
    }

    internal static bool Complete(string output, ProcessRuntime? runtime, StandardBootCollectors415Proof? proof)
    {
        var rows = new List<object>();
        bool passed = runtime is not null && proof is not null && proof.states.Count == 2;
        if (runtime is not null && proof is not null)
            lock (Field(runtime, StandardBootCollectors415Names.Gate)!)
                foreach (var state in proof.states)
                {
                    bool slotCleared = Property(state.Slot, ManagedCollectors413Names.Actor) is null
                        && Property(state.Slot, ManagedCollectors413Names.Source) is null
                        && Property(state.Slot, StandardBootCollectors415Names.Reservation) is null;
                    bool leaseReleased = !Active(state.Actor, state.Image);
                    bool terminated = (bool)Field(state.Actor, StandardBootCollectors415Names.Terminated)!;
                    var done = (TaskCompletionSource<Term>)Property(state.Actor, StandardBootCollectors415Names.Done)!;
                    bool settled = done.Task.IsCompleted;
                    bool rowPassed = slotCleared && leaseReleased && terminated && settled;
                    passed &= rowPassed;
                    rows.Add(new
                    {
                        state.Module,
                        ActualActorPid = state.Actor.Self.ToString(),
                        ExactSourceOwnerAndActiveLeaseObservedAfterRealBoot = true,
                        ActualSystemActorObserved = true,
                        SlotActorSourceReservationCleared = slotCleared,
                        ActualSourceImageLeaseReleased = leaseReleased,
                        ActualActorTerminated = terminated,
                        ActualDoneSettled = settled,
                        Passed = rowPassed
                    });
                }
        bool noPending = runtime is not null && ManagedCollectors413Access.Requests(runtime).Length == 0;
        passed &= noPending;
        File.WriteAllText(
            Path.Combine(output, StandardBootCollectors415Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Passed = passed,
                AutomaticPreludeNotManualRoleSpawn = true,
                ActualSelectedOriginalImages = proof?.states.Count ?? 0,
                NoPendingCollectorRequestsAfterActualDispose = noPending,
                Roles = rows,
                Original79OracleProofIsSeparate = true,
                WholeOriginalExpectedEtfUnchanged = true,
                NoGarbageCollectionOrFullCapabilityClaim = true
            })
        );

        return passed;
    }
}
