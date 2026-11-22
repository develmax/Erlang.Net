using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearProtectedSendArgumentRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Send)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int message = 1; message + 10 < body.Count; message++)
        {
            if (body[message] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } tuple
                || destination != x1
                || body[message + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } recipient, var outgoing]
                } || outgoing != x0)
            {
                continue;
            }

            int send = message + 2;
            while (body[send].Definition.Opcode == Line)
            {
                send++;
            }

            if (send + 9 >= body.Count || body[send].Definition.Opcode != Send
                || body[send + 1] is not { Definition.Opcode: TryEnd, Operands: [var token] }
                || body[send + 2] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || body[send + 3].Definition.Opcode != Return
                || body[send + 4] is not { Definition.Opcode: Label, Operands: [var landing] }
                || body[send + 5] is not { Definition.Opcode: TryCase, Operands: [var caught] } || caught != token
                || body[send + 6] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil },
                        var constantTarget]
                } || constantTarget != x0
                || body[send + 7] is not { Definition.Opcode: Deallocate, Operands: [var exceptionalWidth] }
                || exceptionalWidth != width || body[send + 8].Definition.Opcode != Return)
            {
                continue;
            }

            int installed = message - 1;
            while (installed >= 0 && body[installed].Definition.Opcode is not (TryEnter or FunctionInfo or EndCode))
            {
                installed--;
            }

            if (installed < 0 || body[installed] is not { Definition.Opcode: TryEnter, Operands: [var installedToken, var target] }
                || installedToken != token || target.Value != landing.Value
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    installed,
                    (int)landing.Value,
                    out int catchEntry
                ) || catchEntry != send + 4 || !flow.HasUniqueProtectedSendEntry(installed, send))
            {
                continue;
            }

            int[] roots = tuple.Operands.Skip(2).Append(recipient)
                .Where(operand => operand.Kind == BeamOperandParameters.Y)
                .Where(operand => !SameProjectionRegister(operand, token) && operand.Value < width.Value)
                .Select(operand => (int)operand.Value).Distinct().Order().ToArray();
            if (roots.Length == 0)
            {
                continue;
            }

            // The outgoing registers now retain the recipient and message.
            // Neither the normal release nor the closed landing reads these slots.
            int site = body[message + 2].Offset;
            output.Write(code.Span[copied..site]);
            WriteFrameInitialization(output, roots);
            copied = site;
            message = send;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasUniqueProtectedSendEntry(int installed, int send)
        {
            var work = new Stack<int>();
            var seen = new HashSet<int>();
            work.Push(send);
            while (work.TryPop(out int at))
            {
                if (at == installed || !seen.Add(at))
                {
                    continue;
                }

                if (predecessors[at].Count == 0)
                {
                    return false;
                }

                foreach (int previous in predecessors[at])
                {
                    if (previous < installed || previous >= at || previous != installed
                        && body[previous].Definition.Opcode is TryEnter or TryEnd or TryCase or CatchEnter or CatchEnd
                            or Allocate or AllocateHeap or TrimFrame or Deallocate or DebugLine or ExecutableLine)
                    {
                        return false;
                    }

                    work.Push(previous);
                }
            }

            return true;
        }
    }
}
