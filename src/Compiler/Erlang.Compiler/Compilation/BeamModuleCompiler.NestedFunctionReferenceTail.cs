using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedFunctionReferenceTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Call
                    {
                        Arguments: [Expr.Call
                        {
                            Arguments: [Expr.FunctionReference reference, var seed, Expr.Variable forwarded]
                        } producer]
                    } continuation
                }
                ] || input.Name == VariableScopeNames.Wildcard || input.Name != forwarded.Name
                || !AttributeLiteral.TryNormalizeConstant(seed, out Term constant)
                || !entries.TryGetValue((reference.Name, reference.Arity), out int entry)
                || ReadEntryCall(producer, [input.Name], allowFunctionArgument: true) is not { } ordinary
                || ReadEntryCall(continuation, [], preparedOperand: 0) is not { } tail)
            {
                return false;
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.EmitClosureHeap(0, 1, 1);
            owner.EmitDeclaredFunctionReference(reference, entry, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitConstant(constant, 1);
            owner.EmitCallLine(producer, location);
            Call(ordinary.Module, producer.Function, producer.Arguments.Count);
            EmitPreparedFrameTail(tail, CallLocation(producer, location), 0);

            return true;
        }
    }
}
