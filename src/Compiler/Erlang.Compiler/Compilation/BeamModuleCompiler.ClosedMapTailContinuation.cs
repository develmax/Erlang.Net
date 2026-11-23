using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedMapTailContinuation(FunctionDefinition function)
        {
            if (recursiveClosure is not null || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence
                    {
                        Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable result,
                                    Value: Expr.Call
                                    {
                                        DynamicRemote: false, Module: ListModuleNames.Module, Function: ListModuleNames.Map,
                                        Arguments: [Expr.Fun { SelfName: null } apply, Expr.Variable list]
                                    } map
                                },
                                    Expr.Call
                                {
                                    DynamicRemote: false, Module: not null, Arguments: [Expr.Variable mapped]
                                } finish
                                ]
                    }
                }
                ]
                || !DistinctBindings(input, result) || list.Name != input.Name || mapped.Name != result.Name
                || !TryResolveOrdinaryCallTarget(finish, out string? module) || module is null
                || IsTrappingTailBif(module, finish.Function, 1)
                || ClosureCaptures.Capture(apply, new Dictionary<string, Term>(StringComparer.Ordinal) { [input.Name] = Nil.Value }).Count != 0)
            {
                return false;
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.EmitClosureHeap(words: 0, live: 1, funCount: 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            EmitGeneralClosure(apply, heapChecked: true);
            owner.EmitCallLine(map, location);
            Call(map.Module, map.Function, 2);
            owner.EmitCallLine(finish, CallLocation(map, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(module, finish.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            return true;
        }
    }
}
