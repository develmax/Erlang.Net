using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CoalesceFinalMapScratchProjections(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetMapElements)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return CoalesceCheckedMapScratchProjections(code, body);
    }

    private static byte[]? CoalesceCheckedMapScratchProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: GetMapElements,
                    Operands: [
                    { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure,
                    { Kind: BeamOperandParameters.X } map,
                        _,
                    { Kind: BeamOperandParameters.Atom } firstKey,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } scratch
                    ]
                }
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                        var source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } saved
                    ]
                }
                || source != scratch || SameProjectionRegister(map, scratch)
                || SameProjectionRegister(map, saved) || saved == scratch
                || body[index + 2] is not
                {
                    Definition.Opcode: GetMapElements,
                    Operands: [
                        var secondFailure,
                        var secondMap,
                        _,
                    { Kind: BeamOperandParameters.Atom } secondKey,
                        var destination
                    ]
                }
                || secondFailure != failure || secondMap != map || secondKey == firstKey || destination != scratch)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.IsGuardTemporaryDeadOnFailure(index + 2, scratch, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(index + 2, saved, preserveHeapRoots: true))
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(GetMapElements);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            if (map.TypeIndex is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, map);
            if (map.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }

            // beam_block prepends the later key. Neither destination is visible
            // from the failure edge, so the loader order cannot change that path.
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 4);
            BeamCompactWriter.Write(output, AtomTag, secondKey.Value);
            WriteRegister(output, scratch);
            BeamCompactWriter.Write(output, AtomTag, firstKey.Value);
            WriteRegister(output, saved);
            copied = body[index + 3].Offset;
            index += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
