using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardTerminalJoinArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Jump)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var labelReferences = body.Where(instruction => instruction.Definition.Opcode != Label)
            .SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind is BeamOperandParameters.Failure or BeamOperandParameters.Label)
            .GroupBy(operand => operand.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int join = 4; join + 2 < body.Count; join++)
        {
            if (body[join] is not { Definition.Opcode: Label, Operands: [var label] }
                || labelReferences.GetValueOrDefault(label.Value) != 1
                || body[join - 1] is not { Definition.Opcode: Move, Operands: [var alternate, var alternateDestination] }
                || alternate is not { Kind: BeamOperandParameters.Y, TypeIndex: null } || alternateDestination != x0
                || body[join - 2].Definition.Opcode != Label
                || body[join - 3] is not { Definition.Opcode: Jump, Operands: [var target] }
                || target.Value != label.Value
                || body[join - 4].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[join + 1] is not { Definition.Opcode: Move, Operands: [var input, var scratch] }
                || input != x0 || scratch is not { Kind: BeamOperandParameters.Y, TypeIndex: null })
            {
                continue;
            }

            int start = join + 2;
            int tail = start;
            int transfer = -1;
            var arguments = new HashSet<int>();
            while (tail < body.Count && body[tail] is
                { Definition.Opcode: Move, Operands: [var source, var destination] }
                && source is { Kind: BeamOperandParameters.Y, TypeIndex: null }
                && destination is { Kind: BeamOperandParameters.X, TypeIndex: null }
                && arguments.Add((int)destination.Value))
            {
                if (source == scratch)
                {
                    if (transfer >= 0 || destination == x0)
                    {
                        break;
                    }

                    transfer = tail;
                }

                tail++;
            }

            int transferEnd = tail;
            while (tail < body.Count && body[tail].Definition.Opcode == Line)
            {
                tail++;
            }

            if (transfer < 0 || tail >= body.Count || body[tail] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                    Operands: [var arity, _, _]
                } || arity.Value != arguments.Count || !arguments.Contains(0)
                || arguments.Any(register => register >= arity.Value))
            {
                continue;
            }

            // The join has one jump and one fallthrough. Both define the
            // same outgoing argument before any other argument can read it.
            // The tail consumes the frame before another instruction can read
            // the removed stack copy or use it as a GC root.
            BeamOperand argument = body[transfer].Operands[1];
            output.Write(code.Span[copied..body[join - 3].Offset]);
            WriteMove(x0, argument);
            output.Write(code.Span[body[join - 3].Offset..body[join - 1].Offset]);
            WriteMove(alternate, argument);
            output.Write(code.Span[body[join].Offset..body[join + 1].Offset]);
            output.Write(code.Span[body[start].Offset..body[transfer].Offset]);
            output.Write(code.Span[body[transfer + 1].Offset..body[transferEnd].Offset]);
            copied = body[transferEnd].Offset;
            join = tail;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteMove(BeamOperand source, BeamOperand target)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, target);
        }
    }
}
