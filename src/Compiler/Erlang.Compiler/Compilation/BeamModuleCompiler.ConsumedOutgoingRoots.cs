using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (byte[] Code, IReadOnlySet<int> Initializations)? ClearConsumedOutgoingRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var edits = new SortedDictionary<int, int[]>();
        BeamProjectionFlow? flow = null;
        var cleared = new HashSet<(int Index, int Slot)>();
        bool protectedFunction = true;
        for (int call = 0; call < body.Count; call++)
        {
            if (body[call].Definition.Opcode == FunctionInfo)
            {
                protectedFunction = false;
                for (int end = call + 1; body[end].Definition.Opcode is not (FunctionInfo or EndCode); end++)
                {
                    protectedFunction |= body[end].Definition.Opcode is CatchEnter or TryEnter or DebugLine or ExecutableLine;
                }
            }

            if (protectedFunction || body[call] is not
                {
                    Definition.Opcode: LocalCall or ExternalCall,
                    Operands: [var arity, _]
                } || arity.Value.IsZero)
            {
                continue;
            }

            int afterSetup = call;
            while (afterSetup > 0 && body[afterSetup - 1].Definition.Opcode == Line)
            {
                afterSetup--;
            }

            // Calls already preceded by a kill retain their existing schedule.
            var sources = new SortedSet<int>();
            var destinations = new HashSet<int>();
            int setup = afterSetup - 1;
            for (; setup > 0 && body[setup] is
            {
                Definition.Opcode: Move,
                Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            } && destination.Value < arity.Value && destinations.Add((int)destination.Value); setup--)
            {
                if (source is not { Kind: BeamOperandParameters.Y, TypeIndex: null })
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (flow.IsInitialized(setup, source) && flow.IsDead(call, source, allowBadRecordExit: true))
                {
                    sources.Add((int)source.Value);
                }
            }

            if (setup > 0 && body[setup] is
                {
                    Definition.Opcode: PutList,
                    Operands: [var head, var tail, { Kind: BeamOperandParameters.X, TypeIndex: null } list]
                }
                && list.Value < arity.Value && !destinations.Contains((int)list.Value))
            {
                flow ??= new BeamProjectionFlow(body);
                // The outgoing cons holds these values after their stack slots die.
                foreach (BeamOperand input in new[] { head, tail })
                {
                    if (input is { Kind: BeamOperandParameters.Y, TypeIndex: null }
                        && flow.HasLiveSharedFrameRoot(setup, input, cleared)
                        && flow.IsInitialized(setup, input, allowFramePredicates: true)
                        && flow.IsDead(call, input, allowBadRecordExit: true))
                    {
                        sources.Add((int)input.Value);
                    }
                }
            }

            if (sources.Count != 0)
            {
                edits.Add(afterSetup, sources.ToArray());
                foreach (int slot in sources)
                {
                    cleared.Add((call, slot));
                }
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        var initializations = new HashSet<int>();
        int copied = 0;
        foreach (var (index, slots) in edits)
        {
            int insertion = body[index].Offset;
            output.Write(code.Span[copied..insertion]);
            initializations.Add(checked((int)output.Position));
            WriteFrameInitialization(output, slots);
            copied = insertion;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        return (output.ToArray(), initializations);
    }
}
