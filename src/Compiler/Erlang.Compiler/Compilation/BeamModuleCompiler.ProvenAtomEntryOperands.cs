using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateProvenAtomEntryOperands(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        if (properListSourceFunctions is not { } definitions || ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || (!code.Span.Contains(ExactEqualTest) && !code.Span.Contains(SelectValue))
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        string[] names = atoms.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
        using var output = new MemoryStream();
        GeneralEmitter? emitter = null;
        int copied = 0;
        for (int entry = 0; entry + 2 < body.Count; entry++)
        {
            if (body[entry] is not { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arity] }
                || body[entry + 1].Definition.Opcode != Label)
            {
                continue;
            }

            var signature = (Name: names[(int)name.Value - 1], Arity: (int)arity.Value);
            if (exportedFunctions.Contains(signature) || IsDeclaredNative(signature)
                || !definitions.TryGetValue(signature, out var definition))
            {
                continue;
            }

            // Every successful edge in the entry corridor still sees the original X arguments.
            for (int index = entry + 2; index < body.Count; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Line)
                {
                    continue;
                }

                if (instruction is
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } tested,
                        { Kind: BeamOperandParameters.Atom } literal]
                    })
                {
                    int argument = (int)tested.Value;
                    if (argument < signature.Arity
                        && HasSingleSourceAtomTest(definition, argument, names[(int)literal.Value - 1]))
                    {
                        Annotate(
                            instruction,
                            operand: 1,
                            definition,
                            argument
                        );
                    }

                    continue;
                }

                if (instruction is
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } selected, _, _, _, _, _, _, ..]
                    }
                    && selected.Value < signature.Arity && IsAtomEntrySelection(instruction.Operands))
                {
                    Annotate(
                        instruction,
                        operand: 0,
                        definition,
                        (int)selected.Value
                    );
                }

                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Annotate(
            BeamInstruction instruction,
            int operand,
            FunctionDefinition definition,
            int argument
        )
        {
            emitter ??= new GeneralEmitter(
                this,
                localEntries,
                definitions,
                default,
                frameSize: 0
            );
            if (emitter.ReadAtomEntryType(definition, argument) != 0)
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    operand,
                    AtomTypeBits
                );
            }
        }
    }

    private static bool HasSingleSourceAtomTest(FunctionDefinition function, int argument, string spelling)
    {
        if (function.Clauses.Count == 0
            || ReadAtom(function.Clauses[0].Patterns[argument])?.Name != spelling)
        {
            return false;
        }

        // A source selector over different atoms can become a new equality
        // during OTP switch simplification, where the operand hint is absent.
        return function.Clauses.All(clause =>
            clause.Patterns[argument] is Pattern.Variable || ReadAtom(clause.Patterns[argument])?.Name == spelling);

        static Atom? ReadAtom(Pattern pattern) =>
            pattern switch
            {
                Pattern.Literal { Value: Atom atom } => atom,
                AliasPattern { Left: Pattern.Literal { Value: Atom atom }, Right: Pattern.Variable } => atom,
                AliasPattern { Left: Pattern.Variable, Right: Pattern.Literal { Value: Atom atom } } => atom,
                _ => null
            };
    }

    private static bool IsAtomEntrySelection(IReadOnlyList<BeamOperand> operands)
    {
        for (int index = 3; index + 1 < operands.Count; index += 2)
        {
            if (operands[index].Kind != BeamOperandParameters.Atom)
            {
                return false;
            }
        }

        return true;
    }

    private byte[]? AnnotateOriginalAtomGuardInputs(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (properListSourceFunctions is not { } definitions || ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(ExactEqualTest)
            || (!code.Span.Contains(GetList) && !code.Span.Contains(GetHead)
                && !code.Span.Contains(GetTail) && !code.Span.Contains(GetTupleElement))
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var names = atoms.Where(pair => MapEmittedAtom(pair.Value) > 0)
            .ToDictionary(pair => MapEmittedAtom(pair.Value), pair => pair.Key);
        using var output = new MemoryStream(code.Length);
        GeneralEmitter? emitter = null;
        int copied = 0;
        for (int entry = 0; entry + 2 < body.Count; entry++)
        {
            if (body[entry] is not { Definition.Opcode: FunctionInfo, Operands: [_, var atom, var arity] }
                || body[entry + 1].Definition.Opcode != Label
                || !names.TryGetValue((int)atom.Value, out string? name))
            {
                continue;
            }

            var signature = (name, (int)arity.Value);
            if (exportedFunctions.Contains(signature) || IsDeclaredNative(signature)
                || !definitions.TryGetValue(signature, out var function))
            {
                continue;
            }

            // The next label ends the protected fallthrough. Track which X
            // registers still carry their original arguments on this path.
            bool checkedShape = false;
            var overwritten = new HashSet<int>();
            int index = entry + 2;
            for (; index < body.Count; index++)
            {
                BeamInstruction operation = body[index];
                if (operation.Definition.Opcode is IsNonemptyList or IsTuple or TestArity or IsTaggedTuple)
                {
                    checkedShape = true;
                    continue;
                }

                if (operation is { Definition.Opcode: GetList, Operands: [_, var head, var tail] })
                {
                    RecordWrite(head);
                    RecordWrite(tail);
                    continue;
                }

                if (operation is { Definition.Opcode: GetHead or GetTail, Operands: [_, var projected] })
                {
                    RecordWrite(projected);
                    continue;
                }

                if (operation is { Definition.Opcode: GetTupleElement, Operands: [_, _, var field] })
                {
                    RecordWrite(field);
                    continue;
                }

                if (operation.Definition.Opcode == Line)
                {
                    continue;
                }

                break;
            }

            if (!checkedShape || index >= body.Count || body[index] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [_, { Kind: BeamOperandParameters.X } left, { Kind: BeamOperandParameters.X } right]
                } comparison)
            {
                continue;
            }

            Annotate(comparison, 1, left);
            Annotate(comparison, 2, right);

            void Annotate(BeamInstruction instruction, int operand, BeamOperand value)
            {
                if (value.TypeIndex is not null || value.Value >= signature.Item2 || overwritten.Contains((int)value.Value))
                {
                    return;
                }

                emitter ??= new GeneralEmitter(
                    this,
                    new Dictionary<(string Name, int Arity), int>(),
                    definitions,
                    default,
                    frameSize: 0
                );
                if (emitter.ReadAtomEntryType(function, (int)value.Value) != 0)
                {
                    copied = AnnotateRegisterInput(
                        code,
                        output,
                        copied,
                        instruction,
                        operand,
                        AtomTypeBits
                    );
                }
            }

            void RecordWrite(BeamOperand destination)
            {
                if (destination.Kind == BeamOperandParameters.X && destination.Value < signature.Item2)
                {
                    overwritten.Add((int)destination.Value);
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
}
