using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RequiredSequenceCall(
            Expr.Call Call,
            string? Module,
            int Entry,
            Term Options
        );

        private bool TryEmitNamedRequiredSequenceDecoder(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable parsed,
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Variable passedTags]
                                    } match
                                },
                                    ..,
                                    Expr.Case extra,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable output,
                                    Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, ..] } result
                                },
                                    Expr.Variable returned
                                ]
                        } sequence
                    }
                ] || !DistinctBindings(
                    input,
                    tags,
                    parsed,
                    output
                )
                || source.Name != input.Name || passedTags.Name != tags.Name || returned.Name != output.Name
                || !entries.TryGetValue((match.Function, 2), out int matcher))
            {
                return false;
            }

            int count = result.Items.Count - 1;
            if (count < 3 || count >= FirstReservedXRegister || sequence.Items.Count != 2 * count + 4)
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, parsed.Name, output.Name };
            var fields = new SequenceRequired[count];
            var readers = new RequiredSequenceCall[count];
            RequiredSequenceCall? rename = null;
            string remaining = parsed.Name;
            for (int index = 0; index < count; index++)
            {
                if (ReadRequiredPart(
                    sequence.Items[2 * index + 1],
                    sequence.Items[2 * index + 2],
                    remaining,
                    result.Items[index + 1],
                    bindings
                ) is not { } field)
                {
                    return false;
                }

                fields[index] = field;
                remaining = field.Rest.Name;
                Expr read = field.Read;
                if (index == 0)
                {
                    Expr body = read is Expr.Block block ? block.Body : read;
                    if (body is not Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match { Pattern: Pattern.Variable value, Value: var decode },
                                    var named
                                ]
                        }
                        || value.Name == VariableScopeNames.Wildcard || !bindings.Add(value.Name)
                        || ReadRequiredSequenceCall(named, value.Name) is not { Module: null } converted
                        || !IsClosedNamedNumberList(converted.Options))
                    {
                        return false;
                    }

                    rename = converted;
                    read = decode;
                }

                if (ReadRequiredSequenceCall(read, field.Payload.Name) is not { } reader)
                {
                    return false;
                }

                readers[index] = reader;
            }

            if (ReadTableUnexpectedFields(extra, remaining) is not { } unexpected)
            {
                return false;
            }

            int unexpectedLabel = Label();
            var badMatches = new int[count];
            for (int index = count - 1; index >= 0; index--)
            {
                badMatches[index] = Label();
            }

            int frame = count + 1;
            EmitFrameAllocation(2, frame);
            EmitFrameInitialization(0, frame);
            BeamSourceLocation previous = location;
            owner.EmitCallLine(match, previous);
            previous = CallLocation(match, previous);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, matcher);

            var outputs = new int[count - 1];
            var inputs = new int[count];
            int restSlot = 0;
            for (int index = 0; index < count; index++)
            {
                int tagKind = index == 0 ? XRegisterTag : YRegisterTag;
                inputs[index] = restSlot;
                owner.Op(IsNonemptyList, LabelTag, badMatches[index]);
                owner.Operand(tagKind, restSlot);
                owner.Op(GetList, tagKind, restSlot);
                owner.Operand(XRegisterTag, 0);
                int nextRest = index == 0 ? count : count - index - 1;
                owner.Operand(YRegisterTag, nextRest);
                EmitReader(readers[index], index == 0 ? null : restSlot);
                if (index == 0)
                {
                    EmitReader(rename!, null);
                }

                if (index < count - 1)
                {
                    int resultSlot = index == 0 ? count - 1 : restSlot;
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        resultSlot
                    );
                    outputs[index] = resultSlot;
                }

                restSlot = nextRest;
            }

            owner.EmitNilTest(YRegisterTag, restSlot, unexpectedLabel);
            owner.Op(TestHeap, BeamWire.UnsignedTag, count + 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, count + 1);
            owner.LiteralOperand(tag);
            foreach (int register in outputs)
            {
                owner.Operand(YRegisterTag, register);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(unexpectedLabel);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Pair(unexpected.UnexpectedTag, YRegisterTag);
            Pair(unexpected.DetailsTag, XRegisterTag);
            Pair(unexpected.ErrorTag, XRegisterTag);
            owner.EmitCallLine(unexpected.Exit, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, unexpected.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frame);
            for (int index = count - 1; index >= 0; index--)
            {
                Mark(badMatches[index]);
                owner.EmitArithmeticLine(fields[index].Split.SourceLine, ref previous);
                owner.Op(BadMatch, index == 0 ? XRegisterTag : YRegisterTag, inputs[index]);
            }

            return true;

            void EmitReader(RequiredSequenceCall reader, int? dead)
            {
                owner.EmitConstant(reader.Options, 1);
                if (dead is int slot)
                {
                    EmitFrameInitialization(slot, slot + 1);
                }

                owner.EmitCallLine(reader.Call, previous);
                previous = CallLocation(reader.Call, previous);
                if (reader.Module is null)
                {
                    owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
                    owner.Operand(LabelTag, reader.Entry);
                }
                else
                {
                    Call(reader.Module, reader.Call.Function, 2);
                }
            }

            void Pair(Atom atom, int kind)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(atom);
                owner.Operand(kind, 0);
            }
        }

        private RequiredSequenceCall? ReadRequiredSequenceCall(Expr expression, string source)
        {
            if (expression is not Expr.Call
                {
                    DynamicRemote: false, Arguments: [Expr.Variable input, var tag]
                } call || input.Name != source
                || !AttributeLiteral.TryNormalizeConstant(tag, out Term options)
                || !TryResolveOrdinaryCallTarget(call, out string? module))
            {
                return null;
            }

            int entry = 0;
            if (module is null && !entries.TryGetValue((call.Function, 2), out entry))
            {
                return null;
            }

            return new(
                call,
                module,
                entry,
                options
            );
        }
    }
}
