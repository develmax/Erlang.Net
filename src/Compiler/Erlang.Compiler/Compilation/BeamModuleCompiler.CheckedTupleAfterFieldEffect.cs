using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseCheckedTupleAfterFieldEffect(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteCheckedTupleAfterFieldEffect(code, body);
    }

    private static byte[]? RewriteCheckedTupleAfterFieldEffect(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            bool? unprotected = null;
            for (int check = start + 1; check + 10 < end; check++)
            {
                if (body[check - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[check] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [
                        { Kind: BeamOperandParameters.Label } failed,
                            var tested,
                        { Value: var arity },
                        { Kind: BeamOperandParameters.Atom } tag
                        ]
                    } || tested != x0 || arity != 2
                    || body[check + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var tuple, { Value.IsOne: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || tuple != x0
                    || body[check + 2] is not { Definition.Opcode: Move, Operands: [var context, var second] }
                    || second != x1 || !IsNonRegisterValue(context)
                    || body[check + 3] is not { Definition.Opcode: Move, Operands: [var payload, var first] }
                    || payload != saved || first != x0)
                {
                    continue;
                }

                int effect = check + 4;
                while (effect < end && body[effect].Definition.Opcode == Line)
                {
                    effect++;
                }

                int heap = effect + 1;
                int construct = effect + 2;
                int fallback = effect + 3;
                if (fallback + 2 >= end || body[effect] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arguments }, _] }
                    || arguments != 2 || body[heap] is not
                    { Definition.Opcode: TestHeap, Operands: [{ Value: var words }, { Value.IsZero: true }] }
                    || words != 3 || body[construct] is not
                    { Definition.Opcode: PutTuple, Operands: [var rebuilt, _, var resultTag, var value] }
                    || rebuilt != x0 || resultTag != tag || value != saved
                    || body[fallback + 1] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                    || saved.Value >= width.Value || body[fallback + 2].Definition.Opcode != Return
                    || !(unprotected ??= HasUnprotectedCallKillSchedule(body, start)))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetExclusiveForwardTarget(
                    check,
                    construct,
                    (int)failed.Value,
                    out int target
                ) || target != fallback
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frame)
                {
                    continue;
                }

                int owner = frame.Owners[check - start];
                if (owner < 0 || frame.Owners[fallback - start] != owner)
                {
                    continue;
                }

                // sys_core_alias returns this exact checked constructor.
                // The effect still receives its original field and argument.
                output.Write(code.Span[copied..body[check].Offset]);
                WriteMove(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    saved.Value
                );
                CopyInstruction(check);
                output.WriteByte(GetTupleElement);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                CopyInstruction(check + 2);
                output.Write(code.Span[body[check + 4].Offset..body[heap].Offset]);
                WriteMove(
                    YRegisterTag,
                    saved.Value,
                    XRegisterTag,
                    0
                );
                output.Write(code.Span[body[fallback + 1].Offset..body[fallback + 3].Offset]);
                copied = body[fallback].Offset;
                check = fallback + 2;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void CopyInstruction(int index) =>
            output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteMove(
            int sourceTag,
            System.Numerics.BigInteger source,
            int targetTag,
            System.Numerics.BigInteger target
        )
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, sourceTag, source);
            BeamCompactWriter.Write(output, targetTag, target);
        }
    }
}
