using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitBoundTupleBitwiseGuard(Expr.Binary guard, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.receiveScratchRegisters is not null || recursiveClosure is not null
                || ReadTupleArityGuard(guard.Left) is not { Value: Expr.Variable tuple, Arity: >= 2 } checkedTuple
                || !variables.TryGetValue(tuple.Name, out int slot)
                || guard.Left is not Expr.Binary check
                || (check.Left as Expr.Call ?? check.Right as Expr.Call) is not { } size
                || !owner.ResolvesErlangBif(size)
                || ReadZeroMask(guard.Right) is not { } test
                || !IsTupleElementFold(test.Input, checkedTuple.Arity, tuple.Name))
            {
                return false;
            }

            const int accumulator = 0;
            const int field = 1;
            int union = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseOr, 2);
            Load(slot);
            EmitTupleArityGuard(accumulator, checkedTuple.Arity, failure);
            owner.EmitTuplePart(
                slot,
                0,
                accumulator,
                YRegisterTag
            );
            for (int index = 1; index < checkedTuple.Arity; index++)
            {
                owner.EmitTuplePart(
                    slot,
                    index,
                    field,
                    YRegisterTag
                );
                owner.Op(GcBifTwo, LabelTag, failure);
                owner.Operand(BeamWire.UnsignedTag, field + 1);
                owner.Operand(BeamWire.UnsignedTag, union);
                if (index == 1)
                {
                    owner.Operand(XRegisterTag, accumulator);
                }
                else
                {
                    owner.EmitTypedRegister(accumulator, IntegerTypeBits);
                }

                owner.Operand(XRegisterTag, field);
                owner.Operand(XRegisterTag, accumulator);
            }

            // The reduction now proves the mask operand is an integer.
            // Neither scratch value needs a Y slot across this guard.
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, accumulator + 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2));
            owner.EmitTypedRegister(accumulator, IntegerTypeBits);
            owner.LiteralOperand(test.Mask);
            owner.Operand(XRegisterTag, accumulator);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.EmitTypedRegister(accumulator, IntegerTypeBits);
            owner.Operand(BeamWire.IntegerTag, 0);

            return true;

            bool IsTupleElementFold(Expr input, int count, string tupleName)
            {
                while (count > 1)
                {
                    if (input is not Expr.Binary { Operator: ErlangOperators.BitwiseOr } fold
                        || !IsCheckedElement(fold.Right, count, tupleName))
                    {
                        return false;
                    }

                    count--;
                    input = fold.Left;
                }

                return IsCheckedElement(input, count, tupleName);
            }

            bool IsCheckedElement(Expr input, int index, string tupleName) => input is Expr.Call
            {
                Arguments: [
                    Expr.Literal { Value: Integer position },
                    Expr.Variable carrier
                ]
            } read && IsElementGuardCall(read) && owner.ResolvesErlangBif(read)
                && carrier.Name == tupleName && position.Value == index;
        }
    }
}
