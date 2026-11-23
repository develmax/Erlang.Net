using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitInlineTablePairDecoder(FunctionDefinition function)
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
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Variable parameters]
                                } match
                            },
                                var firstSplit, var firstRead, var secondSplit, var secondRead,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable callback,
                                Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key] } select
                            },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable field, Value: Expr.Try invoke
                            },
                                Expr.Case tailCheck,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable output,
                                Value: Expr.Tuple
                                { Items: [Expr.Literal { Value: Atom tag }, var firstMember, Expr.Variable secondMember] }
                            },
                                Expr.Variable returned
                            ]
                        }
                    }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    parsed,
                    callback,
                    field,
                    output
                )
                || input.Name != source.Name || tags.Name != parameters.Name || returned.Name != output.Name
                || secondMember.Name != field.Name || !entries.TryGetValue((match.Function, 2), out int matcher)
                || !TryResolveOrdinaryCallTarget(select, out string? module) || module is null or ErlangModuleNames.Module
                || secondRead is not Expr.Match { Pattern: Pattern.Variable temporary })
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal)
                { input.Name, tags.Name, parsed.Name, callback.Name, field.Name, output.Name };
            if (ReadRequiredPart(
                firstSplit,
                firstRead,
                parsed.Name,
                firstMember,
                bindings
            ) is not { } first
                || ReadSequenceCall(first.Read, first.Payload.Name) is not { } read || key.Name != first.Output.Name
                || ReadRequiredPart(
                    secondSplit,
                    secondRead,
                    first.Rest.Name,
                    new Expr.Variable(temporary.Name),
                    bindings
                ) is not { } second
                || ReadSequenceCall(second.Read, second.Payload.Name) is not { } opened
                || ReadTableDecodeTry(invoke, temporary.Name, callback.Name) is not { } caught
                || ReadTableUnexpectedFields(tailCheck, second.Rest.Name) is not { } unexpected)
            {
                return false;
            }

            const int Frame = 4;
            const int Token = 3;
            int extra = Label();
            int handler = Label();
            int report = Label();
            int reraise = Label();
            int badRest = Label();
            int badInput = Label();
            EmitFrameAllocation(2, Frame);
            EmitFrameInitialization(0, Frame);
            owner.EmitCallLine(match, location);
            owner.EmitFieldCall(2, matcher);
            var previous = CallLocation(match, location);
            Split(
                XRegisterTag,
                0,
                Token,
                badInput
            );
            EmitRead(read);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            Split(
                YRegisterTag,
                Token,
                1,
                badRest
            );
            owner.EmitConstant(opened.Options, 1);
            owner.InitStackSlot(Token);
            owner.EmitCallLine(opened.Call, previous);
            owner.EmitFieldCall(2, opened.Entry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveToX0(YRegisterTag, 2);
            previous = CallLocation(opened.Call, previous);
            owner.EmitCallLine(select, previous);
            Call(module, select.Function, 1);
            previous = CallLocation(select, previous);
            owner.Op(TryEnter, YRegisterTag, Token);
            owner.Operand(LabelTag, handler);
            owner.EmitConstant(caught.Options, 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            owner.InitStackSlot(0);
            owner.EmitConstant(caught.Field, 0);
            owner.EmitArithmeticLine(caught.Apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            owner.Op(TryEnd, YRegisterTag, Token);
            owner.EmitNilTest(YRegisterTag, 1, extra);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(Frame);

            Mark(extra);
            EmitCheckedDecoderTailExit(
                unexpected.Exit,
                [unexpected.ErrorTag, unexpected.DetailsTag, unexpected.UnexpectedTag],
                YRegisterTag,
                1,
                Frame
            );
            EmitTableCallbackCatch(
                caught,
                Token,
                Frame,
                handler,
                report,
                reraise,
                previous
            );
            Mark(badRest);
            previous = location;
            owner.EmitArithmeticLine(second.Split.SourceLine, ref previous);
            owner.Op(BadMatch, YRegisterTag, Token);
            Mark(badInput);
            previous = location;
            owner.EmitArithmeticLine(first.Split.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void Split(
                int register,
                int sourceIndex,
                int remaining,
                int failure
            )
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(register, sourceIndex);
                owner.Op(GetList, register, sourceIndex);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, remaining);
            }

            void EmitRead(SequenceRead plan)
            {
                owner.EmitConstant(plan.Options, 1);
                owner.EmitCallLine(plan.Call, previous);
                owner.EmitFieldCall(2, plan.Entry);
                previous = CallLocation(plan.Call, previous);
            }
        }

        private void EmitTableCallbackCatch(
            TableDecodeCatch caught,
            int token,
            int frame,
            int handler,
            int reported,
            int reraised,
            BeamSourceLocation previous
        )
        {
            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.Operand(LabelTag, reraised);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(AtomTag, owner.Intern(ErlangExceptionClasses.Error));
            owner.Operand(LabelTag, reported);
            owner.Operand(AtomTag, owner.Intern(ErlangExceptionClasses.Exit));
            owner.Operand(LabelTag, reported);
            Mark(reported);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(caught.ErrorTag);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(caught.Exit, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, caught.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(reraised);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
        }
    }
}
