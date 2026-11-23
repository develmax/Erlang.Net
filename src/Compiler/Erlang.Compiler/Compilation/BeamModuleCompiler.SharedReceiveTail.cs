using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SharedReceiveTail(
            int Slot,
            Expr.Call Body,
            Atom WideTag,
            Atom FirstTag,
            Atom SecondTag
        );

        private SharedReceiveTail? ReadSharedReceiveTail(IGrouping<int, Clause>[] arities)
        {
            if (arities.Length != 2 || arities[0].Key < 2
                || parentFunction is not { Arity: 1 } function
                || !entries.ContainsKey((function.Name, function.Arity)))
            {
                return null;
            }

            if (arities[0].ToArray() is not [var first, var second]
                || arities[1].ToArray() is not [var wide]
                || ReadArm(wide) is not { } wideArm
                || ReadArm(first) is not { } firstArm
                || ReadArm(second) is not { } secondArm
                || firstArm.Slot != wideArm.Slot || secondArm.Slot != wideArm.Slot
                || firstArm.Tag.Equals(secondArm.Tag))
            {
                return null;
            }

            if (TermOrder.Compare(firstArm.Tag, secondArm.Tag, exact: true) > 0)
            {
                (firstArm, secondArm) = (secondArm, firstArm);
            }

            return new(
                wideArm.Slot,
                wideArm.Call,
                wideArm.Tag,
                firstArm.Tag,
                secondArm.Tag
            );

            (int Slot, Expr.Call Call, Atom Tag)? ReadArm(Clause clause)
            {
                if (clause is not { Guard: null, Body: Expr.Call call }
                    || call is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable argument] }
                    || call.Function != function.Name
                    || !variables.TryGetValue(argument.Name, out int slot)
                    || deferredPairMembers.ContainsKey(argument.Name)
                    || OmitUnusedCaseBindings(clause, null, null).Patterns[0] is not Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable key, ..]
                    } tuple || key.Name != argument.Name
                    || tuple.Items.Skip(2).Any(item => item is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
                {
                    return null;
                }

                return (slot, call, tag);
            }
        }

        private void EmitSharedReceiveTail(
            SharedReceiveTail shared,
            IGrouping<int, Clause>[] arities,
            int[] labels,
            (string Name, int Slot)[] joined
        )
        {
            int wide = labels[0];
            int narrow = labels[1];
            int accept = labels[2];
            int retry = labels[^2];

            owner.EmitTupleArityDispatch(retry, [(arities[0].Key, narrow), (arities[1].Key, wide)]);
            Mark(wide);
            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 0);
            owner.Op(ExactEqualTest, LabelTag, retry);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(shared.WideTag);
            JumpTo(accept);

            Mark(narrow);
            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 0);
            owner.EmitAtomSelection(
                1,
                retry,
                shared.FirstTag.Name,
                accept,
                shared.SecondTag.Name,
                accept
            );

            Mark(accept);
            owner.Op(ExactEqualTest, LabelTag, retry);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, shared.Slot);
            owner.instructions.WriteByte(RemoveMessage);
            EmitReceiveBranch(shared.Body, joined, terminal: true);
        }
    }
}
