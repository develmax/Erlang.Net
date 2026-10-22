namespace Erlang;

internal static class DebugDirtyTermCopy424
{
    internal static Term Copy(ProcessRuntime runtime, Term source)
    {
        // The native copy_struct traversal copies each heap occurrence; do not
        // turn repeated boxed children into a shared memoized output graph.
        // Explicit frames avoid CLR recursion for deep proper/improper lists.
        var stack = new Stack<DebugDirtyCopyFrame424>();
        stack.Push(new(source));
        while (true)
        {
            DebugDirtyCopyFrame424 frame = stack.Peek();
            if (frame.Index < frame.Children.Length)
            {
                stack.Push(new(frame.Children[frame.Index]));
                continue;
            }
            Term copy = Build(runtime, frame.Source, frame.Copied);
            stack.Pop();
            if (stack.Count == 0)
            {
                runtime.ObserveIncomingTerm(copy);

                return copy;
            }
            DebugDirtyCopyFrame424 parent = stack.Peek();
            parent.Copied[parent.Index++] = copy;
        }
    }

    private static Term Build(ProcessRuntime runtime, Term source, Term[] values)
    {
        switch (source)
        {
            case Nil:
                return Nil.Value;
            case Atom atom:
                return Term.A(atom.Name);
            case Integer integer:
                return new Integer(integer.Value);
            case FloatTerm number:
                return new FloatTerm(number.Value);
            case Cons:
                return new Cons(values[0], values[1]);
            case TupleTerm { Items.Count: 0 }:
                // OTP's empty tuple is always its global literal.
                return source;
            case TupleTerm:
                return new TupleTerm(values);
            case HamtBitmapTerm429 node:
                return new HamtBitmapTerm429(
                    node.Domain,
                    node.Ownership,
                    node.Bitmap,
                    values
                );
            case MapTerm:
                return new MapTerm(Enumerable.Range(0, values.Length / 2)
                    .Select(index => new KeyValuePair<Term, Term>(values[index * 2], values[index * 2 + 1])));
            case NativeRecordTerm record:
                return new NativeRecordTerm(
                    record.Module,
                    record.Name,
                    record.IsExported,
                    record.FieldNames.Select((name, index) => new KeyValuePair<Atom, Term>(name, values[index]))
                );
            case BitString binary:
                PersistentBinarySlice416 slice = binary.CapturePersistentSlice416();

                return slice.Backing is null ? new BitString(binary.ToArray(), binary.BitLength)
                    : slice.Backing.Materialize(slice.BitOffset, slice.BitLength);
            case ResourceReferenceTerm resource:
                // New heap wrapper, same actual authenticated magic resource.
                // Its strong Entry reference owns payload lifetime naturally.
                return new ResourceReferenceTerm(resource.Entry);
            case Pid pid:
                return pid.LocalIdentity is { } pidOwner ? new Pid(pidOwner, pid.Id) : new Pid(pid.Node, pid.Id, pid.Creation);
            case PortTerm port:
                return port.LocalIdentity is { } portOwner ? new PortTerm(portOwner, port.Id) : new PortTerm(port.Node, port.Id, port.Creation);
            case ReferenceTerm reference:
                return new ReferenceTerm(reference);
            case FunctionTerm function:
                IRuntimeStoredFunctionRecipe recipe = RuntimeStoredFunctionRecipes.Capture(runtime, function);
                if (recipe is ExternalStoredFunctionRecipe external)
                    recipe = new DebugDirtyExternalFunctionRecipe424(external.Owner, external.Identity);

                return recipe.Rebind(values);
            default:
                throw new NotSupportedException(PersistentArenaNames416.Unsupported);
        }
    }
}
