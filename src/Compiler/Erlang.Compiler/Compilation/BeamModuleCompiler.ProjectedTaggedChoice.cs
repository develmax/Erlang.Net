using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProjectedTaggedChoice(
            Expr.Case choice,
            Pattern.Tuple tuple,
            Atom tag,
            int failure,
            bool arityKnown,
            bool tagKnown,
            int? payloadRegister,
            bool validateOnly
        )
        {
            if (tuple.Items is not [_, Pattern.Variable selector]
                || selector.Name == VariableScopeNames.Wildcard
                || variables.ContainsKey(selector.Name) || deferredPairMembers.ContainsKey(selector.Name)
                || choice.Value is not Expr.Variable selected || selected.Name != selector.Name
                || choice.Clauses is not [var first, var second]
                || first is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom valueTag }, Pattern.Variable value] }],
                    Body: Expr.Variable returned
                }
                || returned.Name != value.Name || !IsFresh(value)
                || second is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom callTag }, Pattern.Variable argument] }],
                    Body: Expr.Call call
                }
                || valueTag.Name == callTag.Name || !IsFresh(argument)
                || call.Arguments is not [Expr.Variable passed] || passed.Name != argument.Name
                || ReadEntryCall(call, [argument.Name]) is not { } target)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            if (!tagKnown)
            {
                EmitCaseTupleTest(
                    tuple.Items.Count,
                    tag,
                    failure,
                    arityKnown
                );
            }

            if (payloadRegister is int projected)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    projected,
                    XRegisterTag,
                    0
                );
            }
            else
            {
                owner.EmitTuplePart(0, 1, 0);
            }

            int payloadSlot = Slot();
            int rejected = Label();
            int projectedArm = Label();
            int callArm = Label();
            int join = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                rejected
            );
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, payloadSlot);
            var cases = new[] { (Tag: valueTag, Label: projectedArm), (Tag: callTag, Label: callArm) };
            Array.Sort(cases, (left, right) => TermOrder.Compare(left.Tag, right.Tag, exact: true));
            EmitAritySelect(1, cases.Length, rejected);
            foreach (var (choiceTag, label) in cases)
            {
                owner.LiteralOperand(choiceTag);
                owner.Operand(LabelTag, label);
            }

            foreach (var (_, label) in cases.Reverse())
            {
                Mark(label);
                Load(payloadSlot);
                if (label == callArm)
                {
                    owner.InitStackSlot(payloadSlot);
                    EmitEntryCall(target);
                }

                JumpTo(join);
            }

            EmitBooleanCaseFailure(rejected, CaseSourceLocation(choice));
            Mark(join);

            return true;

            bool IsFresh(Pattern.Variable variable) => variable.Name != VariableScopeNames.Wildcard
                && !variables.ContainsKey(variable.Name) && !deferredPairMembers.ContainsKey(variable.Name);
        }
    }
}
