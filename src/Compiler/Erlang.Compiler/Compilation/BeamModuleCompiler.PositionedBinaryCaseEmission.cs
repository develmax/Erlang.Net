using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ModuleDefinition? PositionedCaseSourceModule => sourceModule;

    private sealed partial class GeneralEmitter
    {
        private sealed record PositionedCaseInput(
            IReadOnlyList<PositionedBinaryCasePlan.Arm> Arms,
            int Cursor,
            int Position,
            int Padding
        );

        private bool TryEmitPositionedCaseOwner(
            Expr.Case? outerCase,
            Clause outerArm,
            int input,
            int nextArm,
            out Expr remainder
        )
        {
            remainder = outerArm.Body;
            if (outerCase is null || parentFunction is null || owner.PositionedCaseSourceModule is not { } module
                || owner.receiveScratchRegisters is not null
                || PositionedBinaryCasePlan.Read(
                    module,
                    parentFunction,
                    outerCase,
                    outerArm
                ) is not { } plan
                || variables.ContainsKey(plan.Suffix)
                || deferredPairMembers.ContainsKey(plan.Suffix)
                || plan.Padding.Pattern is not Pattern.Variable pad
                || variables.ContainsKey(pad.Name)
                || deferredPairMembers.ContainsKey(pad.Name))
            {
                return false;
            }

            // These statements are discarded in the outer body. The pair may still be read
            // later, so perform its normal match after emitting the nested case result.
            var sequence = (Expr.Sequence)outerArm.Body;
            Expr[] rest = sequence.Items.Skip(2).ToArray();
            var continuation = rest.Prepend(plan.Result with { Value = new Expr.Literal(Nil.Value) });
            try
            {
                if (ClosureCaptures.ReadBranchExports(
                    plan.Selector.Clauses,
                    variables.Keys.Concat(deferredPairMembers.Keys),
                    continuation
                ).Length != 0)
                {
                    return false;
                }
            }
            catch (NotSupportedException)
            {
                return false;
            }

            int cursor = Slot();
            int position = Slot();
            var segments = plan.Producer.Segments.ToArray();
            var wildcard = new Pattern.Variable(VariableScopeNames.Wildcard);
            segments[^1] = segments[^1] with
            {
                Value = wildcard,
                Specification = segments[^1].Specification with { Value = new Expr.Variable(wildcard.Name) }
            };
            EmitFixedBitPattern(
                plan.Producer with { Segments = segments },
                input,
                nextArm,
                positionedContext: cursor
            );

            int padding = EmitPositionedBitSizeMask(
                plan.Size,
                plan.Mask,
                plan.Padding.Value is Expr.Binary mask ? mask.SourceLine : plan.Padding.SourceLine,
                cursor
            );
            EmitCaseClauses(
                plan.Selector.Clauses,
                input: null,
                caseFailureLocation: CaseSourceLocation(plan.Selector),
                positionedInput: new PositionedCaseInput(
                    plan.Arms,
                    cursor,
                    position,
                    padding
                )
            );
            MatchValue(plan.Result, Save(), returnValue: false);
            remainder = rest is [var result] ? result : sequence with { Items = rest };

            return true;
        }

        private void EmitPositionedCaseDiscriminator(
            (PositionedBinaryCasePlan.Arm Arm, int Label)[] targets,
            int consumed,
            int cursor,
            int failure
        )
        {
            if (targets.Length == 0)
            {
                JumpTo(failure);

                return;
            }

            if (targets is [var leaf] && leaf.Arm.PrefixWidth == consumed)
            {
                JumpTo(leaf.Label);

                return;
            }

            // Every remaining prefix has a bit at this depth. A selected prefix cannot be an
            // ancestor of another arm, so no cursor reset is needed between discriminator edges.
            var zero = targets.Where(target => !PositionedCaseBit(target.Arm, consumed)).ToArray();
            var one = targets.Where(target => PositionedCaseBit(target.Arm, consumed)).ToArray();
            int falseTarget = one.Length == 0 ? failure : Label();
            Load(cursor);
            EmitBitMatchHeader(
                0,
                failure,
                9,
                1,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                new BitSegment(new Expr.Literal(Term.I(0)), null),
                1,
                1,
                XRegisterTag,
                1
            );
            owner.Op(ExactEqualTest, LabelTag, falseTarget);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(Term.I(0));
            EmitPositionedCaseDiscriminator(
                zero,
                consumed + 1,
                cursor,
                failure
            );
            if (one.Length != 0)
            {
                Mark(falseTarget);
                EmitPositionedCaseDiscriminator(
                    one,
                    consumed + 1,
                    cursor,
                    failure
                );
            }
        }

        private static bool PositionedCaseBit(PositionedBinaryCasePlan.Arm arm, int consumed) =>
            !((arm.Prefix >> (arm.PrefixWidth - consumed - 1)) & BigInteger.One).IsZero;
    }
}
