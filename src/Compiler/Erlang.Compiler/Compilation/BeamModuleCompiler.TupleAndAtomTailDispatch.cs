using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleAndAtomTailDispatch(FunctionDefinition function, int functionInfo)
        {
            var clauses = function.Clauses;
            if (clauses.Count < 4 || function.Arity == 0
                || clauses[^1] is not { Guard: null } fallback
                || !HasUnconditionalParameters(fallback)
                || clauses[^2].Patterns[0] is not Erlang.Pattern.Tuple tuple
                || !HasTupleTag(tuple, out Atom tag)
                || !tuple.Items.Skip(1).All(field =>
                    field is Erlang.Pattern.Any or Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || ReadDispatchTail(clauses[^2]) is not { } tupleTail)
            {
                return false;
            }

            var cases = new Dictionary<Atom, EntryCall>();
            foreach (Clause clause in clauses.Take(clauses.Count - 2))
            {
                if (clause.Patterns[0] is not Erlang.Pattern.Literal { Value: Atom key }
                    || ReadDispatchTail(clause) is not { } tail || !cases.TryAdd(key, tail))
                {
                    return false;
                }
            }

            int atomBranch = Label();
            var branches = cases.OrderByDescending(pair => pair.Key,
                    Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
                .Select(pair => (pair.Key, pair.Value, Label: Label())).ToArray();
            int failure = Label();

            owner.Op(IsTuple, LabelTag, atomBranch);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            EmitDispatchTail(tupleTail);

            Mark(atomBranch);
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.Reverse())
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }

            foreach (var branch in branches)
            {
                Mark(branch.Label);
                EmitDispatchTail(branch.Value);
            }

            Mark(failure);
            if (ReadDispatchTail(fallback) is { } tailFallback)
            {
                EmitDispatchTail(tailFallback);
            }
            else
            {
                EmitGeneralFunction(function with { Clauses = [fallback] }, functionInfo);
            }

            return true;
        }

        private EntryCall? ReadDispatchTail(Clause clause)
        {
            if (clause.Guard is not null || clause.Body is not Expr.Call call
                || clause.Patterns.Skip(1).Any(pattern => pattern is not Erlang.Pattern.Variable)
                || !HasUnconditionalParameters(clause with
                {
                    Patterns = clause.Patterns.Skip(1).ToArray()
                }))
            {
                return null;
            }

            var parameters = clause.Patterns.Select(pattern => pattern is Erlang.Pattern.Variable variable
                ? variable.Name
                : VariableScopeNames.Wildcard).ToArray();

            return ReadEntryCall(call, parameters, allowControlTransfer: true) is { Module: null } tail ? tail : null;
        }

        private void EmitDispatchTail(EntryCall tail)
        {
            EmitEntryCallArguments(tail);
            var call = tail.Call;
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, entries[(call.Function, call.Arguments.Count)]);
        }
    }
}
