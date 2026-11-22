using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateRepeatedNumericInputs(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo))
        {
            return null;
        }

        int erlang = atoms.GetValueOrDefault(ErlangModuleNames.Module);
        var operations = new HashSet<int>();
        foreach (string name in new[] { ErlangOperators.Plus, ErlangOperators.Minus, ErlangOperators.Multiply })
        {
            if (atoms.TryGetValue(name, out int atom) && imports.TryGetValue((erlang, atom, 2), out int import))
            {
                operations.Add(import);
            }
        }

        if (operations.Count == 0 || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        var numbers = new Dictionary<BeamOperand, bool>();
        int copied = 0;
        foreach (BeamInstruction instruction in body)
        {
            var operands = instruction.Operands;
            if (IsOrdinaryBinaryBif(instruction, operations))
            {
                // Pinned beam_call_types meets the original operands with number
                // on the successful continuation of these GC BIFs. Preserve that
                // fact only for the same physical value and for live GC roots.
                for (int index = 3; index <= 4; index++)
                {
                    BeamOperand argument = operands[index];
                    BeamOperand register = WithoutType(argument);
                    if (argument.TypeIndex is null && numbers.GetValueOrDefault(register))
                    {
                        copied = AnnotateRegisterInput(
                            code,
                            output,
                            copied,
                            instruction,
                            index,
                            NumberTypeBits
                        );
                    }
                }

                for (int index = 3; index <= 4; index++)
                {
                    BeamOperand register = WithoutType(operands[index]);
                    if (register.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
                        && (register.Kind == BeamOperandParameters.Y || register.Value < operands[1].Value))
                    {
                        numbers.TryAdd(register, false);
                    }
                }

                Forget(operand => operand.Kind == BeamOperandParameters.X && operand.Value >= operands[1].Value);
                numbers.Remove(WithoutType(operands[^1]));
                continue;
            }

            switch (instruction.Definition.Opcode)
            {
                case Move:
                    BeamOperand source = WithoutType(operands[0]);
                    BeamOperand destination = WithoutType(operands[1]);
                    if (numbers.TryGetValue(source, out bool acrossCall))
                    {
                        numbers[destination] = acrossCall;
                    }
                    else
                    {
                        numbers.Remove(destination);
                    }

                    break;
                case Allocate or AllocateHeap:
                    Forget(operand => operand.Kind == BeamOperandParameters.Y || operand.Value >= operands[^1].Value);
                    break;
                case TestHeap:
                    Forget(operand => operand.Kind == BeamOperandParameters.X && operand.Value >= operands[^1].Value);
                    break;
                case InitYRegisters:
                    foreach (BeamOperand operand in operands)
                    {
                        numbers.Remove(WithoutType(operand));
                    }

                    break;
                case TrimFrame:
                    var released = operands[0].Value;
                    numbers = numbers
                        .Where(pair => pair.Key.Kind == BeamOperandParameters.X || pair.Key.Value >= released)
                        .ToDictionary(
                            pair => pair.Key.Kind == BeamOperandParameters.X
                                ? pair.Key : pair.Key with { Value = pair.Key.Value - released },
                            pair => pair.Value
                        );
                    break;
                case ExternalCall or LocalCall or DynamicApply or CallFun or CallFunTyped:
                    Forget(operand => operand.Kind == BeamOperandParameters.X);
                    foreach (BeamOperand slot in numbers.Keys.ToArray())
                    {
                        numbers[slot] = true;
                    }

                    break;
                case Line:
                    break;
                default:
                    numbers.Clear();
                    break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Forget(Func<BeamOperand, bool> predicate)
        {
            foreach (BeamOperand register in numbers.Keys.Where(predicate).ToArray())
            {
                numbers.Remove(register);
            }
        }

        static BeamOperand WithoutType(BeamOperand operand) =>
            operand.TypeIndex is null ? operand : operand with { TypeIndex = null };
    }
}
