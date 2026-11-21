using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ProtectedCallFailure(
            Atom Tag,
            int Arity,
            Expr.Call Handler,
            Term[] Arguments
        );

        private bool TryEmitProtectedSavedCallSequence(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Try
                    {
                        Clauses: [], After: null,
                        Body: Expr.Sequence { Items: [_, .., var result] } sequence,
                        Catches: [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple
                                {
                                    Items: [
                                            Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                            Pattern.Tuple reason,
                                            Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                        ]
                                }],
                                Body: var recovered
                            }
                            ]
                    }
                } clause
                ] || !HasUnconditionalParameters(clause) || !AttributeLiteral.TryNormalizeConstant(result, out Term constant)
                || reason.Items is not [Pattern.Literal { Value: Atom tag }, ..]
                || reason.Items.Skip(1).Any(field => field is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadProtectedCallHandler(recovered, parameters) is not { } handler
                || ReadCallsWithClosures(
                    FlattenCallArguments(sequence.Items.SkipLast(1), prepareClosures: true),
                    parameters,
                    out var closures
                ) is not { Length: >= 3 } calls || closures.Count == 0
                || ReadSavedCallSequence(
                    calls,
                    parameters,
                    extraction: null,
                    closures: closures,
                    constant: constant,
                    protectedFrame: true
                ) is not { } plan)
            {
                return false;
            }

            var (call, arguments) = handler;
            EmitSavedCallSequence(plan, protection: new ProtectedCallFailure(
                tag,
                reason.Items.Count,
                call,
                arguments
            ));

            return true;
        }

        private static (Expr.Call Call, Term[] Arguments)? ReadProtectedCallHandler(Expr body, string[] parameters)
        {
            Expr.Call? effect = body as Expr.Call;
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            if (body is Expr.Sequence { Items: [.., Expr.Call final] } sequence)
            {
                effect = final;
                foreach (Expr setup in sequence.Items.SkipLast(1))
                {
                    if (setup is not Expr.Match { Pattern: Pattern.Variable binding, Value: var value }
                        || binding.Name == VariableScopeNames.Wildcard || parameters.Contains(binding.Name)
                        || !AttributeLiteral.TryNormalizeConstant(value, out Term folded, constants)
                        || !constants.TryAdd(binding.Name, folded))
                    {
                        return null;
                    }
                }
            }

            if (effect is not { Module: not null, DynamicRemote: false, Arguments.Count: 2 } call)
            {
                return null;
            }

            var arguments = new Term[call.Arguments.Count];
            for (int index = 0; index < arguments.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[index], out arguments[index], constants))
                {
                    return null;
                }
            }

            return (call, arguments);
        }

        private void EmitProtectedCallFailure(
            ProtectedCallFailure failure,
            int token,
            int handler,
            BeamSourceLocation previous
        )
        {
            int reject = Label();
            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            EmitDetailRecordTest(
                1,
                failure.Arity,
                failure.Tag,
                reject
            );
            owner.EmitConstant(failure.Arguments[1], 1);
            owner.EmitConstant(failure.Arguments[0], 0);
            owner.EmitCallLine(failure.Handler, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, failure.Arguments.Length);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(failure.Handler.Module!, failure.Handler.Function, failure.Arguments.Length));
            owner.Operand(BeamWire.UnsignedTag, token + 1);
            Mark(reject);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
        }
    }
}
