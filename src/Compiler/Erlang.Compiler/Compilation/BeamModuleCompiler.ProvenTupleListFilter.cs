using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (int Position, Atom[] Excluded)? ReadTupleFieldExclusions(Expr guard, string item)
    {
        if (guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } both
            || ReadComparison(both.Left) is not { } left || ReadComparison(both.Right) is not { } right
            || left.Position != right.Position || left.Key.Equals(right.Key))
        {
            return null;
        }

        var order = Comparer<Atom>.Create((first, second) => TermOrder.Compare(first, second, exact: true));

        return (left.Position, new[] { left.Key, right.Key }.Order(order).ToArray());

        (int Position, Atom Key)? ReadComparison(Expr expression)
        {
            if (expression is not Expr.Binary { Operator: ErlangOperators.ExactNotEqual } comparison)
            {
                return null;
            }

            var (source, literal) = comparison.Left is Expr.Call
                ? (comparison.Left, comparison.Right) : (comparison.Right, comparison.Left);
            if (literal is not Expr.Literal { Value: Atom key } || source is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    DynamicRemote: false,
                    Function: AutoImportNames.Element,
                    Arguments: [Expr.Literal { Value: Integer position }, Expr.Variable tuple]
                } element || tuple.Name != item || position.Value <= 0 || position.Value > int.MaxValue
                || !ResolvesErlangBif(element))
            {
                return null;
            }

            return ((int)position.Value, key);
        }
    }

    private bool HasMinimumTupleListInput(
        Expr source,
        Clause parent,
        Expr.ListComprehension comprehension,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        int minimumArity
    )
    {
        return source is Expr.Variable binding
            ? HasDominatingListBinding(
                parent,
                comprehension,
                binding.Name,
                ReadBoundValue
            )
            : IsLocalTupleListCall(source);

        bool ReadBoundValue(Expr expression, HashSet<string> proven) => expression is Expr.Variable alias
            ? proven.Contains(alias.Name) : IsLocalTupleListCall(expression);

        bool IsLocalTupleListCall(Expr expression)
        {
            if (OnLoadFunction is not null || expression is not Expr.Call { Module: null, DynamicRemote: false } call
                || ResolvesErlangBif(call))
            {
                return false;
            }

            var signature = (call.Function, call.Arguments.Count);
            if (IsDeclaredNative(signature) || !localFunctions.TryGetValue(signature, out var producer))
            {
                return false;
            }

            bool constructsTuple = false;

            return producer.Clauses.All(clause => IsTupleListResult(clause.Body)) && constructsTuple;

            // A successful self call has a finite return derivation. Every terminal
            // path in that derivation must return nil or construct a proper tuple list.
            bool IsTupleListResult(Expr result) => result switch
            {
                Expr.Block block => IsTupleListResult(block.Body),
                Expr.Sequence { Items: [.., var final] } => IsTupleListResult(final),
                Expr.Case choice => choice.Clauses.All(arm => IsTupleListResult(arm.Body)),
                Expr.If choice => choice.Clauses.All(arm => IsTupleListResult(arm.Body)),
                Expr.List list => list.Items.All(IsTupleHead) && (list.Tail is null || IsTupleListResult(list.Tail)),
                Expr.Literal { Value: Nil } => true,
                Expr.Call { Module: null, DynamicRemote: false } recurse =>
                    recurse.Function == signature.Function && recurse.Arguments.Count == signature.Count,
                _ => false
            };

            bool IsTupleHead(Expr head)
            {
                bool tuple = head is Expr.Tuple construction && construction.Items.Count >= minimumArity
                    || head is Expr.Literal { Value: TupleTerm literal } && literal.Items.Count >= minimumArity;
                constructsTuple |= tuple;

                return tuple;
            }
        }
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProvenTupleListFilter(FunctionDefinition function)
        {
            if (!function.HasProvenRecursiveTupleFilterInput || !function.IsRecursiveListMapper || function.Arity != 1
                || function.Clauses[0] is not
                {
                    Guard: { } guard,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [Expr.Variable kept] }
                } || kept.Name != item.Name || owner.ReadTupleFieldExclusions(guard, item.Name) is not { } plan
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int skip = Label();
            int keep = Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitTuplePart(1, plan.Position - 1, 2);
            owner.EmitMembershipSelect(2, keep, plan.Excluded.Select(atom => ((Term)atom, skip)));

            Mark(skip);
            EmitRecursiveListMappingRejection(function, entry, rejectedRegister: 1);

            Mark(keep);
            EmitFrameAllocation(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitRecursiveCons(entry, 1);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}
