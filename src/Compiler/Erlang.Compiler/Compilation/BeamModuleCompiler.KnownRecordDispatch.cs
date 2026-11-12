using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitKnownRecordDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Tuple tuple, Pattern.Variable argument],
                  Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call },
                { Guard: null, Body: Expr.Literal { Value: Atom result } } fallback
            ] || tuple.Items is not [Pattern.Literal { Value: Atom tag }, ..]
                || argument.Name == VariableScopeNames.Wildcard || !MatchesDetailArguments(call, argument)
                || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            int field = -1;
            Atom? key = null;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case Pattern.Literal { Value: Atom value } when key is null:
                        field = index;
                        key = value;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    default:
                        return false;
                }
            }

            if (key is null || !HasOnlyLocalCalls(function, 2, (caller, invocation) =>
                invocation.Arguments[0] is Expr.Variable record
                && (invocation.Arguments[1] is Expr.RecordAccess { Value: Expr.Variable checkedRecord } access
                    && checkedRecord.Name == record.Name && access.Name == tag.Name && access.Arity == tuple.Items.Count
                    || caller.Clauses.Any(clause => clause.Body is Expr.Sequence sequence
                        && sequence.Items.Any(item => ReferenceEquals(item, invocation))
                        && clause.Patterns.Any(pattern => pattern is AliasPattern
                        {
                            Left: Pattern.Tuple typed, Right: Pattern.Variable alias
                        } && alias.Name == record.Name && typed.Items.Count == tuple.Items.Count
                          && typed.Items[0] is Pattern.Literal { Value: Atom actual } && actual.Equals(tag))))))
            {
                return false;
            }

            int other = Label();
            owner.EmitTuplePart(0, field, 0);
            owner.EmitAtomTest(0, key.Name, other);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, 1));
            Mark(other);
            owner.EmitConstant(result, 0);
            owner.instructions.WriteByte(Return);
            return true;
        }
    }
}
