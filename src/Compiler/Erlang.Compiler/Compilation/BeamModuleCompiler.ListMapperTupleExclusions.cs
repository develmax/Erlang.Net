using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListMapperTupleExclusions(Expr.Binary conjunction, int failure)
        {
            if (parentFunction is not { IsRecursiveListMapper: true, IsStrictRecursiveListMapper: false, Arity: 1 } mapper
                || mapper.Clauses[0] is not
                {
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [Expr.Variable emitted] }
                } || item.Name != emitted.Name
                || !CanUseEntryTypeFacts() || owner.receiveScratchRegisters is not null)
            {
                return false;
            }

            Expr.Call? projection = null;
            Integer? field = null;
            var keys = new SortedSet<Atom>(MapKeyOrder);
            Expr current = conjunction;
            while (true)
            {
                var pair = current as Expr.Binary;
                var remaining = pair is { Operator: ErlangOperators.AndAlso } ? pair.Right : null;
                Expr comparison = remaining is null ? current : pair!.Left;
                if (comparison is not Expr.Binary
                    {
                        Operator: ErlangOperators.ExactNotEqual,
                        Left: Expr.Call
                        {
                            Function: AutoImportNames.Element,
                            DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable tuple]
                        } read,
                        Right: Expr.Literal { Value: Atom key }
                    }
                    || index.Value.Sign <= 0 || !owner.ResolvesErlangBif(read)
                    || tuple.Name != item.Name || !variables.ContainsKey(tuple.Name)
                    || projection is not null && !index.Equals(field))
                {
                    return false;
                }

                projection ??= read;
                field ??= index;
                keys.Add(key);
                if (remaining is null)
                {
                    break;
                }

                current = remaining;
            }

            if (keys.Count < 2)
            {
                return false;
            }

            // Keep the element failure edge. Only the successful projection is
            // shared, and every excluded atom branches to the same next clause.
            int slot = ValueSlot(projection!, failure);
            int accepted = Label();
            owner.Op(SelectValue, YRegisterTag, slot);
            owner.EmitMembershipSelectTable(accepted, keys.Select(key => ((Term)key, failure)).ToArray());
            Mark(accepted);

            return true;
        }
    }
}
