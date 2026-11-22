using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedTupleGuardAlternatives(Expr.GuardAlternatives guard, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.receiveScratchRegisters is not null || recursiveClosure is not null
                || guard.Items.Count is not (2 or 3)
                || guard.Items[^2] is not Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } tupleOption
                || guard.Items[^1] is not Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } literalOption)
            {
                return false;
            }

            bool tupleFirst = ReadZeroMask(tupleOption.Left) is null;
            bool literalFirst = ReadZeroMask(literalOption.Left) is null;
            Expr firstScalar = tupleFirst ? tupleOption.Right : tupleOption.Left;
            Expr secondScalar = literalFirst ? literalOption.Right : literalOption.Left;
            Expr literal = literalFirst ? literalOption.Left : literalOption.Right;
            if (ReadZeroMask(firstScalar) is not { Input: Expr.Variable scalar } portMask
                || ReadZeroMask(secondScalar) is not { Input: Expr.Variable repeated } repeatedMask
                || scalar.Name != repeated.Name || !portMask.Mask.Equals(repeatedMask.Mask)
                || !variables.TryGetValue(scalar.Name, out int scalarSlot) || deferredPairMembers.ContainsKey(scalar.Name)
                || literal is not Expr.Binary { Left: Expr.Variable address }
                || ReadPairedGuardEquality(literal) is not { } comparison || comparison.Slot == scalarSlot
                || deferredPairMembers.ContainsKey(address.Name)
                || ReadProtectedAddressFold(tupleFirst ? tupleOption.Left : tupleOption.Right, address.Name) is not { } tuple
                || guard.Items.Count == 3 && !IsMapAlternative(guard.Items[0], address.Name))
            {
                return false;
            }

            if (guard.Items.Count == 3)
            {
                int check = Label();
                owner.Op(IsMap, LabelTag, check);
                owner.Operand(YRegisterTag, comparison.Slot);
                int accepted = Label();
                JumpTo(accepted);
                Mark(check);
                EmitChecks(accepted);
                Mark(accepted);
            }
            else
            {
                int accepted = Label();
                EmitChecks(accepted);
                Mark(accepted);
            }

            return true;

            bool IsMapAlternative(Expr expression, string name) => expression is Expr.Call
            {
                Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                Function: AutoImportNames.IsMap, Arguments: [Expr.Variable tested]
            } call && tested.Name == name && owner.ResolvesErlangBif(call);

            void EmitChecks(int accepted)
            {
                int wrongArity = Label();
                int tupleDone = Label();
                int firstBad = Label();
                int firstDone = Label();
                int secondBad = Label();
                int secondDone = Label();
                int tupleResult = tupleFirst ? 0 : 1;
                int field = tupleResult + 1;
                if (!tupleFirst)
                {
                    EmitScalarMask(0, firstBad);
                }

                owner.Op(IsTuple, LabelTag, firstBad);
                owner.Operand(YRegisterTag, comparison.Slot);
                owner.Op(TestArity, LabelTag, wrongArity);
                owner.Operand(YRegisterTag, comparison.Slot);
                owner.Operand(BeamWire.UnsignedTag, tuple.Arity);
                owner.EmitTuplePart(
                    comparison.Slot,
                    0,
                    tupleResult,
                    YRegisterTag
                );
                int union = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseOr, 2);
                for (int index = 1; index < tuple.Arity; index++)
                {
                    owner.EmitTuplePart(
                        comparison.Slot,
                        index,
                        field,
                        YRegisterTag
                    );
                    BeginBif(
                        firstBad,
                        field + 1,
                        union
                    );
                    if (index == 1)
                    {
                        owner.Operand(XRegisterTag, tupleResult);
                    }
                    else
                    {
                        owner.EmitTypedRegister(tupleResult, IntegerTypeBits);
                    }

                    owner.Operand(XRegisterTag, field);
                    owner.Operand(XRegisterTag, tupleResult);
                }

                if (tuple.At is Expr.Binary { SourceLine: > 0 } at)
                {
                    var source = new BeamSourceLocation(location.File, at.SourceLine);
                    if (source != owner.lastEmittedLine)
                    {
                        owner.EmitLine(source);
                    }
                }

                EmitMask(
                    0,
                    tupleResult,
                    tupleResult,
                    tuple.Mask,
                    typed: true
                );
                JumpTo(tupleDone);
                Mark(wrongArity);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), tupleResult);
                Mark(tupleDone);
                if (tupleFirst)
                {
                    EmitScalarMask(1, firstBad);
                }

                And(0, 1);
                JumpTo(firstDone);
                Mark(firstBad);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), 0);
                Mark(firstDone);

                if (literalFirst)
                {
                    EmitLiteralEquality(1);
                    EmitScalarMask(2, secondBad);
                }
                else
                {
                    EmitScalarMask(1, secondBad);
                    EmitLiteralEquality(2);
                }

                And(1, 2);
                JumpTo(secondDone);
                Mark(secondBad);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), 1);
                Mark(secondDone);

                owner.Op(ExactEqualTest, LabelTag, accepted);
                owner.EmitTypedRegister(0, AtomTypeBits);
                owner.LiteralOperand(Term.A(ErlangBooleanAtoms.False));
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.EmitTypedRegister(1, AtomTypeBits);
                owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
            }

            void EmitLiteralEquality(int destination) =>
                EmitGuardValueEquality(
                    comparison.Slot,
                    comparison.Value,
                    destination,
                    stack: true
                );

            void EmitScalarMask(int result, int rejected) => EmitMask(
                rejected,
                result,
                scalarSlot,
                portMask.Mask,
                typed: false
            );

            void EmitMask(
                int rejected,
                int result,
                int input,
                Integer mask,
                bool typed
            )
            {
                BeginBif(
                    rejected,
                    typed ? result + 1 : result,
                    owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2)
                );
                if (typed)
                {
                    owner.EmitTypedRegister(input, IntegerTypeBits);
                }
                else
                {
                    owner.Operand(YRegisterTag, input);
                }

                owner.LiteralOperand(mask);
                owner.Operand(XRegisterTag, result);
                EmitGuardValueEquality(
                    result,
                    Term.I(0),
                    result,
                    typedInteger: true
                );
            }

            void BeginBif(int rejected, int live, int import)
            {
                owner.Op(GcBifTwo, LabelTag, rejected);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, import);
            }

            void And(int left, int right)
            {
                owner.Op(BifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.And, 2));
                owner.EmitTypedRegister(left, AtomTypeBits);
                owner.EmitTypedRegister(right, AtomTypeBits);
                owner.Operand(XRegisterTag, left);
            }
        }
    }
}
