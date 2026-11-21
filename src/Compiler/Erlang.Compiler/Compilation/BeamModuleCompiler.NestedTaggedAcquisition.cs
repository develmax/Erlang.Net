using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedTaggedAcquisition(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [
                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom outerTag }, ..] } outer,
                            Pattern.Variable unused,
                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom stateTag }, ..] } state
                        ],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable resource,
                                Value: Expr.Call
                                {
                                    Module: not null, DynamicRemote: false, Arguments: [Expr.Variable request]
                                } create
                            },
                                Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [Expr.Variable acquired, ..]
                            } consume,
                                Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [Expr.Variable released]
                            } release,
                                var final
                            ]
                        }
                    }
                ] || acquired.Name != resource.Name || released.Name != resource.Name
                || consume.Arguments.Count < 2 || consume.Arguments.Count > FirstReservedXRegister
                || !entries.TryGetValue((consume.Function, consume.Arguments.Count), out int entry)
                || !AttributeLiteral.TryNormalizeConstant(final, out Term result))
            {
                return false;
            }

            int eventField = -1;
            Pattern.Tuple? nested = null;
            for (int index = 1; index < outer.Items.Count; index++)
            {
                if (outer.Items[index] is Pattern.Tuple tuple && nested is null)
                {
                    nested = tuple;
                    eventField = index;
                }
                else if (outer.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            if (nested is not { Items: [Pattern.Literal { Value: Atom eventTag }, ..] }
                || nested.Items.Skip(1).Any(field => field is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return false;
            }

            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 1; index < state.Items.Count; index++)
            {
                if (state.Items[index] is not Pattern.Variable field
                    || field.Name != VariableScopeNames.Wildcard && !bindings.TryAdd(field.Name, index))
                {
                    return false;
                }
            }

            if (resource.Name == VariableScopeNames.Wildcard || resource.Name == unused.Name
                || bindings.ContainsKey(resource.Name) || bindings.ContainsKey(unused.Name)
                || !bindings.TryGetValue(request.Name, out int requestField))
            {
                return false;
            }

            var projections = new int[consume.Arguments.Count - 1];
            for (int index = 0; index < projections.Length; index++)
            {
                if (consume.Arguments[index + 1] is not Expr.Variable selected
                    || !bindings.TryGetValue(selected.Name, out projections[index]))
                {
                    return false;
                }
            }

            int failure = owner.nextLabel - 2;
            EmitDetailRecordTest(
                0,
                outer.Items.Count,
                outerTag,
                failure
            );
            owner.EmitTuplePart(0, eventField, 3);
            EmitDetailRecordTest(
                3,
                nested.Items.Count,
                eventTag,
                failure
            );
            EmitDetailRecordTest(
                2,
                state.Items.Count,
                stateTag,
                failure
            );
            EmitFrameAllocation(3, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(2, requestField, 0);
            owner.EmitCallLine(create, location);
            owner.EmitStaticRemoteCall(create);

            for (int index = 0; index < projections.Length; index++)
            {
                owner.Op(GetTupleElement, YRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, projections[index]);
                owner.Operand(XRegisterTag, index + 1);
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(consume, CallLocation(create, location));
            owner.Op(LocalCall, BeamWire.UnsignedTag, consume.Arguments.Count);
            owner.Operand(LabelTag, entry);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(release, CallLocation(consume, location));
            owner.EmitStaticRemoteCall(release);
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(0);

            return true;
        }
    }
}
