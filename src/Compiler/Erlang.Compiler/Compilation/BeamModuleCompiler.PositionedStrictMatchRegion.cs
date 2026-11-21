using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int PositionedStrictMatchRegionLength = 4;

        private bool TryEmitPositionedStrictMatchRegion(
            IReadOnlyList<Expr> items,
            int index,
            int discardedEnd,
            int? guardFailure
        )
        {
            if (guardFailure is not null || owner.receiveScratchRegisters is not null
                || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || parentFunction is null || index + PositionedStrictMatchRegionLength > discardedEnd
                || items[index] is not Expr.Match
                {
                    Pattern: BitPattern { Segments: [var head, var tail] } producerPattern,
                    Value: Expr.Variable input
                } producer
                || items[index + 1] is not Expr.Match
                {
                    Pattern: Pattern.Variable widthName,
                    Value: Expr.Binary
                    {
                        Operator: ErlangOperators.Plus,
                        Left: Expr.Variable widthInput,
                        Right: Expr.Literal { Value: Integer increment }
                    }
                } widthMatch
                || items[index + 2] is not Expr.Match
                {
                    Pattern: Pattern.Variable padName,
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
                    } pad
                }
                || items[index + 3] is not Expr.Match
                {
                    Pattern: BitPattern { Segments: [var skip, var field, var remainder] } rematchPattern,
                    Value: Expr.Variable rematchInput
                } rematch)
            {
                return false;
            }

            if (head.Value is not Pattern.Variable count
                || head.Specification is not
                {
                    Type: BitSegmentTypes.Integer, Signed: false, Unit: BitSyntaxDefaults.BitStringUnit
                }
                || FixedSize(head.Specification) is not int headWidth || headWidth <= 0 || headWidth >= 31
                || !IsPositionedSuffixSegment(tail, out var suffixName)
                || !IsPositionedSuffixSegment(remainder, out var remainderName)
                || skip.Value is not Pattern.Variable { Name: VariableScopeNames.Wildcard }
                || skip.Specification is not
                {
                    Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.BitStringUnit,
                    Endian: BitByteOrders.Big, Signed: false, Size: Expr.Variable skipCount
                }
                || field.Value is not Pattern.Variable payloadName
                || field.Specification is not
                {
                    Type: BitSegmentTypes.Binary or BitSegmentTypes.Integer, Size: Expr.Variable payloadWidth
                }
                || input.Name == VariableScopeNames.Wildcard || !variables.TryGetValue(input.Name, out int original)
                || increment.Value.Sign < 0 || mask.Value.Sign < 0 || mask.Value > int.MaxValue
                || ((BigInteger.One << headWidth) - 1 + increment.Value) * field.Specification.Unit > int.MaxValue
                || sizeInput.Name != suffixName || rematchInput.Name != suffixName
                || widthInput.Name != count.Name || payloadWidth.Name != widthName.Name || skipCount.Name != padName.Name
                || !owner.ResolvesErlangBif(size))
            {
                return false;
            }

            string[] bindings = [count.Name, suffixName, widthName.Name, padName.Name, payloadName.Name, remainderName];
            if (bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Length
                || bindings.Any(name => name == VariableScopeNames.Wildcard
                    || variables.ContainsKey(name) || deferredPairMembers.ContainsKey(name))
                || !OwnsPositionedSuffix(
                    producer,
                    rematch,
                    sizeInput,
                    rematchInput,
                    suffixName
                ))
            {
                return false;
            }

            int invalidPrefix = Label();
            int invalidRematch = Label();
            int done = Label();
            int context = Slot();
            int position = Slot();
            var wildcard = new Pattern.Variable(VariableScopeNames.Wildcard);
            var retained = tail with
            {
                Value = wildcard,
                Specification = tail.Specification with { Value = new Expr.Variable(wildcard.Name) }
            };
            EmitFixedBitPattern(
                producerPattern with { Segments = [head, retained] },
                original,
                invalidPrefix,
                positionedContext: context
            );

            Match(widthMatch, null, returnValue: false);
            int padding = EmitPositionedBitSizeMask(
                size,
                mask,
                pad.SourceLine,
                context
            );
            EmitPositionedPaddingSkip(
                context,
                position,
                padding,
                invalidRematch
            );
            EmitSequentialBitPatternFromContext(
                rematchPattern with { Segments = [field, remainder] },
                context,
                invalidRematch
            );
            JumpTo(done);

            Mark(invalidPrefix);
            Load(original);
            owner.EmitArithmeticLine(producer.SourceLine, ref location);
            owner.Op(BadMatch, XRegisterTag, 0);

            Mark(invalidRematch);
            RestorePositionedBinarySuffix(context, position);
            owner.EmitArithmeticLine(rematch.SourceLine, ref location);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(done);

            return true;
        }

        private int EmitPositionedBitSizeMask(
            Expr.Call size,
            Integer mask,
            int sourceLine,
            int context
        )
        {
            Load(context);
            owner.EmitCallLine(size, location);
            EmitSizeInstruction(size.Function, failure: 0, live: 1);
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(XRegisterTag, 0);

            var previous = CallLocation(size, location);
            owner.EmitArithmeticLine(sourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2));
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.LiteralOperand(mask);
            owner.Operand(XRegisterTag, 0);

            return Save();
        }

        private void EmitPositionedPaddingSkip(
            int context,
            int position,
            int padding,
            int failure
        )
        {
            Load(context);
            owner.Op(GetBinaryPosition, XRegisterTag, 0);
            owner.Operand(YRegisterTag, position);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(SkipBinaryBits, LabelTag, failure);
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(YRegisterTag, padding);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
        }

        private void RestorePositionedBinarySuffix(int context, int position)
        {
            Load(context);
            owner.Op(SetBinaryPosition, XRegisterTag, 0);
            owner.Operand(YRegisterTag, position);
            owner.Op(GetBinaryTail, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
        }

        private bool OwnsPositionedSuffix(
            Expr.Match producer,
            Expr.Match rematch,
            Expr.Variable sizeInput,
            Expr.Variable rematchInput,
            string suffix
        )
        {
            var function = parentFunction!;
            var expressions = LocalFunctionEntries.ClauseExpressions(function.Clauses)
                .SelectMany(LocalFunctionEntries.Walk).ToArray();
            if (expressions.Any(expression => expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe)
                || expressions.Count(expression => ReferenceEquals(expression, producer)) != 1
                || expressions.Count(expression => ReferenceEquals(expression, rematch)) != 1
                || expressions.OfType<Expr.Fun>().Any(fun => fun.SelfName == suffix))
            {
                return false;
            }

            var references = expressions.OfType<Expr.Variable>().Where(variable => variable.Name == suffix).ToArray();
            if (references.Count(reference => ReferenceEquals(reference, sizeInput)) != 1
                || references.Count(reference => ReferenceEquals(reference, rematchInput)) != 1)
            {
                return false;
            }

            int bindings = CountBindings(function.Clauses, expressions);
            if (bindings == 1 && references.Length == 2)
            {
                return true;
            }

            foreach (var decision in expressions.OfType<Expr.Case>())
            {
                var arm = decision.Clauses.FirstOrDefault(clause =>
                    LocalFunctionEntries.Walk(clause.Body).Any(expression => ReferenceEquals(expression, producer)));
                if (arm is null)
                {
                    continue;
                }

                var body = LocalFunctionEntries.Walk(arm.Body).ToArray();
                if (!body.Any(expression => ReferenceEquals(expression, sizeInput))
                    || !body.Any(expression => ReferenceEquals(expression, rematch)))
                {
                    continue;
                }

                // Subtract only exclusive sibling arms. The selector, enclosing scope, and
                // continuation after the case still count, including a use of an exported binding.
                var siblings = decision.Clauses.Where(clause => !ReferenceEquals(clause, arm)).ToArray();
                var siblingExpressions = LocalFunctionEntries.ClauseExpressions(siblings)
                    .SelectMany(LocalFunctionEntries.Walk).ToArray();
                int siblingReads = siblingExpressions.OfType<Expr.Variable>().Count(variable => variable.Name == suffix);
                if (bindings == CountBindings(siblings, siblingExpressions) + 1
                    && references.Length == siblingReads + 2)
                {
                    return true;
                }
            }

            return false;

            int CountBindings(IEnumerable<Clause> clauses, IEnumerable<Expr> scope) =>
                clauses.SelectMany(clause => clause.Patterns)
                    .Concat(scope.SelectMany(NestedPatterns))
                    .SelectMany(Semantics.Variables)
                    .Count(name => name == suffix);
        }

        private static bool IsPositionedSuffixSegment(BitPatternSegment segment, out string name)
        {
            if (segment.Value is Pattern.Variable variable && segment.Specification is
                {
                    Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BitStringUnit, Size: null
                })
            {
                name = variable.Name;

                return true;
            }

            name = string.Empty;

            return false;
        }
    }
}
