using static Erlang.Tests.KnownHostGraph384Names;

namespace Erlang.Tests;

internal static class KnownHostGraph384Checks
{
    internal static void RequireDetached(Term graph)
    {
        var assembly = typeof(ProcessRuntime).Assembly;
        var storage = assembly.GetType(HeapType, true)!.GetMethod(Storage, Members)!;
        var children = assembly.GetType(ChildrenType, true)!.GetMethod(Children, Members)!;
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(graph);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value)) continue;
            Require(storage.Invoke(null, [value]) is null);
            Require(value is Atom or Nil or Integer or FloatTerm or Cons or TupleTerm or MapTerm
                or NativeRecordTerm or BitString or ReferenceTerm or Pid or PortTerm or FunctionTerm || value.GetType() == assembly.GetType(HamtType, true));
            // The runtime's actual topology walker includes managed private map
            // indexes, physical/arithmetic rows, key tuples and stored HAMT nodes.
            // Unlike deferred control metadata, a public DTO may retain that
            // detached CLR topology; every actual reachable Term must lack cells.
            foreach (Term child in (Term[])children.Invoke(null, [value])!) pending.Push(child);
            if (value is FunctionTerm function
                && function.GetType().GetProperty(StoredIdentity, Members)!.GetValue(function) is LocalFunctionIdentity identity)
                pending.Push(identity.Unique);
            if (value is BitString binary
                && binary.GetType().GetProperty(Origin, Members)!.GetValue(binary) is BitString origin)
                pending.Push(origin);
        }
    }

    internal static FunctionTerm RequireLogicalFunction(ProcessContext context, Term candidate, FunctionTerm original)
    {
        RequireDetached(candidate);
        RequireDetached(original);
        Require(candidate is FunctionTerm);
        var received = (FunctionTerm)candidate;
        Require(received.Arity == original.Arity);
        Require(Equals(received.LocalIdentity, original.LocalIdentity));
        Require(Equals(received.ExternalIdentity, original.ExternalIdentity));
        Require(received.Environment.Count == original.Environment.Count);
        for (int index = 0; index < original.Environment.Count; index++)
            Require(received.Environment[index].Equals(original.Environment[index]));
        var options = new EtfEncodingOptions { FunctionOwner = context.Runtime.InitializationPid };
        Require(ExternalTermFormat.Encode(received, options).AsSpan().SequenceEqual(ExternalTermFormat.Encode(original, options)));

        return received;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
