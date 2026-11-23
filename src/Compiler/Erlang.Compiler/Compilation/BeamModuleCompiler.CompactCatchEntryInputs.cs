using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactClosedCatchEntryInputs(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || !code.Span.Contains(CatchEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || arity.Value.Sign <= 0 || arity.Value >= BeamOperandParameters.MaximumRegister
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value != arity.Value + 1 || arity != live
                || body[info + 3] is not { Definition.Opcode: InitYRegisters } initializes)
            {
                continue;
            }

            int firstSave = info + 4;
            int count = (int)arity.Value;
            int enter = firstSave + count;
            if (enter + 5 >= body.Count || body[enter] is not
                {
                    Definition.Opcode: CatchEnter,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } token,
                    { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } handler]
                }
                || token.Value < 0 || token.Value >= width.Value)
            {
                continue;
            }

            var saves = new Dictionary<BeamOperand, int>();
            var inputs = new HashSet<BeamOperand>();
            for (int index = firstSave; index < enter; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                        ]
                    } || input.Value < 0 || input.Value >= arity.Value || saved.Value < 0 || saved.Value >= width.Value
                    || saved == token || !inputs.Add(input) || !saves.TryAdd(saved, index))
                {
                    break;
                }
            }

            int firstExtra = enter + 1;
            int reload = enter + 2;
            int call = enter + 4;
            if (saves.Count != count || initializes.Operands.Skip(1).Any(slot => !saves.ContainsKey(slot))
                || body[firstExtra] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                            or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                    },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } extra
                    ]
                } || extra.Value != arity.Value
                || body[reload] is not { Definition.Opcode: Move, Operands: [var savedFirst, var reloaded] }
                || !IsBareX0(reloaded) || !saves.TryGetValue(savedFirst, out int first)
                || !IsBareX0(body[first].Operands[0])
                || body[enter + 3].Definition.Opcode != Line
                || body[call] is not { Definition.Opcode: ExternalCall, Operands: [var arguments, _] }
                || arguments.Value != width.Value
                || body[call + 1] is not { Definition.Opcode: Label, Operands: [var landing] }
                || landing.Value != handler.Value
                || call + 2 >= body.Count || body[call + 2] is not { Definition.Opcode: CatchEnd, Operands: [var exit] }
                || exit != token)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetExclusiveForwardTarget(
                enter,
                call,
                (int)handler.Value,
                out int target
            ) || target != call + 1)
            {
                continue;
            }

            // Every saved slot is defined by direct copies before the handler.
            // Reorder only independent copies, then retain ABI X0 for the call.
            output.Write(code.Span[copied..initializes.Offset]);
            foreach (int save in saves.OrderBy(pair => pair.Key.Value).Select(pair => pair.Value))
            {
                output.Write(code.Span[body[save].Offset..body[save + 1].Offset]);
            }

            output.Write(code.Span[body[enter].Offset..body[reload].Offset]);
            copied = body[reload + 1].Offset;
            info = call;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
