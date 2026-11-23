using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCaughtPositionalListLoop(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity is < 2 or >= FirstReservedXRegister
                || function.Clauses is not
                [
                { Guard: null } nil,
                {
                    Guard: null,
                    Body: Expr.Case
                    {
                        Value: Expr.Catch { Operand: Expr.Call invoke },
                        Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [AliasPattern expected],
                                Body: Expr.Variable returned
                            },
                            {
                                Guard: null,
                                Patterns: [global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                Body: Expr.Call { Module: null, DynamicRemote: false } recurse
                            }
                            ]
                    }
                } cons
                ]
                || recurse.Function != function.Name || recurse.Arguments.Count != function.Arity
                || !entries.TryGetValue((recurse.Function, recurse.Arguments.Count), out int entry)
                || invoke.Arguments.Count != function.Arity
                || !TryReadPatternAlias(expected, out var result, out var resultShape)
                || result.Name != returned.Name || result.Name == VariableScopeNames.Wildcard
                || resultShape is not Pattern.Tuple
                {
                    Items:
                    [
                        Pattern.Literal { Value: Atom tag },
                        global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ]
                }
                || !AttributeLiteral.TryNormalizeConstant(nil.Body, out Term emptyResult))
            {
                return false;
            }

            int input = function.Arity - 1;
            if (ListEntryTest(nil.Patterns[input]) != IsNil
                || nil.Patterns.Take(input).Any(pattern => pattern is not global::Erlang.Pattern.Variable)
                || cons.Patterns[input] is not Pattern.List
                {
                    Items: [Pattern.Variable head], Tail: Pattern.Variable tail
                }
                || cons.Patterns.Take(input).Any(pattern => pattern is not global::Erlang.Pattern.Variable))
            {
                return false;
            }

            var emptyBindings = nil.Patterns.Take(input).Cast<Pattern.Variable>()
                .Where(parameter => parameter.Name != VariableScopeNames.Wildcard).ToArray();
            var parameters = cons.Patterns.Take(input).Cast<Pattern.Variable>().ToArray();
            string[] headInputs = [.. parameters.Select(parameter => parameter.Name), head.Name];
            string[] tailInputs = [.. parameters.Select(parameter => parameter.Name), tail.Name];
            if (!DistinctBindings(emptyBindings) || !DistinctBindings([.. parameters, head, tail, result])
                || !PassesPositionalBindings(invoke, headInputs) || !PassesPositionalBindings(recurse, tailInputs)
                || ReadEntryCall(invoke, headInputs) is not { } prepared)
            {
                return false;
            }

            // The checked cons path owns the frame. The extracted head is the
            // final outgoing X operand, and needs no home across the caught call.
            int empty = Label();
            int handler = Label();
            int next = Label();
            int token = function.Arity;
            int frame = token + 1;
            owner.EmitNonemptyList(input, empty);
            EmitFrameAllocation(function.Arity, frame);
            for (int register = input - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    input - register
                );
            }

            owner.EmitListParts(
                input,
                XRegisterTag,
                input,
                YRegisterTag,
                0
            );
            EnterCatch(token, handler);
            EmitEntryCall(prepared);
            EndCatch(token, handler);
            owner.EmitRecordTest(
                next,
                XRegisterTag,
                0,
                tag,
                2
            );
            owner.EmitFrameReturn(frame);

            Mark(next);
            for (int slot = 0; slot <= input; slot++)
            {
                Load(slot, input - slot);
            }

            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            owner.EmitConstant(emptyResult, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static bool PassesPositionalBindings(Expr.Call call, string[] bindings) =>
            call.Arguments.Zip(bindings, (argument, name) => argument is Expr.Variable variable
                && variable.Name == name && name != VariableScopeNames.Wildcard).All(passed => passed);
    }
}
