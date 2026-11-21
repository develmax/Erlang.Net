using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedTaggedHeadDispatch(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Arity != 2 || function.Clauses.Count < 4
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }, Pattern.Variable accumulator],
                    Body: Expr.Call { Arguments: [Expr.Variable emptyArg] } emptyCall
                },
                    ..,
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }, Pattern.Variable restAccumulator],
                    Body: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable next, Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable rest }]
                    } recur
                }
                ]
                || !DistinctBindings(accumulator)
                || emptyArg.Name != accumulator.Name || !TryResolveOrdinaryCallTarget(emptyCall, out string? emptyModule)
                || !DistinctBindings(head, tail, restAccumulator)
                || next.Name != tail.Name || item.Name != head.Name || rest.Name != restAccumulator.Name
                || recur.Function != function.Name
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            int width = 0;
            int shared = 0;
            var tags = new SortedSet<Atom>(Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)));
            foreach (Clause clause in function.Clauses.Skip(1).SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns:
                        [
                            Pattern.List { Items: [Pattern.Tuple { Items: var fields }], Tail: Pattern.Variable unused },
                            Pattern.Variable saved
                        ],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input] } call
                    }
                    || saved.Name != input.Name || saved.Name == VariableScopeNames.Wildcard
                    || fields.Count < 2 || fields[0] is not Pattern.Literal { Value: Atom tag }
                    || !fields.Skip(1).All(field => field is Pattern.Variable)
                    || !AllBindingsDistinct(clause.Patterns)
                    || !tags.Add(tag) || width != 0 && fields.Count != width
                    || !entries.TryGetValue((call.Function, 1), out int target)
                    || shared != 0 && target != shared)
                {
                    return false;
                }

                width = fields.Count;
                shared = target;
            }

            int functionFailure = owner.nextLabel - 2;
            int matched = Label();
            int keep = Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.Op(IsTuple, LabelTag, keep);
            owner.Operand(XRegisterTag, 2);
            owner.Op(TestArity, LabelTag, keep);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, width);
            owner.EmitTuplePart(2, 0, 3);
            owner.Op(SelectValue, XRegisterTag, 3);
            owner.Operand(LabelTag, keep);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tags.Count * 2);
            foreach (Atom tag in tags)
            {
                owner.LiteralOperand(tag);
                owner.Operand(LabelTag, matched);
            }

            Mark(matched);
            owner.MoveToX0(XRegisterTag, 1);
            EmitTailLocal(shared, 1, frame: null);
            Mark(keep);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            EmitTailLocal(entry, 2, frame: null);
            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, functionFailure);
            owner.MoveToX0(XRegisterTag, 1);
            if (emptyModule is null)
            {
                EmitTailLocal(entries[(emptyCall.Function, 1)], 1, frame: null);
            }
            else
            {
                owner.EmitCallLine(emptyCall, location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(emptyModule, emptyCall.Function, 1));
            }

            return true;

            static bool AllBindingsDistinct(IEnumerable<Pattern> patterns)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);

                return patterns.SelectMany(Semantics.Variables).All(name =>
                    name == VariableScopeNames.Wildcard || names.Add(name));
            }
        }
    }
}
