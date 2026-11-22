using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldPureCallArgumentCycles(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && code.Span.Contains(Move)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? RewritePureCallArgumentCycles(code, decoded.Body)
            : null;

    private static byte[]? RewritePureCallArgumentCycles(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index < body.Count; index++)
        {
            if (ReadProjectedTailArgumentCycle(body, index, ref flow) is { } projected)
            {
                output.Write(code.Span[copied..body[index].Offset]);
                WriteProjection(index + 1, projected.FieldRegister);
                WriteProjection(index + 2, projected.Carrier);
                if (projected.RetainInitialProjection)
                {
                    output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                }

                WriteCycle(projected.Registers);
                index = projected.End - 1;
                copied = body[projected.End].Offset;
            }
            else if (ReadPureCallArgumentCycle(body, index) is { } plan)
            {
                output.Write(code.Span[copied..body[index].Offset]);
                WriteCycle(plan.Registers);
                index = plan.End - 1;
                copied = body[plan.End].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteCycle(List<int> registers)
        {
            int first = registers.IndexOf(registers.Min());
            // beam_ssa_codegen orders the parallel argument copies by destination,
            // then walks the cycle from its lowest destination.
            for (int position = 1; position + 1 < registers.Count; position++)
            {
                WriteSwap(registers[(first + position + 1) % registers.Count],
                    registers[(first + position) % registers.Count]);
            }

            WriteSwap(registers[first], registers[(first + registers.Count - 1) % registers.Count]);
        }

        void WriteProjection(int index, BeamOperand destination)
        {
            IReadOnlyList<BeamOperand> operands = body[index].Operands;
            output.WriteByte(GetTupleElement);
            WriteRegister(output, operands[0]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[1].Value);
            WriteRegister(output, destination);
        }

        void WriteSwap(int source, int destination)
        {
            output.WriteByte(Swap);
            BeamCompactWriter.Write(output, XRegisterTag, source);
            BeamCompactWriter.Write(output, XRegisterTag, destination);
        }
    }

    private static (int End, List<int> Registers, BeamOperand Carrier, BeamOperand FieldRegister, bool RetainInitialProjection)?
        ReadProjectedTailArgumentCycle(IReadOnlyList<BeamInstruction> body, int start, ref BeamProjectionFlow? flow)
    {
        if (start + 4 >= body.Count
            || body[start + 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands:
                [
                { Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                { Kind: BeamOperandParameters.Unsigned } first,
                { Kind: BeamOperandParameters.X, TypeIndex: null } initialDestination
                ]
            }
            || body[start + 2] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands:
                [
                    var source,
                { Kind: BeamOperandParameters.Unsigned } second,
                { Kind: BeamOperandParameters.X, TypeIndex: null } output
                ]
            }
            || source != tuple || first == second || tuple == output || initialDestination == output
            || body[start + 3] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } temporary, var restored]
            }
            || restored != tuple)
        {
            return null;
        }

        int after = start + 4;
        int call = after;
        while (call < body.Count && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (call == body.Count || body[call].Definition.Opcode is not
            (TailLocalCall or TailExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame))
        {
            return null;
        }

        var arity = body[call].Operands[0].Value;
        if (temporary.Value < arity
            || tuple.Value >= arity || output.Value >= arity
            || !(flow ??= new BeamProjectionFlow(body)).HasCheckedTupleField(start, tuple, first)
            || !flow.HasCheckedTupleField(start, tuple, second))
        {
            return null;
        }

        int carrier = (int)tuple.Value;
        int result = (int)output.Value;
        if (body[start] is { Definition.Opcode: Move, Operands: [var saved, var backup] }
            && saved == initialDestination && backup == temporary && saved != tuple && saved.Value < arity)
        {
            return (after, [carrier, (int)saved.Value, result], tuple, output, false);
        }

        if (initialDestination == temporary && body[start] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } parent, var field, var destination]
            }
            && parent == destination && parent != tuple && parent != output && parent.Value < arity
            && HasLocalTupleCheck(
                body,
                start,
                parent,
                field
            ))
        {
            return (after, [carrier, result], tuple, output, true);
        }

        return null;
    }

    private static (int End, List<int> Registers)? ReadPureCallArgumentCycle(
        IReadOnlyList<BeamInstruction> body,
        int start
    )
    {
        if (body[start] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
            } || source == scratch || source.Value >= FirstReservedXRegister)
        {
            return null;
        }

        int destination = (int)source.Value;
        var registers = new List<int> { destination };
        for (int index = start + 1; index < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } output]
                } || output.Value != destination)
            {
                return null;
            }

            if (input == scratch)
            {
                int afterMoves = index + 1;
                int call = afterMoves;
                while (call < body.Count && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                // A direct call uses only its declared X prefix and leaves the scratch dead.
                if (registers.Count > 1 && call < body.Count && body[call].Definition.Opcode
                    is LocalCall or ExternalCall or TailLocalCall or TailExternalCall
                        or TailLocalCallWithFrame or TailExternalCallWithFrame)
                {
                    var arity = body[call].Operands[0].Value;
                    if (scratch.Value >= arity && registers.All(register => register < arity))
                    {
                        return (afterMoves, registers);
                    }
                }

                return null;
            }

            if (input.Value >= FirstReservedXRegister || registers.Contains((int)input.Value))
            {
                return null;
            }

            destination = (int)input.Value;
            registers.Add(destination);
        }

        return null;
    }
}
