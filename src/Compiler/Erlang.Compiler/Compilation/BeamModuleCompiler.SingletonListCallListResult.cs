using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingletonListCallListResult(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable input], Tail: null }],
                    Body: Expr.Sequence { Items.Count: >= 3 } sequence
                }
                ] || input.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal) { input.Name };
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            var calls = new List<(string Binding, Expr.Call Call)>();
            string? listBinding = null;
            foreach (Expr item in sequence.Items.SkipLast(1))
            {
                if (item is Expr.Literal)
                {
                    continue;
                }

                if (item is not Expr.Match { Pattern: Pattern.Variable name, Value: var value }
                    || name.Name == VariableScopeNames.Wildcard || !names.Add(name.Name))
                {
                    return false;
                }

                if (AttributeLiteral.TryNormalizeConstant(value, out Term literal, constants))
                {
                    constants.Add(name.Name, literal);
                }
                else if (value is Expr.Call call && calls.Count < 2)
                {
                    var prepared = call with { Arguments = call.Arguments.Select(arg => SubstituteConstants(arg, constants)).ToArray() };
                    calls.Add((name.Name, prepared));
                }
                else if (listBinding is null && calls.Count == 2 && IsResultList(value, calls[^1].Binding))
                {
                    listBinding = name.Name;
                }
                else
                {
                    return false;
                }
            }

            if (calls is not [var first, var second]
                || sequence.Items[^1] is not Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom tag }, Expr.Tuple
                    { Items: [var flag, var list] }]
                }
                || !AttributeLiteral.TryNormalizeConstant(flag, out Term flags, constants)
                || !(IsResultList(list, second.Binding) || list is Expr.Variable saved && saved.Name == listBinding)
                || ReadEntryCall(first.Call, [VariableScopeNames.Wildcard, input.Name]) is not { } producer)
            {
                return false;
            }

            var consumesResult = second.Call.Arguments.Select((argument, index) => (argument, index))
                .Where(item => item.argument is Expr.Variable variable && variable.Name == first.Binding).ToArray();
            if (consumesResult is not [var forwarded]
                || ReadEntryCall(second.Call, [], preparedOperand: forwarded.index) is not { } continuation)
            {
                return false;
            }

            owner.EmitNonemptyList(0, functionInfo);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.Op(IsNil, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 2);

            int live = EntryCallLiveCount(producer, [VariableScopeNames.Wildcard, input.Name]);
            EmitFrameAllocation(live, 0);
            EmitEntryCall(producer);
            var next = continuation with { Moves = forwarded.index == 0 ? [] : [(forwarded.index, 0)] };
            EmitEntryCall(next, CallLocation(first.Call, location));
            owner.EmitChildListResult(tag, flags, 1);
            owner.EmitFrameReturn(0);

            return true;

            static bool IsResultList(Expr value, string binding) =>
                value is Expr.List { Tail: null, Items: [Expr.Variable result] } && result.Name == binding;
        }
    }
}
