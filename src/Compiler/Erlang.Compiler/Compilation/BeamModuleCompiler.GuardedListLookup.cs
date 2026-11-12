using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedListLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [var empty, Pattern.Variable defaultValue], Body: Expr.Variable missing },
                { Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom key }, Pattern.Variable value
                ] }], Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard } },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Guard: Expr.Binary { Operator: ErlangOperators.LessOrEqual, Left: Expr.Variable compared,
                        Right: Expr.Literal { Value: Integer limit } }, Body: Expr.Literal replacement },
                { Guard: null, Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom sameKey }, Pattern.Variable valueAgain
                ] }], Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard } },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Variable unchanged },
                { Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable ignored], Tail: Pattern.Variable remaining },
                    Pattern.Variable forwarded],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable tail, Expr.Variable fallback] } recurse }
            ] || !IsEmptyListPattern(empty) || !key.Equals(sameKey)
                || defaultValue.Name == VariableScopeNames.Wildcard || defaultValue.Name != missing.Name
                || value.Name == VariableScopeNames.Wildcard || value.Name != compared.Name
                || valueAgain.Name == VariableScopeNames.Wildcard || valueAgain.Name != unchanged.Name
                || !DistinctBindings(remaining, forwarded)
                || (ignored.Name != VariableScopeNames.Wildcard && !DistinctBindings(ignored, remaining, forwarded))
                || recurse.Function != function.Name || tail.Name != remaining.Name || fallback.Name != forwarded.Name
                || !entries.TryGetValue((function.Name, 2), out int entry))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int original = Label();
            int skip = Label();
            int nil = Label();
            owner.EmitNonemptyList(0, nil);
            owner.EmitListParts(0, XRegisterTag, 2, XRegisterTag, 0);
            EmitDetailRecordTest(2, 2, key, skip);
            owner.EmitTuplePart(2, 1, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, original);
            owner.LiteralOperand(limit);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(replacement.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(original);
            owner.instructions.WriteByte(Return);
            Mark(skip);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            Mark(nil);
            owner.EmitNilTest(XRegisterTag, 0, failure);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
