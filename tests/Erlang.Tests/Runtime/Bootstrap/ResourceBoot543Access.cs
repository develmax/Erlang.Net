using System.Collections;
using System.Reflection;
using static Erlang.Tests.ResourceBoot543Names;

namespace Erlang.Tests;

// Test-only scalar observer of actual live registry membership. No guest entry,
// context AsyncLocal, source-location/argument read or mailbox callback executes.
internal static class ResourceBoot543Access
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo RuntimeGate = typeof(ProcessRuntime).GetField(Gate, Members)!;
    private static readonly FieldInfo RuntimeProcesses = typeof(ProcessRuntime).GetField(Processes, Members)!;
    private static readonly FieldInfo MailboxGate = typeof(Mailbox).GetField(Gate, Members)!;
    private static readonly FieldInfo AdmittedField = typeof(Mailbox).GetField(Admitted, Members)!;
    private static readonly FieldInfo PendingField = typeof(Mailbox).GetField(Pending, Members)!;
    private static readonly FieldInfo DeferredField = typeof(Mailbox).GetField(Deferred, Members)!;
    private static readonly MethodInfo DisplayFrames = typeof(ProcessContext).GetMethod(SnapshotFrames, Members)!;
    private static readonly FieldInfo TerminatedField = typeof(ProcessContext).GetField(Terminated, Members)!;

    internal static object Capture(ProcessRuntime runtime)
    {
        object gate = RuntimeGate.GetValue(runtime)!;
        if (!Monitor.TryEnter(gate))
            return new { RuntimeGateAvailable = false, CompleteMembershipSnapshot = false };
        var captured = new List<(ulong Id, ProcessContext? Context, bool Terminated)>();
        int count;
        try
        {
            IReadOnlyList<Pid> identifiers = runtime.ProcessSnapshot();
            count = identifiers.Count;
            var processes = (IDictionary)RuntimeProcesses.GetValue(runtime)!;
            foreach (Pid pid in identifiers.Take(MaximumActors))
            {
                ProcessContext? context = processes[pid] as ProcessContext;
                captured.Add((pid.Id, context, context is not null && (bool)TerminatedField.GetValue(context)!));
            }
        }
        finally
        {
            Monitor.Exit(gate);
        }
        var rows = new List<object>();
        bool complete = count <= MaximumActors;
        foreach (var actor in captured)
        {
            if (actor.Context is not { } context)
            {
                complete = false;
                rows.Add(new { ActorId = actor.Id, PresentAtLookup = false });
                continue;
            }
            // Existing internal method performs the actual Volatile.Read. Its
            // walk is unchanged; only scalar output serialization is bounded.
            var frames = (IList)DisplayFrames.Invoke(context, null)!;
            var chain = new List<object>();
            foreach (object frame in frames.Cast<object>().Take(MaximumFrames))
            {
                Type type = frame.GetType();
                chain.Add(new
                {
                    Module = (string)type.GetProperty(ModuleName, Members)!.GetValue(frame)!,
                    Function = (string)type.GetProperty(FunctionName, Members)!.GetValue(frame)!,
                    Arity = (int)type.GetProperty(Arity, Members)!.GetValue(frame)!,
                    SourceWrapper = (bool)type.GetProperty(SourceWrapper, Members)!.GetValue(frame)!
                });
            }
            bool chainTruncated = frames.Count > MaximumFrames;
            complete &= !chainTruncated;
            Mailbox mailbox = context.Mailbox;
            object mailboxGate = MailboxGate.GetValue(mailbox)!;
            bool available = Monitor.TryEnter(mailboxGate);
            int? admitted = null, pending = null, deferred = null;
            if (available)
            {
                try
                {
                    admitted = (int)AdmittedField.GetValue(mailbox)!;
                    pending = (int)PendingField.GetValue(mailbox)!;
                    deferred = (int)DeferredField.GetValue(mailbox)!;
                }
                finally
                {
                    Monitor.Exit(mailboxGate);
                }
            }
            complete &= available;
            rows.Add(new
            {
                ActorId = actor.Id,
                PresentAtLookup = true,
                actor.Terminated,
                VisibleFrameCount = frames.Count,
                VisibleFrameChain = chain.ToArray(),
                ChainTruncated = chainTruncated,
                MailboxGateAvailable = available,
                AdmittedMessages = admitted,
                PendingMessages = pending,
                DeferredMessages = deferred
            });
        }

        return new
        {
            RuntimeGateAvailable = true,
            MembershipCount = count,
            ActorsTruncated = count > MaximumActors,
            CompleteMembershipSnapshot = complete,
            AtomicCrossActorSnapshotClaimed = false,
            TimerAsyncLocalUsed = false,
            MailboxCountCallbackUsed = false,
            GuestPayloadsRead = false,
            Actors = rows.ToArray()
        };
    }

}
