using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedAppendApply(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable fastName, Pattern.Variable fastArgs, Pattern.Tuple fastTuple],
                    Guard: null, Body: Expr.Call
                    {
                        Function: AutoImportNames.Apply, Arguments:
                            [Expr.Literal { Value: Atom module }, Expr.Variable fastFunction, Expr.Variable fastValues]
                    } fast
                },
                {
                    Patterns: [Pattern.Variable atomName, Pattern.Variable atomArgs, Pattern.Tuple atomTuple],
                    Guard: Expr.Call { Function: AutoImportNames.IsAtom, DynamicRemote: false, Arguments: [Expr.Variable tested] } guard,
                    Body: Expr.Call
                    {
                        Function: AutoImportNames.Apply, Arguments:
                            [
                                Expr.Variable target, Expr.Variable atomFunction,
                                Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Variable left, Right: Expr.Variable right } append
                            ]
                    } atom
                },
                {
                    Patterns: [Pattern.Variable otherName, Pattern.Variable otherArgs, Pattern.Tuple otherTuple],
                    Guard: null, Body: Expr.Call
                    {
                        Arguments:
                            [Expr.List { Items: [Expr.Variable first, Expr.Variable second, Expr.Variable third], Tail: Expr.Variable tail }]
                    } other
                }
                ]
                || !DistinctBindings(fastName, fastArgs)
                || fastName.Name != fastFunction.Name || fastArgs.Name != fastValues.Name
                || atomName.Name != atomFunction.Name || atomArgs.Name != left.Name || tested.Name != target.Name
                || otherName.Name != second.Name || otherArgs.Name != third.Name
                || !owner.ResolvesErlangBif(guard)
                || !TryResolveOrdinaryCallTarget(fast, out string? fastModule) || fastModule != ErlangModuleNames.Module
                || !TryResolveOrdinaryCallTarget(atom, out string? atomModule) || atomModule != ErlangModuleNames.Module
                || !TryResolveOrdinaryCallTarget(other, out string? otherModule) || otherModule is null
                || IsTrappingTailBif(otherModule, other.Function, other.Arguments.Count)
                || !HasTupleTag(fastTuple, out Atom tag) || !HasTupleTag(atomTuple, out Atom atomTag)
                || !HasTupleTag(otherTuple, out Atom otherTag) || !tag.Equals(atomTag) || !tag.Equals(otherTag)
                || fastTuple.Items.Count != atomTuple.Items.Count || fastTuple.Items.Count != otherTuple.Items.Count)
            {
                return false;
            }

            int selector = BindField(atomTuple, target);
            int extra = BindField(atomTuple, right);
            if (selector <= 0 || extra <= 0 || selector == extra
                || fastTuple.Items[selector] is not Pattern.Literal { Value: Atom fastKey }
                || !BindsOnly(
                    atomTuple,
                    atomName,
                    atomArgs,
                    selector,
                    extra
                )
                || !BindsOnly(
                    otherTuple,
                    otherName,
                    otherArgs,
                    selector,
                    extra
                )
                || BindField(otherTuple, first) != selector || BindField(otherTuple, tail) != extra)
            {
                return false;
            }

            for (int field = 1; field < fastTuple.Items.Count; field++)
            {
                if (field != selector && fastTuple.Items[field] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            const int inputName = 0;
            const int inputList = 1;
            const int inputTuple = 2;
            const int selectorRegister = 3;
            const int projectedExtra = inputTuple;
            const int savedSelector = 0;
            const int savedName = 1;
            const int frame = 2;
            int nonliteral = Label();
            int listTail = Label();

            owner.EmitRecordTest(
                functionInfo,
                XRegisterTag,
                inputTuple,
                tag,
                fastTuple.Items.Count
            );
            owner.EmitTuplePart(inputTuple, selector, selectorRegister);
            owner.EmitAtomTest(selectorRegister, fastKey.Name, nonliteral);
            MoveX(inputList, inputTuple);
            MoveX(inputName, inputList);
            owner.EmitConstant(module, 0);
            owner.EmitCallLine(fast, location);
            int apply = owner.Import(ErlangModuleNames.Module, AutoImportNames.Apply, fast.Arguments.Count);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, fast.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, apply);

            Mark(nonliteral);
            owner.EmitTuplePart(inputTuple, extra, projectedExtra);
            owner.Op(IsAtom, LabelTag, listTail);
            owner.Operand(XRegisterTag, selectorRegister);

            // Only the branch with a non-tail call has values live across a call.
            EmitFrameAllocation(selectorRegister + 1, frame);
            owner.MoveRegister(
                XRegisterTag,
                selectorRegister,
                YRegisterTag,
                savedSelector
            );
            owner.MoveRegister(
                XRegisterTag,
                inputName,
                YRegisterTag,
                savedName
            );
            MoveX(inputList, 0);
            MoveX(projectedExtra, 1);
            var appendLocation = location;
            owner.EmitArithmeticLine(append.SourceLine, ref appendLocation);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            owner.MoveRegister(
                YRegisterTag,
                savedName,
                XRegisterTag,
                1
            );
            MoveX(0, 2);
            owner.MoveRegister(
                YRegisterTag,
                savedSelector,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(atom, appendLocation);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, atom.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, apply);
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(listTail);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, selectorRegister + 1);
            PutCell(inputList, projectedExtra, inputList);
            PutCell(inputName, inputList, inputName);
            PutCell(selectorRegister, inputName, inputName);
            owner.EmitCallLine(other, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, other.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(otherModule, other.Function, other.Arguments.Count));

            return true;

            void MoveX(int source, int destination) =>
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    XRegisterTag,
                    destination
                );

            void PutCell(int head, int tailRegister, int destination)
            {
                owner.Op(PutList, XRegisterTag, head);
                owner.Operand(XRegisterTag, tailRegister);
                owner.Operand(XRegisterTag, destination);
            }
        }

        private static int BindField(Pattern.Tuple tuple, Expr.Variable variable)
        {
            for (int field = 1; field < tuple.Items.Count; field++)
            {
                if (tuple.Items[field] is Pattern.Variable binding && binding.Name == variable.Name)
                {
                    return field;
                }
            }

            return -1;
        }

        private static bool BindsOnly(
            Pattern.Tuple tuple,
            Pattern.Variable name,
            Pattern.Variable args,
            int selector,
            int extra
        )
        {
            if (tuple.Items[selector] is not Pattern.Variable selected || tuple.Items[extra] is not Pattern.Variable rest
                || !DistinctBindings(
                    name,
                    args,
                    selected,
                    rest
                ))
            {
                return false;
            }

            for (int field = 1; field < tuple.Items.Count; field++)
            {
                if (field != selector && field != extra && tuple.Items[field] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            return true;
        }
    }
}
