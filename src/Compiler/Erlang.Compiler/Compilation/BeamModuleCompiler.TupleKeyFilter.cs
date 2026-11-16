using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Expr.Variable Source, Expr.ListComprehension Comprehension, Atom[] Keys)? ReadTupleKeyFilter(
        FunctionDefinition function
    )
    {
        if (function.Clauses is not [{
            Guard: null, Patterns: [Pattern.Variable input],
            Body: Expr.ListComprehension
            {
                Items: [Expr.Variable returned],
                Qualifiers: [
                    ComprehensionQualifier.Generator { Strict: false, Pattern: AliasPattern alias, Source: Expr.Variable source },
                    ComprehensionQualifier.Filter predicate
                ]
            } comprehension
        }] || source.Name != input.Name)
        {
            return null;
        }

        Pattern shape = alias.Left;
        Pattern binding = alias.Right;
        if (shape is Pattern.Variable)
        {
            (shape, binding) = (binding, shape);
        }
        if (binding is not Pattern.Variable tuple || returned.Name != tuple.Name
            || shape is not Pattern.Tuple { Items: [
                Pattern.Variable key, Pattern.Variable { Name: VariableScopeNames.Wildcard }
            ] } || !DistinctBindings(input, tuple, key))
        {
            return null;
        }

        var keys = new HashSet<Atom>();
        if (!ReadKeys(predicate.Expression) || keys.Count < 2)
        {
            return null;
        }

        return (source, comprehension, keys.OrderBy(atom => atom,
            Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))).ToArray());

        bool ReadKeys(Expr expression) => expression switch
        {
            Expr.Binary { Operator: ErlangOperators.OrElse } union => ReadKeys(union.Left) && ReadKeys(union.Right),
            Expr.Binary { Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable tested, Right: Expr.Literal { Value: Atom value } }
                when tested.Name == key.Name => keys.Add(value),
            _ => false
        };
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleKeyFilter(FunctionDefinition function)
        {
            if (ReadTupleKeyFilter(function) is not { } flow)
            {
                return false;
            }

            int entry = owner.nextLabel - 1;
            int keep = Label();
            int skip = Label();
            int empty = Label();
            int invalid = Label();
            owner.Op(IsNonemptyList, LabelTag, empty);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleCheck(1, 2, skip);
            owner.EmitTuplePart(1, 0, 2);
            owner.Op(SelectValue, XRegisterTag, 2);
            owner.Operand(LabelTag, skip);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, flow.Keys.Length * 2);
            foreach (Atom key in flow.Keys)
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, keep);
            }
            Mark(keep);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.EmitFieldCall(1, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(skip);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            Mark(empty);
            owner.Op(IsNil, LabelTag, invalid);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(invalid);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(ErlangErrorReasons.BadGenerator)), (XRegisterTag, 0));
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));

            return true;
        }
    }
}
