using System.Numerics;

namespace Erlang;

internal static class BeamBinaryEntryProtocol
{
    internal static bool HasLocalContextCall(BeamLinkedCode code, BeamLinkedFunction function)
        => code.Instructions.Skip(function.FirstInstructionIndex)
            .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
            .Any(instruction => instruction.Definition.Name is BeamRegisterNames.Call or BeamLinkNames.LocalCall or BeamGeneralOpcodeNames.LocalLast
                && ContextMask(code, code.Functions.Single(target => target.EntryLabel == instruction.Operands[1].Value)) != BigInteger.Zero);

    internal static BigInteger ContextMask(BeamLinkedCode code, BeamLinkedFunction function)
    {
        var entry = ResolveContextEntry(code, function);
        if (entry is null)
            return BigInteger.Zero;
        BeamInstruction instruction = entry.Value.Instruction;
        if (instruction.Definition.Name == BeamBinaryNames.StartLegacy)
            return BigInteger.One << entry.Value.Argument;
        if (instruction.Operands[0].Kind != BeamOperandParameters.Atom)
            return BigInteger.Zero;
        string mode = code.Structure.Atoms[(int)instruction.Operands[0].Value]!;

        return mode is BeamBinaryNames.NoFail or BeamBinaryNames.Resume
            ? BigInteger.One << entry.Value.Argument : BigInteger.Zero;
    }

    internal static bool RequiresContext(BeamLinkedCode code, BeamLinkedFunction function)
    {
        BeamInstruction? instruction = ResolveContextEntry(code, function)?.Instruction;

        return instruction is not null && instruction.Definition.Name == BeamBinaryNames.Start
            && instruction.Operands[0].Kind == BeamOperandParameters.Atom
            && code.Structure.Atoms[(int)instruction.Operands[0].Value] == BeamBinaryNames.Resume;
    }

    private static (BeamInstruction Instruction, int Argument)? ResolveContextEntry(BeamLinkedCode code, BeamLinkedFunction function)
    {
        // Transparent allocation/Y initialization does not consume or replace any X argument.
        // Track exact incoming origins through a bounded move/tail-call prefix.
        // Literal writes never authenticate a context; unknown prefixes remain refused.
        var origins = Enumerable.Range(0, function.Arity).ToDictionary(index => index);
        bool movedTailPrefix = false;
        var seen = new HashSet<int>();
        while (seen.Add(function.EntryLabel))
        {
            bool allocated = false;
            BeamLinkedFunction? forwarded = null;
            for (int pc = function.FirstInstructionIndex; pc < function.EndInstructionIndex; pc++)
            {
                BeamInstruction instruction = code.Instructions[pc];
                string name = instruction.Definition.Name;
                if (name is BeamLinkNames.Line or BeamLinkNames.Label)
                    continue;
                if (name is BeamBinaryNames.Start or BeamBinaryNames.StartLegacy)
                {
                    if (movedTailPrefix && (name != BeamBinaryNames.Start
                        || instruction.Operands[0].Kind != BeamOperandParameters.Atom
                        || code.Structure.Atoms[(int)instruction.Operands[0].Value] != BeamBinaryNames.Resume))
                        return null;
                    BeamOperand source = instruction.Operands[name == BeamBinaryNames.StartLegacy ? 1 : 2];

                    return source.Kind == BeamOperandParameters.X && source.Value < function.Arity
                        && origins.TryGetValue((int)source.Value, out int argument)
                        ? (instruction, argument) : null;
                }
                if (!allocated && name == BeamLinkNames.Move)
                {
                    movedTailPrefix = true;
                    BeamOperand source = instruction.Operands[0];
                    BeamOperand target = instruction.Operands[1];
                    if (target.Kind != BeamOperandParameters.X || target.Value >= BeamOperandParameters.MaximumRegister
                        || source.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Integer))
                        return null;
                    if (source.Kind == BeamOperandParameters.X && origins.TryGetValue((int)source.Value, out int argument))
                        origins[(int)target.Value] = argument;
                    else
                        origins.Remove((int)target.Value);
                    continue;
                }
                if (!allocated && name == BeamLinkNames.LocalCall)
                {
                    movedTailPrefix = true;
                    forwarded = code.Functions.SingleOrDefault(target => target.EntryLabel == instruction.Operands[1].Value
                        && target.Arity == instruction.Operands[0].Value);
                    break;
                }
                if (!allocated && name is (BeamLinkNames.Allocate or BeamLinkNames.AllocateHeap))
                {
                    int liveIndex = name == BeamLinkNames.AllocateHeap ? 2 : 1;
                    if (instruction.Operands[liveIndex].Value != function.Arity)
                        return null;
                    allocated = true;
                    continue;
                }
                if (allocated && name == BeamOnLoadNames.InitY)
                    continue;
                if (allocated && name == BeamRegisterNames.Call && instruction.Operands[0].Value == function.Arity)
                {
                    forwarded = code.Functions.SingleOrDefault(target => target.EntryLabel == instruction.Operands[1].Value
                        && target.Arity == function.Arity);
                    break;
                }

                return null;
            }
            if (forwarded is null)
                return null;
            foreach (int register in origins.Keys.Where(index => index >= forwarded.Arity).ToArray())
                origins.Remove(register);
            function = forwarded;
        }

        return null;
    }

    internal static void RequireEntry(BeamLinkedCode code, BeamLinkedFunction function, BeamRegisterState registers)
    {
        BigInteger allowed = ContextMask(code, function);
        bool required = RequiresContext(code, function);
        for (int index = 0; index < function.Arity; index++)
        {
            BeamRegisterSlot slot = registers.ReadSlot(new(BeamOperandParameters.X, index));
            bool accepts = (allowed & (BigInteger.One << index)) != 0;
            if (slot.Kind == BeamRegisterSlotKind.BinaryMatchContext && accepts)
                continue;
            if (slot.Kind != BeamRegisterSlotKind.Term || required && accepts)
                throw new InvalidDataException(BeamBinaryNames.InvalidContext);
        }
    }

    internal static bool IsContextSizeImport(BeamLinkedCode code, int index)
    {
        BeamImport import = code.Structure.Imports[index];

        return import.Arity == 1 && code.Structure.Atoms[import.ModuleAtomIndex] == ErlangModuleNames.Module
            && code.Structure.Atoms[import.FunctionAtomIndex] is BeamBinaryNames.BitSize or BeamBinaryNames.ByteSize;
    }

    internal static Term SizeArgument(
        BeamLinkedCode code,
        BeamRegisterState registers,
        BeamExternalImport import,
        BeamOperand source
    )
    {
        if (import.Module == ErlangModuleNames.Module && import.Arity == 1
            && import.Function is BeamBinaryNames.BitSize or BeamBinaryNames.ByteSize
            && source.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
            && registers.ReadSlot(source).Kind == BeamRegisterSlotKind.BinaryMatchContext)
        {
            BeamBinaryMatchContext match = registers.ReadBinaryMatchContext(source);

            return match.Source.SliceBits(match.Position, match.Source.BitLength - match.Position);
        }

        return BeamRegisterInterpreter.Read(code, registers, source);
    }
}
