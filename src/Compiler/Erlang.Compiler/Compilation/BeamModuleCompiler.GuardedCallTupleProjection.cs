using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedCallTupleProjection(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable firstArg, Expr.Variable secondArg]
                        } prepare,
                        Clauses:
                            [
                            {
                                Patterns: [Pattern.Variable binary],
                                Guard: Expr.Call
                                {
                                    Module: null or ErlangModuleNames.Module,
                                    Function: AutoImportNames.IsBinary,
                                    DynamicRemote: false,
                                    Arguments: [Expr.Variable tested]
                                } typeCheck,
                                Body: Expr.Sequence
                                {
                                    Items:
                                        [
                                            Expr.Match
                                        {
                                            Pattern: Pattern.Tuple
                                            {
                                                Items: [Pattern.Variable value, Pattern.Variable
                                                { Name: VariableScopeNames.Wildcard }]
                                            },
                                            Value: Expr.Call
                                            {
                                                Module: null,
                                                DynamicRemote: false,
                                                Arguments: [Expr.Variable input]
                                            } project
                                        } match,
                                            Expr.Variable field
                                        ]
                                }
                            },
                            {
                                Guard: null,
                                Patterns: [Pattern.Variable unchanged],
                                Body: Expr.Variable fallback
                            }
                            ]
                    }
                }
                ]
                || !DistinctBindings(
                    first,
                    second,
                    binary,
                    value
                )
                || !DistinctBindings(first, second, unchanged)
                || first.Name != firstArg.Name || second.Name != secondArg.Name
                || binary.Name != input.Name || binary.Name != tested.Name
                || value.Name != field.Name || unchanged.Name != fallback.Name
                || !owner.ResolvesErlangBif(typeCheck)
                || !entries.TryGetValue((prepare.Function, 2), out int prepareTarget)
                || !entries.TryGetValue((project.Function, 1), out int projectTarget))
            {
                return false;
            }

            int unchangedReturn = Label();
            int badMatch = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitCallLine(prepare, location);
            owner.EmitFieldCall(2, prepareTarget);
            owner.Op(IsBinary, LabelTag, unchangedReturn);
            owner.Operand(XRegisterTag, 0);

            // The guarded call replaces the selected value with its own result.
            // Both returns and the failed tuple match can consume X0 directly.
            BeamSourceLocation prepared = CallLocation(prepare, location);
            owner.EmitCallLine(project, prepared);
            owner.EmitFieldCall(1, projectTarget);
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                badMatch
            );
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitFrameReturn(0);

            Mark(unchangedReturn);
            owner.EmitFrameReturn(0);

            Mark(badMatch);
            BeamSourceLocation call = CallLocation(project, prepared);
            if (match.SourceLine != 0 && new BeamSourceLocation(match.PreprocessorFile ?? location.File, match.SourceLine) is var source
                && source != call)
            {
                owner.EmitLine(source);
            }

            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
