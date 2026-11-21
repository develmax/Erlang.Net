using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearOverwrittenOutgoingRoots(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var sites = new List<int>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start >= end || !HasOneUnprotectedRootSlot(body, start, end))
            {
                continue;
            }

            bool defined = false;
            for (int index = start; index + 1 < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode is Label or InitYRegisters)
                {
                    defined = false;
                }
                else if (DefinesResultRoot(instruction))
                {
                    defined = true;
                }
                else if (defined && instruction is
                {
                    Definition.Opcode: Move,
                    Operands:
                        [
                        { Kind: BeamOperandParameters.Y, Value.IsZero: true },
                        { Kind: BeamOperandParameters.X, Value.IsZero: true }
                        ]
                })
                {
                    int call = index + 1;
                    while (call < end && body[call].Definition.Opcode == Line)
                    {
                        call++;
                    }

                    if (call + 1 < end && body[call] is
                        {
                            Definition.Opcode: LocalCall or ExternalCall,
                            Operands: [var arity, _]
                        } && arity.Value > 0 && ReplacesOutgoingRoot(
                            body,
                            start,
                            end,
                            call + 1
                        ))
                    {
                        sites.Add(body[index + 1].Offset);
                        defined = false;
                    }
                }
            }
        }

        if (sites.Count == 0)
        {
            return null;
        }

        // beam_ssa_codegen:def_is clears the dead old owner even when its slot is
        // needed again for the normal result. The argument itself is already in X0.
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (int site in sites)
        {
            output.Write(code.Span[copied..site]);
            WriteFrameInitialization(output, [0]);
            copied = site;
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, InitYRegisters);

        return output.ToArray();
    }

    private static bool HasOneUnprotectedRootSlot(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        bool allocated = false;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is CatchEnter or TryEnter or TryEnd or TryCase or CatchEnd
                or LoopReceive or Wait or WaitTimeout or TrimFrame or DebugLine or ExecutableLine)
            {
                return false;
            }

            if (instruction.Definition.Opcode is Allocate or AllocateHeap)
            {
                if (allocated || instruction.Operands[0].Value != 1)
                {
                    return false;
                }

                allocated = true;
            }
        }

        return allocated;
    }

    private static bool DefinesResultRoot(BeamInstruction instruction) =>
        instruction.Definition.Opcode is Move or GetTupleElement
        && instruction.Operands[0].Kind == BeamOperandParameters.X
        && instruction.Operands[^1] is { Kind: BeamOperandParameters.Y, Value.IsZero: true };

    private static bool ReplacesOutgoingRoot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int next
    )
    {
        if (DefinesResultRoot(body[next]))
        {
            return true;
        }

        if (next + 2 >= end || body[next] is not
            {
                Definition.Opcode: IsTuple,
                Operands: [var failure, { Kind: BeamOperandParameters.X, Value.IsZero: true }]
            }
            || body[next + 1] is not
            {
                Definition.Opcode: TestArity,
                Operands: [var arityFailure, { Kind: BeamOperandParameters.X, Value.IsZero: true }, _]
            } || failure.Value != arityFailure.Value || !DefinesResultRoot(body[next + 2]))
        {
            return false;
        }

        for (int label = start; label + 1 < end; label++)
        {
            if (body[label] is not { Definition.Opcode: Label, Operands: [var id] } || id.Value != failure.Value)
            {
                continue;
            }

            int exit = label + 1;
            while (exit < end && body[exit].Definition.Opcode == Line)
            {
                exit++;
            }

            return exit < end && body[exit] is
            {
                Definition.Opcode: BadMatch or CaseEnd,
                Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }]
            };
        }

        return false;
    }
}
