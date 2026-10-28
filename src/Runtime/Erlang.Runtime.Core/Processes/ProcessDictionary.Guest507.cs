namespace Erlang;

public sealed partial class ProcessDictionary
{
    private readonly Stack<(int Key, int Value)> reusableDictionarySlots507 = new();

    // Only callback-free primitive/tuple/list keys enter the direct dictionary
    // hash path. Other key classes retain the existing detached facade path.
    internal static bool IsDirectKey507(Term key)
    {
        var pending = new Stack<Term>();
        pending.Push(key);
        while (pending.TryPop(out Term? value))
        {
            switch (value)
            {
                case Atom or Integer or FloatTerm or Nil:
                    break;
                case Cons cons:
                    pending.Push(cons.Head);
                    pending.Push(cons.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (Term child in tuple.Items) pending.Push(child);
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    internal bool TryReadGuest507(
        OwnedTermInvocation506 invocation,
        Term key,
        bool remove,
        out Term result
    )
    {
        result = Nil.Value;
        if (!Admitted507(invocation) || !IsDirectKey507(key))
            return false;
        Term undefined = ProcessDictionary507Atoms.Undefined;
        lock (gate)
        {
            CheckOpen();
            Term value = data.TryGetValue(key, out Term? found) ? found : undefined;
            // Publish before removing/replacing any permanent dictionary root.
            result = invocation.HoldGuestResult507(value);
            if (remove)
                RemoveUnderGate(key);

            return true;
        }
    }

    internal bool TryPutGuest507(
        OwnedTermInvocation506 invocation,
        Term key,
        Term value,
        out Term previous
    )
    {
        previous = Nil.Value;
        if (!Admitted507(invocation) || !IsDirectKey507(key))
            return false;
        Term undefined = ProcessDictionary507Atoms.Undefined;
        lock (gate)
        {
            CheckOpen();
            ProcessTermFrame506 ownedFrame = frame!;
            previous = invocation.HoldGuestResult507(data.TryGetValue(key, out Term? found) ? found : undefined);
            if (slots.TryGetValue(key, out var existing))
            {
                ownedFrame.SetLocalGraph507(existing.Value, value);
                data[key] = value;

                return true;
            }
            var reserved = reusableDictionarySlots507.TryPop(out var reused)
                ? reused : (Key: ownedFrame.Reserve(), Value: ownedFrame.Reserve());
            int keySlot = reserved.Key, valueSlot = reserved.Value;
            try
            {
                ownedFrame.SetLocalGraph507(keySlot, key);
                ownedFrame.SetLocalGraph507(valueSlot, value);
                data.Add(key, value);
                try
                {
                    slots.Add(key, (keySlot, valueSlot));
                }
                catch
                {
                    data.Remove(key);
                    throw;
                }
            }
            catch
            {
                ownedFrame.Clear(keySlot);
                ownedFrame.Clear(valueSlot);
                throw;
            }

            return true;
        }
    }

    internal bool TrySnapshotGuest507(OwnedTermInvocation506 invocation, bool erase, out Term result)
    {
        result = Nil.Value;
        if (!Admitted507(invocation))
            return false;
        int pairSlot = invocation.Frame.Reserve();
        lock (gate)
        {
            CheckOpen();
            invocation.HoldGuestResult507(Nil.Value);
            foreach (var entry in data.Reverse())
            {
                Term pair = invocation.Frame.CreateTupleInto507(pairSlot, new Term[] { entry.Key, entry.Value });
                invocation.Frame.CreateConsInto507(invocation.ResultSlot507, pair, invocation.Frame.Read(invocation.ResultSlot507));
            }
            result = invocation.Frame.Read(invocation.ResultSlot507);
            if (erase)
            {
                data.Clear();
                slots.Clear();
                reusableDictionarySlots507.Clear();
                frame!.ResetDictionaryRoots507();
            }

            return true;
        }
    }

    private bool Admitted507(OwnedTermInvocation506 invocation)
        => frame is not null && invocation is { IsDisposed: false, CanBorrow507: true, BindingKind507: OwnedBindingKind507.LocalDictionary }
            && invocation.OwnsContext507(context) && RuntimeActorExecutionTurn.IsOwnedBy(context);
}
