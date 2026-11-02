using System.Runtime.CompilerServices;

namespace Erlang;

// Storage sizing only. The original global register validation, source/image
// admission and every physical root publication remain separate requirements.
internal static class BeamRegisterCapacity273
{
    // The exception ABI writes marker/reason/trace/class to X0 through X3.
    internal const int MinimumCapacity = 4;
    private static readonly ConditionalWeakTable<BeamLinkedCode, Capacity> capacities = new();

    internal static int ForEntry(
        BeamLinkedCode code,
        BeamLinkedFunction entry,
        int arguments,
        int environment
    )
    {
        // Only the existing private fresh-reader seam certifies immutable code.
        // Raw IR, equal records, with-clones and unvalidated entries stay full.
        if (!PreparedBeamValidationCache.TryGet(code, out PreparedBeamValidationCertificate? certificate)
            || certificate?.HasSuccess(entry) != true
            || arguments < 0 || environment < 0)
            return BeamOperandParameters.MaximumRegister;
        long actual = (long)arguments + environment;
        if (actual > BeamOperandParameters.MaximumRegister)
            return BeamOperandParameters.MaximumRegister; // Preserve the old invalid import route.
        int capacity = capacities.GetValue(code, static current => new(Conservative(current))).Count;

        return Math.Max(capacity, (int)actual);
    }

    private static int Conservative(BeamLinkedCode code)
    {
        int capacity = MinimumCapacity;
        foreach (BeamLinkedFunction function in code.Functions)
        {
            if (function.Arity < 0 || function.Arity > BeamOperandParameters.MaximumRegister)
                return BeamOperandParameters.MaximumRegister;
            capacity = Math.Max(capacity, function.Arity);
        }
        foreach (BeamLambdaEntry lambda in code.Lambdas)
        {
            if (lambda.TotalArity < 0 || lambda.TotalArity > BeamOperandParameters.MaximumRegister
                || lambda.FreeVariables < 0 || lambda.FreeVariables > lambda.TotalArity)
                return BeamOperandParameters.MaximumRegister;
            capacity = Math.Max(capacity, lambda.TotalArity);
        }
        // Linked operands already flatten argument lists and bs_match commands.
        // Only closed, validated unsigned metadata/Y/heap roles are omitted.
        // Unknown unsigned/count roles retain the conservative bound. Match
        // commands and selection tables are parsed by their existing validators.
        // +2 covers implicit apply module/function slots and overbounds call_fun.
        foreach (BeamInstruction instruction in code.Instructions)
        {
            bool parsedMatch475 = instruction.Definition.Name == BeamBinaryNames.Match;
            if (parsedMatch475 && !TryMatchCapacity475(code, instruction, ref capacity))
                return BeamOperandParameters.MaximumRegister;
            bool selection475 = instruction.Definition.Name == BeamGeneralOpcodeNames.SelectValue;
            if (selection475 && !IsSelectionTable475(code, instruction))
                return BeamOperandParameters.MaximumRegister;
            for (int operandIndex = 0; operandIndex < instruction.Operands.Count; operandIndex++)
            {
                BeamOperand operand = instruction.Operands[operandIndex];
                switch (operand.Kind)
                {
                    case BeamOperandParameters.X:
                        if (operand.Value < 0 || operand.Value >= BeamOperandParameters.MaximumRegister)
                            return BeamOperandParameters.MaximumRegister;
                        capacity = Math.Max(capacity, (int)operand.Value + 1);
                        break;
                    case BeamOperandParameters.Unsigned:
                    case BeamOperandParameters.Count:
                        if (operand.Value < 0)
                            return BeamOperandParameters.MaximumRegister;
                        if (parsedMatch475 || selection475 && operandIndex == 2 && operand.Kind == BeamOperandParameters.Count
                            || operand.Kind == BeamOperandParameters.Unsigned && IsNonXUnsignedRole473(instruction, operandIndex))
                            break;
                        if (operand.Value >= BeamOperandParameters.MaximumRegister - BeamApplyNames.ExtraRegisters)
                            return BeamOperandParameters.MaximumRegister;
                        capacity = Math.Max(capacity, (int)operand.Value + BeamApplyNames.ExtraRegisters);
                        break;
                    case BeamOperandParameters.Integer:
                        break;
                    case BeamOperandParameters.Atom:
                    case BeamOperandParameters.Nil:
                    case BeamOperandParameters.Label:
                    case BeamOperandParameters.Failure:
                    case BeamOperandParameters.Y:
                    case BeamOperandParameters.Float:
                    case BeamOperandParameters.LiteralValue:
                        if (operand.Value < 0)
                            return BeamOperandParameters.MaximumRegister;
                        break;
                    default:
                        return BeamOperandParameters.MaximumRegister;
                }
            }
        }

        return capacity;
    }

    // Production ForEntry requires exact prepared-code/entry certification.
    // Conservative also remains fail-closed for direct structural inspection:
    // only these exact operand positions/layouts omit an unsigned scalar.
    // Explicit X and unknown layouts retain the original bound.
    private static bool IsNonXUnsignedRole473(BeamInstruction instruction, int index)
        => instruction.Definition.Name switch
        {
            BeamLinkNames.Label or BeamLinkNames.Line => instruction.Operands.Count == 1 && index == 0,
            BeamLinkNames.Allocate or BeamLinkNames.HeapTest => instruction.Operands.Count == 2 && index == 0,
            BeamLinkNames.AllocateHeap => instruction.Operands.Count == 3 && index is 0 or 1,
            BeamLinkNames.Deallocate => instruction.Operands.Count == 1 && index == 0,
            BeamGeneralOpcodeNames.Trim => instruction.Operands.Count == 2 && index is 0 or 1,
            BeamGeneralOpcodeNames.LocalLast or BeamOnLoadNames.ExternalLast => instruction.Operands.Count == 3 && index == 2,
            _ => false
        };

    private static bool TryMatchCapacity475(BeamLinkedCode code, BeamInstruction instruction, ref int capacity)
    {
        IReadOnlyList<BeamBinaryCommand> commands;
        try
        {
            // Reuse the exact reader/validator/interpreter command grammar.
            // Unknown commands or malformed raw inspection stay conservative.
            commands = BeamBinaryCommands.Read(code.Structure, instruction.Operands);
        }
        catch (Exception error) when (error is NotSupportedException or InvalidDataException
            or ArgumentOutOfRangeException or IndexOutOfRangeException or OverflowException)
        {
            return false;
        }
        int required = capacity;
        foreach (BeamBinaryCommand command in commands)
        {
            if (command.Live == -1) continue;
            if (command.Live < 0 || command.Live >= BeamOperandParameters.MaximumRegister - BeamApplyNames.ExtraRegisters)
                return false;
            required = Math.Max(required, command.Live + BeamApplyNames.ExtraRegisters);
        }
        capacity = required;

        return true;
    }

    private static bool IsSelectionTable475(BeamLinkedCode code, BeamInstruction instruction)
    {
        IReadOnlyList<BeamOperand> operands = instruction.Operands;
        if (operands.Count < 3 || operands[0].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y
                or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue)
            || operands[1].Kind is not (BeamOperandParameters.Label or BeamOperandParameters.Failure)
            || operands[2].Kind != BeamOperandParameters.Count || operands[2].Value != operands.Count - 3
            || operands[2].Value % 2 != 0) return false;
        for (int index = 3; index < operands.Count; index += 2)
            if (operands[index].Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue)
                || operands[index + 1].Kind != BeamOperandParameters.Label) return false;
        try
        {
            BeamSelectValueValidation.Validate(code.Structure, operands);

            return true;
        }
        catch (Exception error) when (error is NotSupportedException or InvalidDataException
            or ArgumentOutOfRangeException or IndexOutOfRangeException or OverflowException)
        {
            return false;
        }
    }

    private sealed record Capacity(int Count);
}
