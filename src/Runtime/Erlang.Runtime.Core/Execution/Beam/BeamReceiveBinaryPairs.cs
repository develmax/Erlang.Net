namespace Erlang;

internal static class BeamReceiveBinaryPairs
{
    internal static BeamInstruction Match(BeamLinkedCode code, int start)
    {
        BeamInstruction first = code.Instructions[start];
        if (first.Definition.Name != BeamBinaryNames.StartLegacy || start + 1 >= code.Instructions.Count)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        BeamInstruction match = code.Instructions[start + 1];
        var operands = first.Operands;
        if (match.Definition.Name != BeamBinaryNames.Match
            || operands[1].Kind != BeamOperandParameters.X || operands[3].Kind != BeamOperandParameters.X
            || operands[1].Value != operands[3].Value
            || match.Operands[1].Kind != BeamOperandParameters.X || match.Operands[1].Value != operands[3].Value
            || operands[0].Value == 0 || operands[0].Value != match.Operands[0].Value)
            throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        IReadOnlyList<BeamBinaryCommand> commands = BeamBinaryCommands.Read(code.Structure, match.Operands);
        for (int index = 0; index < commands.Count; index++)
        {
            BeamBinaryCommand command = commands[index];
            bool last = index == commands.Count - 1;
            if (last && (command.Kind != BeamBinaryCommandKind.Tail || command.Destination is not { } tail
                || tail.Kind != BeamOperandParameters.X || tail.Value != operands[3].Value))
                throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
            if (!last && command.Destination is { } destination
                && (destination.Kind != BeamOperandParameters.X || destination.Value == operands[3].Value))
                throw new NotSupportedException(BeamReceiveNames.UnsupportedMatcher);
        }

        return match;
    }

    internal static bool Execute(BeamLinkedCode code, BeamRegisterState registers, int start)
    {
        BeamInstruction match = Match(code, start);
        var operands = code.Instructions[start].Operands;
        // Ordinary Read deliberately refuses incoming opaque contexts/positions.
        // Never reuse a mutable context retained in the before-receive snapshot.
        if (BeamRegisterInterpreter.Read(code, registers, operands[1]) is not BitString binary)
            return false;
        registers.CheckLive((int)operands[2].Value);
        registers.WriteBinaryMatchContext(operands[3], new(binary));

        return BeamBinaryExecution.Match(code, registers, match.Operands);
    }

    internal static void ValidateFunction(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyDictionary<int, BeamReceivePlan> receives
    )
    {
        // No candidate scratch exists in delay-only plans. The frame worklist
        // separately rejects every path that has seen a non-create binary role.
        if (receives.Count != 0 && receives.Values.All(plan => !plan.HasMatcher))
            return;

        var matcher = new HashSet<int>(receives.Values.SelectMany(plan => plan.MatcherInstructions));
        var pairs = new HashSet<int>();
        foreach (int pc in matcher)
            if (code.Instructions[pc].Definition.Name == BeamBinaryNames.StartLegacy)
            {
                _ = Match(code, pc);
                if (!matcher.Contains(pc + 1))
                    throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                pairs.Add(pc);
                pairs.Add(pc + 1);
            }
        // Only receive-candidate scratch must use the reviewed adjacent pair.
        // Ordinary binary blocks are validated by the exceptional role worklist;
        // their contexts/positions must be dead before another receive begins.
        foreach (int pc in matcher)
            if (IsBinary(code.Instructions[pc].Definition.Name) && !pairs.Contains(pc))
                throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
    }

    internal static bool HasOrdinaryScratch(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyDictionary<int, BeamReceivePlan> receives
    )
    {
        var matcher = new HashSet<int>(receives.Values.SelectMany(plan => plan.MatcherInstructions));
        for (int pc = function.FirstInstructionIndex; pc < function.EndInstructionIndex; pc++)
            if (!matcher.Contains(pc) && IsBinary(code.Instructions[pc].Definition.Name)
                && code.Instructions[pc].Definition.Name != BeamBinaryNames.Create)
                return true;

        return false;
    }

    internal static bool IsBinary(string name)
        => name is BeamBinaryNames.Start or BeamBinaryNames.Match or BeamBinaryNames.Create
            or BeamBinaryNames.StartLegacy or BeamBinaryNames.ExtractBinary or BeamBinaryNames.InitializeWritable
            or BeamBinaryNames.ExtractTail or BeamBinaryNames.SavePosition or BeamBinaryNames.RestorePosition
            or BeamFloatStringNames.ExtractFloat or BeamFloatStringNames.MatchString or BeamBinaryNames.ExtractInteger
            or BeamBinaryNames.ExtractUtf16 or BeamBinaryNames.ExtractUtf32 or BeamBinaryNames.ExtractUtf8
            or BeamBinaryNames.SkipUtf16 or BeamBinaryNames.SkipUtf32 or BeamBinaryNames.SkipUtf8 or BeamBinaryNames.SkipBits;
}
