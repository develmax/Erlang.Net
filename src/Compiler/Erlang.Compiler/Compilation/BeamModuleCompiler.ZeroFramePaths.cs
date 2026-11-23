using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ZeroFramePathPlan(
        Dictionary<int, int> Calls,
        HashSet<int> RetiredReleases,
        HashSet<int> FramelessTails
    );

    private byte[]? PlaceZeroFramesOnCallingPaths(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Allocate)
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
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.IsZero: true }, var oldLive]
                } || oldLive.Value != arity.Value)
            {
                continue;
            }

            int allocation = info + 2;
            int end = allocation + 1;
            while (end < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            // The next function's location and header label do not belong to this body.
            while (end > allocation + 1 && body[end - 1].Definition.Opcode == Line)
            {
                end--;
            }

            if (body[end].Definition.Opcode != EndCode && end > allocation + 1
                && body[end - 1].Definition.Opcode == Label)
            {
                end--;
            }

            flow ??= new BeamProjectionFlow(body);
            if (flow.ReadZeroFramePaths(allocation + 1, end) is not { } plan)
            {
                continue;
            }

            output.Write(code.Span[copied..body[allocation].Offset]);
            copied = body[allocation + 1].Offset;
            for (int index = allocation + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (plan.Calls.TryGetValue(index, out int live))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Allocate);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
                    copied = instruction.Offset;
                }

                if (plan.RetiredReleases.Contains(index))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = body[index + 1].Offset;
                }
                else if (plan.FramelessTails.Contains(index))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(instruction.Definition.Opcode == TailLocalCallWithFrame ? TailLocalCall : TailExternalCall);
                    int endOfCall = body[index + 1].Offset;
                    output.Write(code.Span[(instruction.Offset + 1)..(endOfCall - CompactRegisterCountWidth(0))]);
                    copied = endOfCall;
                }
            }
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
        internal ZeroFramePathPlan? ReadZeroFramePaths(int start, int end)
        {
            var calls = new Dictionary<int, int>();
            var releases = new HashSet<int>();
            var tails = new HashSet<int>();
            var pending = new Queue<(int Index, bool Framed)>();
            var states = new Dictionary<int, bool>();
            pending.Enqueue((start, false));
            while (pending.TryDequeue(out var item))
            {
                var (index, framed) = item;
                if (index < start || index >= end)
                {
                    return null;
                }

                if (states.TryGetValue(index, out bool before))
                {
                    if (before != framed)
                    {
                        // A shared continuation cannot have two physical frame states.
                        return null;
                    }

                    continue;
                }

                states.Add(index, framed);
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)
                    || opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter or LoopReceive
                        or Wait or WaitTimeout or ReceiveTimeout or DynamicApply or TailDynamicApply or CallFun or CallFunTyped
                        or NifStart or DebugLine or ExecutableLine)
                {
                    return null;
                }

                switch (opcode)
                {
                    case LocalCall or ExternalCall when !framed:
                        var arguments = instruction.Operands[0].Value;
                        if (arguments.Sign < 0 || arguments > BeamOperandParameters.MaximumRegister)
                        {
                            return null;
                        }

                        // A call owns exactly its outgoing ABI register prefix.
                        int insertion = index > start && body[index - 1].Definition.Opcode == Line ? index - 1 : index;
                        if (!calls.TryAdd(insertion, (int)arguments))
                        {
                            return null;
                        }

                        framed = true;
                        break;
                    case Deallocate:
                        if (!instruction.Operands[0].Value.IsZero)
                        {
                            return null;
                        }

                        if (!framed)
                        {
                            releases.Add(index);
                        }

                        framed = false;
                        break;
                    case TailLocalCallWithFrame or TailExternalCallWithFrame when !framed:
                        if (!instruction.Operands[^1].Value.IsZero)
                        {
                            return null;
                        }

                        tails.Add(index);
                        break;
                    case Return or TailLocalCall or TailExternalCall when framed:
                        return null;
                }

                foreach (int successor in successors[index])
                {
                    if (successor <= index)
                    {
                        return null;
                    }

                    pending.Enqueue((successor, framed));
                }
            }

            // Unreachable frame releases must not survive after removing the old allocation.
            if (states.Count != end - start || calls.Count == 0 || releases.Count + tails.Count == 0)
            {
                return null;
            }

            return new(calls, releases, tails);
        }
    }
}
