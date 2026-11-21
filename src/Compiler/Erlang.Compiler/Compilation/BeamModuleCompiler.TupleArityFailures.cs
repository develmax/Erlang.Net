using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void RedirectIsolatedTupleArityFailures(
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> references,
        IReadOnlyList<int> labels,
        List<CodeEdit> edits
    )
    {
        var definitions = new Dictionary<int, int>();
        for (int index = 0; index < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: Label, Operands: [var entry] })
            {
                definitions.Add((int)entry.Value, index);
            }
        }

        var incoming = new int[labels.Count];
        var referencesAtOffset = new Dictionary<int, (int End, int Label)>();
        foreach (var (start, end, label) in references)
        {
            incoming[label]++;
            referencesAtOffset.Add(start, (end, label));
        }

        var redirected = new HashSet<int>();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction arityTest = body[index + 1];
            if (body[index] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } nonTuple, var source]
                }
                || source is not { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null }
                || arityTest is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var wrongArity, var tested, _]
                }
                || wrongArity != nonTuple || source != tested
                || !referencesAtOffset.TryGetValue(arityTest.Offset + 1, out var branch)
                || incoming[branch.Label] != 2
                || !definitions.TryGetValue(branch.Label, out int relay)
                || relay <= index + 1 || relay + 1 >= body.Count
                || !EndsBeamBlock(body[relay - 1].Definition.Opcode)
                || body[relay + 1] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } failure,
                        var compared,
                    { Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil }
                    ]
                }
                || compared != source
                || !definitions.TryGetValue((int)failure.Value, out int continuation)
                || continuation <= relay)
            {
                continue;
            }

            // Only the adjacent arity test proves that the value is a tuple.
            // Keep the comparison on the non-tuple edge and preserve shared fallbacks.
            int start = arityTest.Offset + 1;
            edits.Add(new CodeEdit(
                start,
                branch.End,
                LabelTag,
                labels[(int)failure.Value]
            ));
            redirected.Add(start);
        }

        for (int entry = 1; entry + 5 < body.Count; entry++)
        {
            if (ReadConsShapeRejection(body, entry, definitions) is not { } rejection)
            {
                continue;
            }

            int type = body[entry + 3].Offset + 1;
            int arity = body[entry + 4].Offset + 1;
            if (!referencesAtOffset.TryGetValue(type, out var typeEdge)
                || !referencesAtOffset.TryGetValue(arity, out var arityEdge)
                || typeEdge.Label != rejection.NilEntry || arityEdge.Label != rejection.NilEntry)
            {
                continue;
            }

            // This path has already extracted the cons cells without changing
            // any argument. The empty-list test can only reject that input.
            int failure = labels[rejection.FunctionFailure];
            edits.Add(new CodeEdit(
                type,
                typeEdge.End,
                LabelTag,
                failure
            ));
            edits.Add(new CodeEdit(
                arity,
                arityEdge.End,
                LabelTag,
                failure
            ));
            redirected.Add(type);
            redirected.Add(arity);
        }

        if (redirected.Count != 0)
        {
            references.RemoveAll(reference => redirected.Contains(reference.Start));
        }
    }

    private static (int NilEntry, int FunctionFailure)? ReadConsShapeRejection(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        IReadOnlyDictionary<int, int> definitions
    )
    {
        if (body[entry - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arguments] }
            || body[entry].Definition.Opcode != Label
            || body[entry + 1] is not
            {
                Definition.Opcode: IsNonemptyList,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } nilEntry,
                { Kind: BeamOperandParameters.X, TypeIndex: null } list
                ]
            } || list.Value >= arguments.Value
            || body[entry + 2] is not
            {
                Definition.Opcode: GetList,
                Operands: [
                var subject, { Kind: BeamOperandParameters.X, TypeIndex: null } head,
                { Kind: BeamOperandParameters.X, TypeIndex: null } tail
                ]
            } || subject != list || head == tail
            || head.Value < arguments.Value || tail.Value < arguments.Value
            || body[entry + 3] is not
            {
                Definition.Opcode: IsTuple,
                Operands: [var nonTuple, var tuple]
            } || nonTuple != nilEntry || tuple != head
            || body[entry + 4] is not
            {
                Definition.Opcode: TestArity,
                Operands: [var arityRejected, var checkedTuple, _]
            } || arityRejected != nilEntry || checkedTuple != head
            || !definitions.TryGetValue((int)nilEntry.Value, out int relay) || relay <= entry + 4
            || relay + 1 >= body.Count || body[relay + 1] is not
            {
                Definition.Opcode: IsNil,
                Operands: [{ Kind: BeamOperandParameters.Label } rejected, var empty]
            } || empty != list)
        {
            return null;
        }

        int failure = EntryFrameFailureLabel(body, entry);
        if (failure <= 0 || rejected.Value != failure || !HasUnprotectedCheckedFailureRegion(body, entry, relay))
        {
            return null;
        }

        return ((int)nilEntry.Value, failure);
    }
}
