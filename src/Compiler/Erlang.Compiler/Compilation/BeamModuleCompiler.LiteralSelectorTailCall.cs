using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool HasClosedAtomSelector(
        FunctionDefinition function,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        if (function.Clauses is not [
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom first }], Body: Expr.Literal { Value: Atom } },
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom second }], Body: Expr.Literal { Value: Atom } }
        ] || first.Equals(second) || exportedFunctions.Contains((function.Name, 1)))
        {
            return false;
        }

        var observed = new HashSet<Atom>();
        foreach (var expression in localFunctions.Values.SelectMany(definition => LocalFunctionEntries.ClauseExpressions(definition.Clauses))
            .SelectMany(LocalFunctionEntries.Walk))
        {
            if (expression is Expr.FunctionReference reference && reference.Name == function.Name && reference.Arity == 1)
            {
                return false;
            }

            if (expression is Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 1 } call && call.Function == function.Name)
            {
                if (call.Arguments is not [Expr.Literal { Value: Atom key }] || (!first.Equals(key) && !second.Equals(key)))
                {
                    return false;
                }

                observed.Add(key);
            }
        }

        return observed.Count == 2;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedAtomSelector(FunctionDefinition function)
        {
            if (!owner.HasClosedAtomSelector(function, localFunctions))
            {
                return false;
            }

            int alternative = Label();
            owner.EmitLiteralTest(function.Clauses[0], alternative);
            owner.EmitConstant(((Expr.Literal)function.Clauses[0].Body).Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(alternative);
            owner.EmitConstant(((Expr.Literal)function.Clauses[1].Body).Value, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitLiteralSelectorTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Call { Module: not null, DynamicRemote: false } outer } clause])
            {
                return false;
            }

            Expr.Call selector;
            Term? pair = null;
            Atom? module = null;
            int frame;
            if (clause.Patterns is [] && outer.Arguments is [
                Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Call tupleSelector] },
                Expr.Literal { Value: Atom callback }, Expr.List { Items: [], Tail: null }])
            {
                selector = tupleSelector;
                module = callback;
                frame = 0;
                if (selector.Arguments is not [Expr.Literal { Value: Atom key }]
                    || !localFunctions.TryGetValue((selector.Function, 1), out var definition)
                    || !HasLiteralDispatchKeys(definition.Clauses)
                    || definition.Clauses.FirstOrDefault(branch => branch.Patterns is [Pattern.Literal literal]
                        && literal.Value.Equals(key))?.Body is not Expr.Literal { Value: Atom selected })
                {
                    return false;
                }

                pair = new TupleTerm([tag, selected]);
            }
            else if (clause.Patterns is [Pattern.Variable parameter] && parameter.Name != "_"
                && outer.Arguments is [Expr.Call argumentSelector, Expr.Variable forwarded] && forwarded.Name == parameter.Name)
            {
                selector = argumentSelector;
                frame = 1;
            }
            else
            {
                return false;
            }

            if (selector is not { Module: null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Atom input }] }
                || !entries.TryGetValue((selector.Function, 1), out int target))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            if (frame == 1)
            {
                Save();
            }

            owner.EmitConstant(input, 0);
            owner.EmitCallLine(selector, location);
            owner.EmitFieldCall(1, target);
            if (pair is not null)
            {
                owner.EmitConstant(module!, 1);
                owner.EmitConstant(Nil.Value, 2);
                owner.EmitConstant(pair, 0);
            }
            else
            {
                Load(0, 1);
            }

            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, outer.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(outer.Module, outer.Function, outer.Arguments.Count));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;
        }
    }
}
