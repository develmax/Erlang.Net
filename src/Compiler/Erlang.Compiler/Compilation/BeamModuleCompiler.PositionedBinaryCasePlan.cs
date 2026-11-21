using System.Numerics;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    internal sealed record PositionedBinaryCasePlan(
        BitPattern Producer,
        string Suffix,
        Expr.Match Padding,
        Expr.Call Size,
        Integer Mask,
        Expr.Match Result,
        Expr.Case Selector,
        IReadOnlyList<PositionedBinaryCasePlan.Arm> Arms
    )
    {
        private const int MaximumDiscriminatorBits = 64;

        internal sealed record Arm(
            Clause Clause,
            BigInteger Prefix,
            int PrefixWidth,
            Expr.Variable Padding,
            BitPattern Remainder
        );

        internal static PositionedBinaryCasePlan? Read(
            ModuleDefinition module,
            FunctionDefinition function,
            Expr.Case ownerCase,
            Clause ownerArm
        )
        {
            if (module.HasOtherCompileOptions || module.DisableModuleOptimization
                || ownerArm is not
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments: [_, .., var tail] } producer],
                    Body: Expr.Sequence { Items: [var padding, var result, _, ..] }
                }
                || tail is not
                {
                    Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } suffix,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BitStringUnit }
                }
                || !producer.Segments.SkipLast(1).All(segment => ReadLiteralPrefix(segment) is not null)
                || padding is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } pad,
                    Value: Expr.Binary
                    {
                        Operator: ErlangOperators.BitwiseAnd,
                        Left: Expr.Call
                        {
                            DynamicRemote: false,
                            Function: AutoImportNames.BitSize,
                            Arguments: [Expr.Variable sizeInput]
                        } size,
                        Right: Expr.Literal { Value: Integer mask }
                    }
                } padBinding
                || mask.Value.Sign < 0 || mask.Value > int.MaxValue
                || result is not Expr.Match
                {
                    Pattern: Pattern.Tuple
                    {
                        Items: [Pattern.Variable first, Pattern.Variable second]
                    },
                    Value: Expr.Case { Value: Expr.Variable selectorInput, Clauses.Count: > 1 } selector
                } resultBinding
                || first.Name == second.Name || first.Name == VariableScopeNames.Wildcard || second.Name == VariableScopeNames.Wildcard
                || suffix.Name == pad.Name || sizeInput.Name != suffix.Name || selectorInput.Name != suffix.Name
                || size.Module != ErlangModuleNames.Module
                    && (size.Module is not null || !SourceCallResolution.IsGuardAutoImport(module, size.Function, 1))
                || ownerCase.Clauses.Count(clause => ReferenceEquals(clause, ownerArm)) != 1)
            {
                return null;
            }

            var expressions = LocalFunctionEntries.ClauseExpressions(function.Clauses)
                .SelectMany(LocalFunctionEntries.Walk).ToArray();
            if (expressions.Count(expression => ReferenceEquals(expression, ownerCase)) != 1
                || expressions.Any(expression => expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe
                    || expression is Expr.Fun fun && (fun.SelfName == suffix.Name || fun.SelfName == pad.Name)))
            {
                return null;
            }

            var bindings = function.Clauses.SelectMany(clause => clause.Patterns)
                .Concat(expressions.SelectMany(NestedPatterns))
                .SelectMany(Semantics.Variables).ToArray();
            if (bindings.Count(name => name == suffix.Name) != 1 || bindings.Count(name => name == pad.Name) != 1
                || !HasExactReads(expressions, suffix.Name, [sizeInput, selectorInput]))
            {
                return null;
            }

            var arms = new List<Arm>();
            foreach (var clause in selector.Clauses)
            {
                if (ReadArm(clause, pad.Name) is not { } arm || arms.Any(other => PrefixesOverlap(other, arm)))
                {
                    return null;
                }

                arms.Add(arm);
            }

            if (!HasExactReads(expressions, pad.Name, arms.Select(arm => arm.Padding).ToArray()))
            {
                return null;
            }

            return new(
                producer,
                suffix.Name,
                padBinding,
                size,
                mask,
                resultBinding,
                selector,
                arms.AsReadOnly()
            );
        }

        private static Arm? ReadArm(Clause clause, string pad)
        {
            if (clause.Patterns is not [BitPattern { Segments: [var skip, _, _, .., var tail] } bits]
                || skip is not
                {
                    Value: Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Specification:
                    {
                        Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.BitStringUnit,
                        Signed: false, Endian: BitByteOrders.Big, Size: Expr.Variable padding
                    }
                }
                || padding.Name != pad || tail is not
                {
                    Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } remainder,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BitStringUnit }
                })
            {
                return null;
            }

            BigInteger prefix = BigInteger.Zero;
            int width = 0;
            int segment = 1;
            for (; segment < bits.Segments.Count - 1; segment++)
            {
                if (ReadLiteralPrefix(bits.Segments[segment]) is not { } literal)
                {
                    break;
                }

                width += literal.Width;
                if (width > MaximumDiscriminatorBits)
                {
                    return null;
                }

                prefix = (prefix << literal.Width) | literal.Value;
            }

            if (width == 0 || segment != bits.Segments.Count - 2
                || bits.Segments[segment] is not
                {
                    Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } capture,
                    Specification:
                    {
                        Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.BitStringUnit,
                        Signed: false, Endian: BitByteOrders.Big,
                        Size: Expr.Literal { Value: Integer count }
                    }
                }
                || count.Value.Sign <= 0 || count.Value > MaximumDiscriminatorBits || capture.Name == remainder.Name)
            {
                return null;
            }

            return new(
                clause,
                prefix,
                width,
                padding,
                bits with { Segments = bits.Segments.Skip(segment).ToArray() }
            );
        }

        private static (BigInteger Value, int Width)? ReadLiteralPrefix(BitPatternSegment segment)
        {
            if (segment is not
                {
                    Value: Pattern.Literal { Value: Integer value },
                    Specification:
                    {
                        Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.BitStringUnit,
                        Signed: false, Endian: BitByteOrders.Big,
                        Size: Expr.Literal { Value: Integer width }
                    }
                }
                || width.Value.Sign <= 0 || width.Value > MaximumDiscriminatorBits
                || value.Value.Sign < 0 || value.Value >= BigInteger.One << (int)width.Value)
            {
                return null;
            }

            return (value.Value, (int)width.Value);
        }

        private static bool HasExactReads(IEnumerable<Expr> expressions, string name, IReadOnlyList<Expr.Variable> expected)
        {
            var actual = expressions.OfType<Expr.Variable>().Where(variable => variable.Name == name).ToArray();

            return actual.Length == expected.Count
                && expected.All(reference => actual.Count(variable => ReferenceEquals(variable, reference)) == 1);
        }

        private static bool PrefixesOverlap(Arm first, Arm second)
        {
            int width = Math.Min(first.PrefixWidth, second.PrefixWidth);

            return (first.Prefix >> (first.PrefixWidth - width)) == (second.Prefix >> (second.PrefixWidth - width));
        }
    }
}
