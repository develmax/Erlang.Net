using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedTupleFieldsTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Call call } clause]
                || !TryResolveOrdinaryCallTarget(call, out string? module))
            {
                return false;
            }

            int source = -1;
            Pattern.Tuple? tuple = null;
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable)
                {
                    continue;
                }

                if (tuple is not null || clause.Patterns[index] is not Pattern.Tuple candidate)
                {
                    return false;
                }

                source = index;
                tuple = candidate;
            }

            if (tuple is null || !HasTupleTag(tuple, out Atom tag)
                || tuple.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable))
            {
                return false;
            }

            var parameters = clause.Patterns.OfType<Pattern.Variable>().Concat(tuple.Items.OfType<Pattern.Variable>())
                .Where(variable => variable.Name != VariableScopeNames.Wildcard).ToArray();
            if (!DistinctBindings(parameters)
                || call.Arguments.Count == 0 || call.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            var fields = new int[call.Arguments.Count];
            var members = tuple.Items.ToArray();
            for (int index = 0; index < fields.Length; index++)
            {
                if (call.Arguments[index] is not Expr.Variable { Name: not VariableScopeNames.Wildcard } variable)
                {
                    return false;
                }

                fields[index] = Array.FindIndex(members, item => item is Pattern.Variable field && field.Name == variable.Name);
                if (fields[index] < 0 || index == source && index != fields.Length - 1)
                {
                    return false;
                }
            }

            EmitDetailRecordTest(source, tuple.Items.Count, tag, owner.nextLabel - 2);
            for (int index = 0; index < fields.Length; index++)
            {
                owner.EmitTuplePart(source, fields[index], index);
            }

            EmitRegisterTailTarget(call with { Module = module }, fields.Length);
            return true;
        }

        private bool TryEmitTaggedTupleAndConstantsTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null,
                Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } parameter],
                Body: Expr.Call { DynamicRemote: false, Arguments: [
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value] }, _, ..] } call }]
                || value.Name != parameter.Name)
            {
                return false;
            }

            Expr.Call target = owner.ResolveImportedCall(call, entries);
            if (target.Module is null or ErlangModuleNames.Module)
            {
                return false;
            }

            var constants = new List<(int Destination, Term Value)>();
            for (int index = 1; index < target.Arguments.Count; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(target.Arguments[index], out Term constant))
                {
                    return false;
                }

                constants.Add((index, constant));
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.EmitIndependentCallMoves(constants);
            EmitRegisterTailTarget(target, target.Arguments.Count);
            return true;
        }

        private bool TryEmitTupleElementAtomDispatchTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null,
                Patterns: [Pattern.Variable parameter],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable binding,
                        Value: Expr.Case { Value: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module,
                            DynamicRemote: false,
                            Function: AutoImportNames.Element,
                            Arguments: [Expr.Literal { Value: Integer position }, Expr.Variable input]
                        } element } decision },
                    Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [
                        Expr.Variable selected, Expr.List { Items: [Expr.Variable forwarded], Tail: null }] } tail] } }]
                || !DistinctBindings(parameter, binding) || parameter.Name != input.Name
                || binding.Name != selected.Name || parameter.Name != forwarded.Name
                || !owner.ResolvesErlangBif(element) || position.Value <= 0)
            {
                return false;
            }

            var cases = new Dictionary<Atom, Atom>();
            foreach (Clause clause in decision.Clauses)
            {
                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }],
                        Body: Expr.Literal { Value: Atom result } } || !cases.TryAdd(key, result))
                {
                    return false;
                }
            }

            if (cases.Count < 2)
            {
                return false;
            }

            var ordered = cases.OrderBy(pair => pair.Key,
                Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))).ToArray();
            var branches = ordered.Select(pair => pair.Value).Distinct().Reverse()
                .Select(value => (Value: value, Label: Label())).ToArray();
            var labels = branches.ToDictionary(branch => branch.Value, branch => branch.Label);
            int joined = Label();
            int failed = Label();
            owner.EmitCallLine(element, location);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
            owner.LiteralOperand(position);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(SelectValue, XRegisterTag, 1);
            owner.Operand(LabelTag, failed);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, ordered.Length * 2);
            foreach (var (key, value) in ordered)
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, labels[value]);
            }

            for (int index = 0; index < branches.Length; index++)
            {
                var (value, label) = branches[index];
                Mark(label);
                owner.EmitConstant(value, 1);
                if (index != branches.Length - 1)
                {
                    JumpTo(joined);
                }
            }

            Mark(joined);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(tail, CallLocation(element, location));
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(tail.Module, tail.Function, 2));

            Mark(failed);
            owner.EmitLine(decision.SourceLine == 0 ? CallLocation(element, location)
                : new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 1);

            return true;
        }
    }
}