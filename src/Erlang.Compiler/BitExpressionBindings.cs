// Modified: C# adaptation of OTP-29.1.1 eval_bits:expr_grp/expr_grp1/eval_field.
// Evaluates values/sizes before construction, with sequential interpreter scopes.
// Compiled binary pre-expressions are also sequential, following v3_core:expr_bin_1.
// Modified: adapt v3_core:bitstr empty-string size prechecks and segment elimination.
// Binary storage, error metadata and resource limits remain CLR adaptations.
namespace Erlang.Compiler;

internal static class BitExpressionBindings
{
    public static async ValueTask<BitString> Evaluate(
        Expr.Bits bits,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        bool compiled
    )
    {
        var segments = new List<(Term Value, Term? Size, BitSegment Segment)>(bits.Segments.Count);
        var scope = CompiledBindingScope.Copy(bindings);
        foreach (var segment in bits.Segments)
        {
            var value = await evaluate(segment.Value, scope);
            var size = segment.Size is null ? null : await evaluate(segment.Size, scope);
            if (compiled && segment.IsStringLiteral && value is Nil && segment.Type is BitSegmentTypes.Integer or BitSegmentTypes.Float)
            {
                if (size is null || size is Integer { Value.Sign: >= 0 })
                    continue;
                // Literal failures are lowered to a failed binary after its pre-expressions.
                // Dynamic sizes carry an immediate integer/nonnegative precheck.
                if (segment.Size is not Expr.Literal)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            segments.Add((value, size, segment));
        }
        var result = BitConstruction.Create(segments, encodeFloatInOrder: !compiled);
        foreach (var binding in scope)
            bindings[binding.Key] = binding.Value;

        return result;
    }

}
