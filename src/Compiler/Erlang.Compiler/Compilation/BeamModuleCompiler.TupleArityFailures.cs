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

        if (redirected.Count != 0)
        {
            references.RemoveAll(reference => redirected.Contains(reference.Start));
        }
    }
}
