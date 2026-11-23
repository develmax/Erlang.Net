using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProvenTerminalRange(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not [{ Guard: null } clause])
            {
                return false;
            }

            Expr.Call? prepare = null;
            Atom? selector = null;
            IReadOnlyList<Pattern.Variable> inputs;
            Expr expression = clause.Body;
            if (clause.Patterns is [Pattern.Variable input, Pattern.Variable tag])
            {
                inputs = [input, tag];
            }
            else if (clause.Patterns is [Pattern.Literal { Value: Atom key }, Pattern.Variable bytes, Pattern.Variable ignored]
                && expression is Expr.Sequence
                {
                    Items: [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable prepared,
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable buffer] } producer
                    }, Expr.Block rangeBody]
                }
                && bytes.Name == buffer.Name && (ignored.Name == VariableScopeNames.Wildcard || ignored.Name != bytes.Name)
                && (ignored.Name == VariableScopeNames.Wildcard || ignored.Name != prepared.Name))
            {
                inputs = ignored.Name == VariableScopeNames.Wildcard ? [prepared, bytes] : [prepared, bytes, ignored];
                expression = rangeBody;
                prepare = producer;
                selector = key;
            }
            else
            {
                return false;
            }

            if (selector is not null && prepare is not null && inputs.Count == 2
                && DistinctBindings(inputs[0], inputs[1])
                && entries.TryGetValue((prepare.Function, 1), out int preparation)
                && !owner.IsDeclaredNative((prepare.Function, 1))
                && ReadForwardedChoiceMeasurement(expression, inputs[0].Name, inputs[1].Name) is
                {
                    Measure.Function: AutoImportNames.ByteSize,
                    Convert: { } conversion
                } measurement)
            {
                EmitPreparedMeasurementTail(
                    selector,
                    prepare,
                    preparation,
                    measurement,
                    conversion
                );

                return true;
            }

            if (expression is not Expr.Block
                {
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable decoded,
                            Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable selected, var tags] } call
                        },
                            Expr.If { Clauses: [{ } accepted, _] } check]
                    }
                }
                || selected.Name != inputs[0].Name || !DistinctBindings([.. inputs, decoded])
                || !entries.TryGetValue((call.Function, 2), out int decoder)
                || !ReturnsSignedSegment(call))
            {
                return false;
            }

            int prepareEntry = 0;
            Term? constant = null;
            if (prepare is null)
            {
                if (tags is not Expr.Variable forwarded || forwarded.Name != inputs[1].Name)
                {
                    return false;
                }
            }
            else if (!entries.TryGetValue((prepare.Function, 1), out prepareEntry)
                || !AttributeLiteral.TryNormalizeConstant(tags, out constant))
            {
                return false;
            }

            Expr.Call? map = null;
            Term? mappingArgument = null;
            int mapEntry = 0;
            if (accepted.Body is Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable alias, Value: Expr.Variable source },
                        Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable result, var table]
                    } resolve]
                }
                && source.Name == decoded.Name && result.Name == alias.Name
                && DistinctBindings([.. inputs, decoded, alias])
                && entries.TryGetValue((resolve.Function, 2), out mapEntry)
                && AttributeLiteral.TryNormalizeConstant(table, out mappingArgument))
            {
                map = resolve;
                check = check with { Clauses = [accepted with { Body = source }, check.Clauses[1]] };
            }

            if (ReadTaggedBranchRange(check, decoded.Name, integerResult: true) is not { } range
                || !IsDecoderIntrinsic(range.Exit))
            {
                return false;
            }

            if (selector is not null)
            {
                owner.EmitAtomTest(0, selector.Name, owner.nextLabel - 2);
            }

            EmitFrameAllocation(2, 0);
            BeamSourceLocation previous = location;
            if (prepare is not null)
            {
                owner.MoveToX0(XRegisterTag, 1);
                owner.EmitCallLine(prepare, previous);
                owner.EmitFieldCall(1, prepareEntry);
                previous = CallLocation(prepare, previous);
                owner.EmitConstant(constant!, 1);
            }

            owner.EmitCallLine(call, previous);
            owner.EmitFieldCall(2, decoder);
            previous = CallLocation(call, previous);
            int rejected = Label();
            EmitRangeTests(
                call,
                range,
                rejected,
                provenInteger: true
            );
            if (map is not null)
            {
                owner.EmitConstant(mappingArgument!, 1);
                owner.EmitCallLine(map, previous);
                EmitTailLocal(mapEntry, 2, 0);
            }
            else
            {
                owner.EmitFrameReturn(0);
            }

            Mark(rejected);
            var exitLocation = CallLocation(range.Exit, previous);
            EmitRangeExit(range, exitLocation == previous ? null : exitLocation, 0);

            return true;
        }

        private bool ReturnsSignedSegment(Expr.Call call)
        {
            var key = (call.Function, call.Arguments.Count);

            return !owner.IsDeclaredNative(key)
                && localFunctions.TryGetValue(key, out var reader)
                && reader.Clauses is [
                    {
                        Body: Expr.Sequence
                        {
                            Items: [.., Expr.Match
                            {
                                Pattern: BitPattern
                                {
                                    Segments: [
                                        {
                                            Value: Pattern.Variable segment,
                                            Specification: { Type: BitSegmentTypes.Integer, Signed: true }
                                        }]
                                }
                            }, Expr.Variable returned]
                        }
                    }]
                && segment.Name != VariableScopeNames.Wildcard && segment.Name == returned.Name;
        }
    }
}
