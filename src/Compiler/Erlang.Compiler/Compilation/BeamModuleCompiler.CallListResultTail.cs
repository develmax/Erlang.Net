using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCallListResultTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable result,
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable left, Expr.Variable right]
                                } call
                            },
                                Expr.Case
                            {
                                Value: Expr.Variable tested,
                                Clauses:
                                    [
                                    {
                                        Guard: null, Patterns: [AliasPattern alias],
                                        Body: Expr.Call
                                        {
                                            Module: null, DynamicRemote: false,
                                            Arguments: [Expr.Variable forwarded]
                                        } consumer
                                    },
                                    {
                                        Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable unchanged
                                    }
                                    ]
                            }
                            ]
                    }
                }
                ]
                || !TryReadPatternAlias(alias, out var binding, out var shape)
                || shape is not Pattern.List
                {
                    Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                }
                || !DistinctBindings(
                    input,
                    tags,
                    result,
                    binding,
                    fallback
                )
                || left.Name != input.Name || right.Name != tags.Name || tested.Name != result.Name
                || forwarded.Name != binding.Name || unchanged.Name != fallback.Name
                || !entries.TryGetValue((call.Function, 2), out int entry)
                || !entries.TryGetValue((consumer.Function, 1), out int tail))
            {
                return false;
            }

            int direct = Label();
            EmitFrameAllocation(2, 0);
            owner.EmitCallLine(call, location);
            owner.EmitFieldCall(2, entry);
            owner.EmitNonemptyList(0, direct);
            owner.EmitCallLine(consumer, CallLocation(call, location));
            EmitTailLocal(tail, 1, 0);
            Mark(direct);
            owner.EmitFrameReturn(0);

            return true;
        }
    }
}
