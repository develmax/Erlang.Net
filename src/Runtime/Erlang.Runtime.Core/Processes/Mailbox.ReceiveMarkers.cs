using System.Numerics;

namespace Erlang;

public sealed partial class Mailbox
{
    private readonly LinkedList<ReceiveMarkerState> receiveMarkers = new();
    private readonly Dictionary<BeamReceiveMarker, ReceiveMarkerState> receiveMarkerTokens = new();
    private ReceiveMarkerState? receiveMarkerSave;
    private long receiveMarkerPriorityRevision;

    private sealed class ReceiveMarkerState
    {
        internal required BeamReceiveMarker Token { get; init; }
        internal required LinkedListNode<Entry> Node { get; init; }
        internal required long PriorityRevision { get; init; }
        internal LinkedListNode<ReceiveMarkerState>? Allocation { get; set; }
        internal BigInteger? ReferenceId { get; set; }
        internal int Passes { get; set; }
        internal int CursorUsers { get; set; }
        internal bool Retired { get; set; }
        internal bool SeenInBeamPriority { get; set; }
    }

    internal BeamReceiveMarker ReserveReceiveMarker()
    {
        ProcessSignals?.Invoke();
        lock (gate)
        {
            var token = new BeamReceiveMarker(this);
            if (HaltRequested?.Invoke() == true || messages.Count == 0)
                return token;
            if (receiveMarkers.Count == MailboxReceiveMarkerParameters.Capacity)
                RetireReceiveMarker(receiveMarkers.First!.Value);
            var state = new ReceiveMarkerState
            {
                Token = token,
                Node = messages.AddLast(new Entry(
                    Nil.Value,
                    null,
                    Control: true,
                    ReceiveMarker: token
                )),
                PriorityRevision = receiveMarkerPriorityRevision
            };
            state.Allocation = receiveMarkers.AddLast(state);
            receiveMarkerTokens.Add(token, state);
            mutationRevision++;

            return token;
        }
    }

    internal void BindReceiveMarker(BeamReceiveMarker marker, Term id)
    {
        lock (gate)
        {
            if (!ReferenceEquals(marker.Owner, this) || !receiveMarkerTokens.TryGetValue(marker, out var state)
                || state.ReferenceId is not null)
                return;
            if (!TryReceiveMarkerReference(id, out BigInteger reference))
            {
                RetireReceiveMarker(state);

                return;
            }
            if (receiveMarkers.Any(other => other.ReferenceId == reference))
                throw new NotSupportedException(MailboxReceiveMarkerParameters.DuplicateReferenceUnsupported);
            state.ReferenceId = reference;
        }
    }

    internal void ClearReceiveMarker(Term id)
    {
        lock (gate)
        {
            if (FindReceiveMarker(id) is { } state)
                RetireReceiveMarker(state);
        }
    }

    internal void UseReceiveMarker(Term id)
    {
        lock (gate)
        {
            if (FindReceiveMarker(id) is not { } state)
                return;
            ValidateReceiveMarkerPriority(state);
            ReceiveMarkerState? previous = receiveMarkerSave;
            receiveMarkerSave = state;
            SetBeamSaveMarker(state);
            ReleaseReceiveMarkerNode(previous);
        }
    }

    private bool TryReceiveMarkerReference(Term id, out BigInteger reference)
    {
        if (id is ReferenceTerm value && value.LocalIdentity is not null
            && ReferenceEquals(value.LocalIdentity, Owner?.LocalIdentity))
        {
            reference = value.FullId;

            return true;
        }
        reference = default;

        return false;
    }

    private ReceiveMarkerState? FindReceiveMarker(Term id)
    {
        if (!TryReceiveMarkerReference(id, out BigInteger reference))
            return null;

        return receiveMarkers.FirstOrDefault(state => state.ReferenceId == reference);
    }

    private void RetireReceiveMarker(ReceiveMarkerState state)
    {
        if (state.Retired)
            return;
        state.Retired = true;
        receiveMarkerTokens.Remove(state.Token);
        if (state.Allocation is { } allocation)
            receiveMarkers.Remove(allocation);
        state.Allocation = null;
        ReleaseReceiveMarkerNode(state);
    }

    private void ReleaseReceiveMarkerNode(ReceiveMarkerState? state)
    {
        if (state is null || !state.Retired || state.CursorUsers != 0 || ReferenceEquals(receiveMarkerSave, state))
            return;
        if (ReferenceEquals(state.Node.List, messages))
        {
            BeforeBeamSaveUnlink(state.Node);
            messages.Remove(state.Node);
            mutationRevision++;
        }
    }

    private void ValidateReceiveMarkerPriority(ReceiveMarkerState? state)
    {
        if (beamSave is not null)
            return;
        if (state is not null && (lastPriority is not null || state.PriorityRevision != receiveMarkerPriorityRevision))
            throw new NotSupportedException(MailboxReceiveMarkerParameters.PriorityUnsupported);
    }

    private void ObserveReceiveMarkerPass(LinkedListNode<Entry> node)
    {
        if (node.Value.ReceiveMarker is not { } token || !receiveMarkerTokens.TryGetValue(token, out var state)
            || state.CursorUsers != 0 || ReferenceEquals(receiveMarkerSave, state))
            return;
        if (node.Previous is null || ++state.Passes > MailboxReceiveMarkerParameters.MaximumPasses)
            RetireReceiveMarker(state);
    }

    private LinkedListNode<Entry>? ReceiveMarkerStart(ReceiveMarkerState? state)
    {
        ValidateReceiveMarkerPriority(state);

        return state is null ? messages.First : state.Node.Next;
    }

    private async ValueTask<T?> ReceiveCore593<T>(
        Func<Term, ValueTask<T?>> select,
        TimeSpan? timeout,
        CancellationToken cancellation,
        Func<TimeSpan?>? resolveTimeout = null,
        bool rescanRemovedCandidate = false,
        MailboxSelectionAdmission593? admission593 = null
    ) where T : class
    {
        ReceiveMarkerState? selected;
        lock (gate)
        {
            selected = receiveMarkerSave;
            receiveMarkerSave = null;
            if (selected is not null)
                selected.CursorUsers++;
        }
        try
        {
            bool sharedSave;
            lock (gate)
            {
                ValidateReceiveMarkerPriority(selected);
                sharedSave = beamSave is not null;
                if (sharedSave && selected is not null && !ReferenceEquals(beamSave!.Marker, selected))
                    SetBeamSaveMarker(selected);
            }
            if (sharedSave)
                return await ReceiveBeamSaveCore593(
                    select,
                    timeout,
                    cancellation,
                    resolveTimeout,
                    rescanRemovedCandidate,
                    admission593
                );

            return await ReceiveMarkedCore593(
                select,
                timeout,
                cancellation,
                resolveTimeout,
                selected,
                admission593
            );
        }
        finally
        {
            lock (gate)
            {
                if (selected is not null)
                {
                    selected.CursorUsers--;
                    ReleaseReceiveMarkerNode(selected);
                }
            }
        }
    }

    private void ClearReceiveMarkersForHalt()
    {
        foreach (ReceiveMarkerState state in receiveMarkers)
        {
            state.Retired = true;
            state.Allocation = null;
        }
        receiveMarkers.Clear();
        receiveMarkerTokens.Clear();
        receiveMarkerSave = null;
    }
}
