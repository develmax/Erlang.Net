using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLiteralShortcutBoolean(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Literal { Value: Atom shortcut }], Guard: null,
                    Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }
                },
                {
                    Patterns: [Pattern.Variable input],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.ExactEqual, Left: Expr.Variable tested,
                        Right: Expr.Literal literal
                    },
                    Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }
                },
                {
                    Patterns: [Pattern.Variable { }], Guard: null,
                    Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }
                }
                ] || input.Name == VariableScopeNames.Wildcard || tested.Name != input.Name
                || literal.Value is Atom)
            {
                return false;
            }

            int compare = Label();
            owner.Op(ExactEqualTest, LabelTag, compare);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(shortcut);
            owner.EmitConstant(Term.A(ErlangBooleanAtoms.True), 0);
            owner.instructions.WriteByte(Return);

            Mark(compare);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(literal.Value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
