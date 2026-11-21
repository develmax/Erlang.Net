using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateBoundedPrivateEntryTests(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 }
            || !code.Span.Contains(ExactEqualTest))
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
                : !HasIntegerEntryFact(signature, argument))
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

    private static bool IsEntryIntegerPattern(Pattern pattern, BigInteger value) =>
        pattern switch
        {
            Pattern.Literal { Value: Integer integer } => integer.Value == value,
            AliasPattern alias => IsEntryIntegerPattern(alias.Left, value) || IsEntryIntegerPattern(alias.Right, value),
            _ => false
        };
}
