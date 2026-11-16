using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Atom Result, (Atom Key, int Register)[] Branches)? ReadTaggedAccumulator(FunctionDefinition function)
    {
        if (function.Arity != 4 || function.Clauses is not [_, _, _, var finish]
            || finish is not { Guard: null, Patterns: [_, Pattern.Variable a, Pattern.Variable b, Pattern.Variable c],
                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom result }, Expr.Variable first,
                    Expr.Variable second, Expr.Variable third] } }
            || !DistinctBindings(a, b, c) || first.Name != a.Name || second.Name != b.Name || third.Name != c.Name
            || finish.Patterns[0] is not (Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }))
        {
            return null;
        }

        var branches = new List<(Atom Key, int Register)>();
        foreach (Clause clause in function.Clauses.Take(3))
        {
            if (clause is not { Guard: null, Patterns: [
                Pattern.List { Items: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom key }, Pattern.Variable value] }],
                    Tail: Pattern.Variable tail },
                Pattern.Variable p1, Pattern.Variable p2, Pattern.Variable p3
            ], Body: Expr.Call { Module: null, DynamicRemote: false,
                Arguments: [Expr.Variable next, Expr.Variable x1, Expr.Variable x2, Expr.Variable x3] } call }
                || call.Function != function.Name || next.Name != tail.Name
                || branches.Any(branch => branch.Key.Equals(key)))
            {
                return null;
            }

            Pattern.Variable[] parameters = [p1, p2, p3];
            Expr.Variable[] arguments = [x1, x2, x3];
            int replaced = Array.FindIndex(parameters, item => item.Name == VariableScopeNames.Wildcard);
            if (replaced < 0 || arguments[replaced].Name != value.Name
                || !DistinctBindings([tail, value, .. parameters.Where((_, index) => index != replaced)])
                || parameters.Where((item, index) => index != replaced && item.Name != arguments[index].Name).Any())
            {
                return null;
            }

            branches.Add((key, replaced + 1));
        }

        return (result, branches.ToArray());
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedAccumulator(FunctionDefinition function)
        {
            if (ReadTaggedAccumulator(function) is not { } flow)
            {
                return false;
            }

            int entry = owner.nextLabel - 1;
            int failure = entry - 1;
            var branches = flow.Branches.Reverse().Select(branch => (branch.Key, branch.Register, Entry: Label())).ToArray();
            int empty = Label();
            owner.Op(IsNonemptyList, LabelTag, empty);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 4);
            owner.Operand(XRegisterTag, 5);
            owner.EmitTupleCheck(4, 2, failure);
            owner.EmitTuplePart(4, 0, 6);
            owner.EmitTuplePart(4, 1, 4);
            owner.Op(SelectValue, XRegisterTag, 6);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.OrderBy(branch => branch.Key, MapKeyOrder))
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Entry);
            }
            foreach (var branch in branches)
            {
                Mark(branch.Entry);
                owner.MoveRegister(XRegisterTag, 4, XRegisterTag, branch.Register);
                owner.MoveToX0(XRegisterTag, 5);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 4);
                owner.Operand(LabelTag, entry);
            }
            Mark(empty);
            owner.Op(IsNil, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(flow.Result.Name)),
                (XRegisterTag, 1), (XRegisterTag, 2), (XRegisterTag, 3));
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
