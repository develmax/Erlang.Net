using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedPairedGuardValue(
            Expr item,
            IReadOnlyList<Expr> alternatives,
            int failure
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.receiveScratchRegisters is not null || recursiveClosure is not null
                || item is not Expr.Binary
                {
                    Operator: ErlangOperators.AndAlso,
                    SourceLine: 0,
                    Left: var left,
                    Right: var right
                }
                || ReadPairedGuardEquality(right) is not { } last
                || ReadPairedGuardLeft(left) is not { } first || first.Slot == last.Slot
                || !alternatives.OfType<Expr.Binary>().Any(MatchesOtherOperand))
            {
                return false;
            }

            if (first.Mask is { } mask)
            {
                owner.Op(GcBifTwo, LabelTag, failure);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2));
                owner.Operand(YRegisterTag, first.Slot);
                owner.LiteralOperand(mask);
                owner.Operand(XRegisterTag, 0);
                EmitGuardValueEquality(
                    0,
                    Term.I(0),
                    0,
                    typedInteger: true
                );
            }
            else
            {
                EmitGuardValueEquality(
                    first.Slot,
                    first.Equality!,
                    0,
                    stack: true
                );
            }

            EmitGuardValueEquality(
                last.Slot,
                last.Value,
                1,
                stack: true
            );
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.And, 2));
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.EmitTypedRegister(1, AtomTypeBits);
            owner.Operand(XRegisterTag, 0);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));

            return true;

            bool MatchesOtherOperand(Expr.Binary alternative) => alternative is
            {
                Operator: ErlangOperators.AndAlso,
                SourceLine: 0,
                Left: var otherLeft,
                Right: var otherRight
            }
                && ReadPairedGuardEquality(otherRight) is { } otherLast
                && otherLast.Slot == last.Slot && otherLast.Value.Equals(last.Value)
                && ReadPairedGuardLeft(otherLeft) is { } otherFirst && otherFirst.Slot == first.Slot
                && (otherFirst.Mask is null) != (first.Mask is null);
        }

        private (int Slot, Term Value)? ReadPairedGuardEquality(Expr expression) => expression is Expr.Binary
        {
            Operator: ErlangOperators.ExactEqual,
            Left: Expr.Variable variable,
            Right: var right
        } && variables.TryGetValue(variable.Name, out int slot)
            && AttributeLiteral.TryNormalizeConstant(right, out Term constant) && constant is Atom or Integer
            ? (slot, constant)
            : null;

        private (int Slot, Integer? Mask, Term? Equality)? ReadPairedGuardLeft(Expr expression)
        {
            if (ReadPairedGuardEquality(expression) is { Value: Integer } equality)
            {
                return (equality.Slot, null, equality.Value);
            }

            if (ReadZeroMask(expression) is { Input: Expr.Variable variable } mask
                && variables.TryGetValue(variable.Name, out int slot))
            {
                return (slot, mask.Mask, null);
            }

            return null;
        }

        private void EmitGuardValueEquality(
            int input,
            Term value,
            int output,
            bool stack = false,
            bool typedInteger = false
        )
        {
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
            if (typedInteger)
            {
                owner.EmitTypedRegister(input, IntegerTypeBits);
            }
            else
            {
                owner.Operand(stack ? YRegisterTag : XRegisterTag, input);
            }

            owner.LiteralOperand(value);
            owner.Operand(XRegisterTag, output);
        }
    }
}
