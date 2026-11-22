using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseSharedArityPayloadOnRetry(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        var removals = new SortedSet<int>();
        for (int select = 0; select + 4 < body.Count; select++)
        {
            if (body[select] is not { Definition.Opcode: SelectTupleArity, Operands: [var carrier, _, _, ..] } arity
                || carrier.Kind != BeamOperandParameters.X)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            for (int option = 3; option + 1 < arity.Operands.Count; option += 2)
            {
                var width = arity.Operands[option].Value;
                if (width < 2 || !flow.TryGetSingleForwardTarget(select, (int)arity.Operands[option + 1].Value, out int entry)
                    || entry + 3 >= body.Count || body[entry + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, { Value.IsZero: true }, { Kind: BeamOperandParameters.X, TypeIndex: null } tag]
                    } || !SameProjectionRegister(carrier, source) || tag == source
                    || body[entry + 2] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var sameSource, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } payload]
                    } shared || sameSource != source || field.Value <= 0 || field.Value >= width || payload == source
                    || payload == tag || body[entry + 3] is not
                    { Definition.Opcode: SelectValue, Operands: [var tested, _, var count, ..] } dispatch
                    || tested != tag || count.Value < 6 || dispatch.Operands.Skip(3).Where((_, index) => index % 2 == 0)
                        .Any(key => key.Kind != BeamOperandParameters.Atom)
                    || !HasUnprotectedCheckedFailureRegion(body, select, entry + 3))
                {
                    continue;
                }

                for (int retry = entry + 4; retry + 2 < body.Count; retry++)
                {
                    if (body[retry].Definition.Opcode is FunctionInfo or EndCode)
                    {
                        break;
                    }

                    if (body[retry].Definition.Opcode == Label
                        && body[retry + 1].Definition.Opcode == GetTupleElement
                        && body[retry + 1].Operands.SequenceEqual(shared.Operands)
                        && body[retry + 2].Definition.Opcode == IsNonemptyList
                        && flow.HasStableProjectionOnEveryRetryEdge(
                            entry,
                            retry,
                            shared,
                            allowStableSelector: true
                        ))
                    {
                        removals.Add(retry + 1);
                    }
                }
            }
        }

        var edits = removals.Select(index => new CodeEdit(
            body[index].Offset,
            body[index + 1].Offset,
            0,
            0,
            IsRemoval: true
        )).ToList();

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }
}
