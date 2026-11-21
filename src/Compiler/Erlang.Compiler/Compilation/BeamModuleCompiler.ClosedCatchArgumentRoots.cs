using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearClosedCatchArgumentRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var edits = new SortedDictionary<int, int[]>();
        BeamProjectionFlow? flow = null;
        for (int entry = 0; entry + 6 < body.Count; entry++)
        {
            if (body[entry] is not { Definition.Opcode: CatchEnter, Operands: [var handler, var fault] })
            {
                continue;
            }

            int landing = entry + 1;
            while (IsClosedCatchArgumentOperation(body[landing].Definition.Opcode))
            {
                landing++;
            }

            if (landing + 3 >= body.Count
                || body[landing] is not { Definition.Opcode: Label, Operands: [var target] }
                || target.Value != fault.Value
                || body[landing + 1] is not { Definition.Opcode: CatchEnd, Operands: [var released] }
                || released != handler
                || body[landing + 2].Definition.Opcode != Deallocate
                || body[landing + 3].Definition.Opcode != Return)
            {
                continue;
            }

            int release = landing + 1;
            for (int call = entry + 2; call < landing; call++)
            {
                if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] })
                {
                    continue;
                }

                int setup = call;
                while (setup > entry + 1 && body[setup - 1].Definition.Opcode == Line)
                {
                    setup--;
                }

                var roots = new SortedSet<int>();
                for (int argument = setup - 1; argument > entry; argument--)
                {
                    if (body[argument] is not { Definition.Opcode: Move, Operands: [var source, var destination] }
                        || destination.Kind != BeamOperandParameters.X || destination.Value >= arity.Value)
                    {
                        break;
                    }

                    if (source.Kind != BeamOperandParameters.Y || source.TypeIndex is not null
                        || SameProjectionRegister(handler, source)
                        || !DeadInClosedCatchContinuation(argument, release, source))
                    {
                        continue;
                    }

                    var ownership = flow ??= new BeamProjectionFlow(body);
                    if (ownership.IsInitialized(
                        argument,
                        source,
                        closedCatchEnter: entry,
                        closedCatchEnd: release
                    ))
                    {
                        roots.Add((int)source.Value);
                    }
                }

                if (roots.Count != 0)
                {
                    edits.Add(body[setup].Offset, roots.ToArray());
                }
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        // The setup moves already make these terms visible in the outgoing X
        // arguments. Both call continuations reach the same closed catch return.
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var (site, roots) in edits)
        {
            output.Write(code.Span[copied..site]);
            WriteFrameInitialization(output, roots);
            copied = site;
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, InitYRegisters);

        return output.ToArray();

        bool DeadInClosedCatchContinuation(int argument, int release, BeamOperand source)
        {
            for (int index = argument + 1; index < release; index++)
            {
                if (body[index].Operands.Any(operand => SameProjectionRegister(operand, source)))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static bool IsClosedCatchArgumentOperation(byte opcode) => opcode is
        Move or LocalCall or ExternalCall or Line or TestHeap or PutList or PutTuple or InitYRegisters;
}
