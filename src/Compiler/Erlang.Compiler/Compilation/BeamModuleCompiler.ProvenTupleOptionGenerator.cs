using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TupleOptionGenerator(
        Expr.Call Call,
        string Capture,
        Atom Option,
        int OptionField,
        int ScalarField
    );

    private static TupleOptionGenerator? ReadTupleOptionGenerator(Pattern item, Expr mapped)
    {
        if (item is not Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }
            || first.Name == second.Name || first.Name == VariableScopeNames.Wildcard || second.Name == VariableScopeNames.Wildcard
            || mapped is not Expr.Call
            {
                DynamicRemote: false,
                Arguments:
                [
                    Expr.Variable capture,
                    Expr.Variable scalar,
                    Expr.List { Items: [Expr.Tuple { Items: [Expr.Literal { Value: Atom option }, Expr.Variable field] }], Tail: null }
                ]
            } call
            || capture.Name == first.Name || capture.Name == second.Name
            || !(scalar.Name == first.Name && field.Name == second.Name
                || scalar.Name == second.Name && field.Name == first.Name))
        {
            return null;
        }

        int optionField = field.Name == first.Name ? 0 : 1;

        return new TupleOptionGenerator(
            call,
            capture.Name,
            option,
            optionField,
            1 - optionField
        );
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProvenTupleOptionGenerator(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || !function.HasProvenRecursiveTupleInput
                || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [var item] }, Pattern.Variable capture],
                    Body: Expr.List { Items: [var mapped] }
                }
                || ReadTupleOptionGenerator(item, mapped) is not { } plan || plan.Capture != capture.Name
                || !TryResolveOrdinaryCallTarget(plan.Call, out string? module)
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 3 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );

            owner.EmitTuplePart(0, plan.OptionField, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.LiteralOperand(plan.Option);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            owner.EmitTuplePart(0, plan.ScalarField, 1);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(plan.Call, location);
            Call(module, plan.Call.Function, 3);

            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitRecursiveCons(entry, 2);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}
