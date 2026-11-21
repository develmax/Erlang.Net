using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRequiredTableSequenceDecoder(FunctionDefinition function)
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
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Variable inputTags]
                                } match
                            },
                                var firstSplit,
                                var firstRead,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable callback,
                                Value: Expr.Call
                                {
                                    Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key]
                                } select
                            },
                                var secondSplit,
                                var secondRead,
                                Expr.Case extra,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable output,
                                Value: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, var firstValue, var secondValue] }
                            },
                                Expr.Variable returned
                            ]
                        }
                    }
                ] || !DistinctBindings(
                    input,
                    tags,
                    parsed,
                    callback,
                    output
                )
                || input.Name != source.Name || tags.Name != inputTags.Name || returned.Name != output.Name
                || !entries.TryGetValue((match.Function, 2), out int matcher)
                || !TryResolveOrdinaryCallTarget(select, out string? remote) || remote is null)
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, parsed.Name, callback.Name, output.Name };
            if (ReadRequiredPart(
                firstSplit,
                firstRead,
                parsed.Name,
                firstValue,
                bindings
            ) is not { } first
                || ReadSequenceCall(first.Read, first.Payload.Name) is not { } read || key.Name != first.Output.Name
                || ReadRequiredPart(
                    secondSplit,
                    secondRead,
                    first.Rest.Name,
                    secondValue,
                    bindings
                ) is not { } second
                || second.Read is not Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable field, var fieldTags, Expr.Variable useCallback]
                } decode || field.Name != second.Payload.Name || useCallback.Name != callback.Name
                || !AttributeLiteral.TryNormalizeConstant(fieldTags, out Term decoderTags)
                || !entries.TryGetValue((decode.Function, 3), out int decoder)
                || ReadTableUnexpectedFields(extra, second.Rest.Name) is not { } unexpected)
            {
                return false;
            }

            int leftover = Label();
            int malformedRest = Label();
            int malformedInput = Label();
            EmitFrameAllocation(2, 3);
            EmitFrameInitialization(0, 3);
            BeamSourceLocation previous = location;
            EmitLocal(match, matcher, 2);

            CheckList(XRegisterTag, 0, malformedInput);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 2);
            owner.EmitConstant(read.Options, 1);
            EmitLocal(read.Call, read.Entry, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.EmitCallLine(select, previous);
            Call(remote, select.Function, 1);
            previous = CallLocation(select, previous);

            CheckList(YRegisterTag, 2, malformedRest);
            owner.Op(GetList, YRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitConstant(decoderTags, 1);
            EmitFrameInitialization(2, 3);
            EmitLocal(decode, decoder, 3);
            owner.EmitNilTest(YRegisterTag, 0, leftover);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(3);

            Mark(leftover);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Pair(unexpected.UnexpectedTag, YRegisterTag);
            Pair(unexpected.DetailsTag, XRegisterTag);
            Pair(unexpected.ErrorTag, XRegisterTag);
            owner.EmitCallLine(unexpected.Exit, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, unexpected.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 3);
            Mark(malformedRest);
            owner.EmitArithmeticLine(second.Split.SourceLine, ref previous);
            owner.Op(BadMatch, YRegisterTag, 2);
            Mark(malformedInput);
            owner.EmitArithmeticLine(first.Split.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void EmitLocal(Expr.Call call, int entry, int arity)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entry);
            }

            void CheckList(int registerTag, int register, int failure)
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(registerTag, register);
            }

            void Pair(Atom atom, int registerTag)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(atom);
                owner.Operand(registerTag, 0);
            }
        }
    }
}
