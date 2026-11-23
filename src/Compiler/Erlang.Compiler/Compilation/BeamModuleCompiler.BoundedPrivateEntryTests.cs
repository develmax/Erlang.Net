using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateBoundedPrivateEntryTests(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 }
            || !code.Span.Contains(ExactEqualTest) && !code.Span.Contains(SelectValue))
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: FunctionInfo, Operands: [_, var function, var arity] }
                || body[index + 1].Definition.Opcode != Label)
            {
                continue;
            }

            var signature = (atomNames[(int)function.Value - 1], (int)arity.Value);
            if (exportedFunctions.Contains(signature)
                || !properListSourceFunctions.TryGetValue(signature, out FunctionDefinition? definition)
                || definition.Clauses is not [{ Guard: null, Patterns: var patterns }, ..])
            {
                continue;
            }

            int comparison = index + 2;
            while (comparison < body.Count && body[comparison].Definition.Opcode == Line)
            {
                comparison++;
            }

            if (comparison < body.Count && body[comparison] is
                {
                    Definition.Opcode: SelectValue,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } selector, _, _, ..]
                }
                && selector.Value < patterns.Count
                && ReadIntegerEntryRange(signature, (int)selector.Value) is { Lower: 0, Upper: byte.MaxValue }
                && HasOnlyOctetKeys(body[comparison].Operands))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    body[comparison],
                    0,
                    IntegerTypeBits,
                    0,
                    byte.MaxValue
                );
                continue;
            }

            if (comparison >= body.Count || body[comparison] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                        _,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tested,
                    { Kind: BeamOperandParameters.Integer } literal]
                }
                || tested.Value >= patterns.Count || !IsEntryIntegerPattern(patterns[(int)tested.Value], literal.Value))
            {
                continue;
            }

            int argument = (int)tested.Value;
            var bounds = ReadIntegerEntryRange(signature, argument);
            if (bounds is { } finite
                ? definition.Clauses.Count != 1 || finite.Lower == finite.Upper
                    || literal.Value < finite.Lower || literal.Value > finite.Upper
                : !HasIntegerEntryFact(signature, argument)
                    && !(argument == 0 && literal.Value.IsZero && HasIntegerPairCounterEntry(definition)))
            {
                continue;
            }

            // The first instruction sees the original private argument on every caller edge.
            copied = AnnotateRegisterInput(
                code,
                output,
                copied,
                body[comparison],
                1,
                IntegerTypeBits,
                bounds?.Lower,
                bounds?.Upper
            );
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool HasOnlyOctetKeys(IReadOnlyList<BeamOperand> operands)
    {
        for (int index = 3; index < operands.Count; index += 2)
        {
            if (operands[index] is not { Kind: BeamOperandParameters.Integer, Value: var key } || key < 0 || key > byte.MaxValue)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasIntegerPairCounterEntry(FunctionDefinition function)
    {
        if (function.Clauses is not
            [
            { Guard: null, Patterns: [Pattern.Literal { Value: Integer { Value.IsZero: true } }, ..] },
            { Guard: null, Patterns: [Pattern.Variable input, ..] } step
            ] || input.Name == VariableScopeNames.Wildcard)
        {
            return false;
        }

        var recursive = LocalFunctionEntries.Walk(step.Body).OfType<Expr.Call>().Where(call =>
            call.Module is null && !call.DynamicRemote && call.Function == function.Name
            && call.Arguments.Count == function.Arity).ToArray();
        if (recursive.Length == 0 || recursive.Any(call => call.Arguments[0] is not Expr.Binary
            {
                Operator: ErlangOperators.Minus,
                Left: Expr.Variable value,
                Right: Expr.Literal { Value: Integer { Value.IsOne: true } }
            } || value.Name != input.Name))
        {
            return false;
        }

        // The original interval facts keep their existing rules. This fallback
        // transfers only the integer kind from a matched pair and its back edge.
        return ReadPrivateEntryFacts()?.ProvesIntegerPairMember((function.Name, function.Arity), 0) == true;
    }

    private static bool IsEntryIntegerPattern(Pattern pattern, BigInteger value) =>
        pattern switch
        {
            Pattern.Literal { Value: Integer integer } => integer.Value == value,
            AliasPattern alias => IsEntryIntegerPattern(alias.Left, value) || IsEntryIntegerPattern(alias.Right, value),
            _ => false
        };
}
