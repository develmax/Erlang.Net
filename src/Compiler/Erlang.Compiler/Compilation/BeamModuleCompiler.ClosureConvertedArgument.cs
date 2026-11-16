using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Expr.Call Remote, Expr.Call Local)? ReadRemoteToLocalClosure(Clause clause) =>
        clause is {
            Guard: null, Patterns: [Pattern.Variable input],
            Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable supplied] } remote
            ] } local
        } && input.Name != VariableScopeNames.Wildcard && input.Name == supplied.Name ? (remote, local) : null;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosureWithConvertedArgument(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable input],
                Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Fun { SelfName: null, Clauses: [var closureBody] } closure,
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable supplied] } convert
                    ] } collection
                ] } apply
            }] || input.Name == VariableScopeNames.Wildcard || supplied.Name != input.Name
                || ReadRemoteToLocalClosure(closureBody) is not { } chain
                || !entries.ContainsKey((chain.Local.Function, 1))
                || !entries.TryGetValue((convert.Function, 1), out int target)
                || owner.ResolvesErlangBif(apply) || owner.ResolvesErlangBif(collection) || owner.ResolvesErlangBif(chain.Remote))
            {
                return false;
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.EmitClosureHeap(0, 1, 1);
            owner.Op(MakeFun, BeamWire.UnsignedTag, owner.closures.Count);
            owner.Operand(YRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.closures.Add(new(
                $"-{function.Name}/{function.Arity}-fun-0-",
                closureBody,
                closure.ClauseLine == 0 ? location : new(closure.ClauseFile, closure.ClauseLine),
                0,
                null
            ));
            var previous = location;
            EmitWindowLocal(convert, target, ref previous);
            EmitWindowCall(collection, ref previous);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            Load(0);
            EmitConfigurationTail(apply, 1, previous);

            return true;
        }

        private bool TryEmitSharedTuplePrefix(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Integer prefix }, Pattern.Literal { Value: Integer first }
                ] }], Body: var firstResult },
                { Guard: null, Patterns: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Integer otherPrefix }, Pattern.Literal { Value: Integer second }
                ] }], Body: var secondResult }
            ] || !prefix.Equals(otherPrefix) || first.Equals(second)
                || !AttributeLiteral.TryNormalizeConstant(firstResult, out Term firstValue)
                || !AttributeLiteral.TryNormalizeConstant(secondResult, out Term secondValue))
            {
                return false;
            }

            if (first.Value > second.Value)
            {
                (first, second) = (second, first);
                (firstValue, secondValue) = (secondValue, firstValue);
            }

            int failure = owner.nextLabel - 2;
            int high = Label();
            int low = Label();
            owner.EmitTupleCheck(0, 2, failure);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(prefix);
            owner.EmitTuplePart(0, 1, 1);
            EmitChildDispatch(1, failure, (first, low), (second, high));
            Mark(high);
            owner.EmitConstant(secondValue, 0);
            owner.instructions.WriteByte(Return);
            Mark(low);
            owner.EmitConstant(firstValue, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitComputedScalar(FunctionDefinition function)
        {
            if (function.Clauses is [{ Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Unary {
                Operator: ErlangOperators.Not, Operand: Expr.Call { Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Variable argument] } predicate
            } }] && input.Name != VariableScopeNames.Wildcard && argument.Name == input.Name
                && !owner.ResolvesErlangBif(predicate))
            {
                owner.Op(Allocate, BeamWire.UnsignedTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 1);
                var previous = location;
                EmitWindowCall(predicate, ref previous);
                owner.Op(BifOne, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Not, 1));
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(0);

                return true;
            }

            if (function.Clauses is not [{ Guard: null, Patterns: [], Body: Expr.Binary {
                Operator: ErlangOperators.Plus, Left: Expr.Literal { Value: Integer offset },
                Right: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Literal bound] } call
            } }] || owner.ResolvesErlangBif(call))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitConstant(bound.Value, 0);
            var callLocation = location;
            EmitWindowCall(call, ref callLocation);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(offset);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);

            return true;
        }
    }
}
