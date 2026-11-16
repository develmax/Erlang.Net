using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConditionalProgressPulse(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable id],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr.Tuple
                            {
                                Items: [
                            Expr.Literal tag, Expr.Binary
                                {
                                    Operator: ErlangOperators.Append,
                                    Left: Expr.Literal prefix, Right: Expr.Variable argument
                                } append]
                            }]
                        } report,
                        var begin]
                    }
                },
                { Guard: null, Patterns: [Pattern.Variable _, Pattern.Variable _], Body: var fallback } otherwise
            ] || !HasUnconditionalParameters(otherwise) || !DistinctBindings(id) || argument.Name != id.Name
                || ReadClosureProgress(begin) is not { } pulse
                || !AttributeLiteral.TryNormalizeConstant(fallback, out Term returned))
            {
                return false;
            }

            int skip = Label();
            owner.EmitAtomTest(0, key.Name, skip);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitConstant(prefix.Value, 0);
            var previous = location;
            owner.EmitArithmeticLine(append.SourceLine, ref previous);
            EmitAppend();
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(tag.Value);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(report, previous);
            owner.EmitStaticRemoteCall(report);
            owner.EmitConstant(pulse.Argument, 0);
            owner.EmitCallLine(pulse.Call, CallLocation(report, previous));
            owner.EmitTailRemote(pulse.Call.Module!, pulse.Call.Function, framed: true);
            Mark(skip);
            owner.EmitConstant(returned, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitDynamicDetailReply(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Tail: null, Items: [
                    Pattern.Variable id, Pattern.Variable data, Pattern.Variable frame, Pattern.Variable callback,
                    Pattern.Variable app, Pattern.Variable parent]
                    }], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Call { Module: null } progress, Expr.Case
                        {
                            Value: Expr.Call
                            {
                                DynamicRemote: true, Arguments: [
                        Expr.Variable target, Expr.Literal { Value: Atom member },
                        Expr.List { Tail: null, Items: [Expr.Variable key, Expr.Variable payload] }]
                            } invoke,
                            Clauses: [var success, var question, var info]
                        } response]
                    }
                }
            ] || !DistinctBindings(
                id,
                data,
                frame,
                callback,
                app,
                parent
            )
                || target.Name != callback.Name || key.Name != id.Name || payload.Name != data.Name
                || !MatchesDetailArguments(progress, frame, app)
                || success is not
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom successTag }, Pattern.Variable details]
                    }],
                    Body: Expr.Sequence { Items: [Expr.Call { Module: null } pulse, Expr.Call { Module: null } open] }
                }
                || !DistinctBindings(
                    id,
                    data,
                    frame,
                    callback,
                    app,
                    parent,
                    details
                )
                || !MatchesDetailArguments(pulse, callback, id) || !MatchesDetailArguments(
                    open,
                    id,
                    frame,
                    callback,
                    app,
                    parent,
                    details
                )
                || question is not
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom questionTag }, Pattern.Variable prompt, Pattern.Variable action]
                    }],
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Call { Module: null } close, Expr.Case
                        {
                            Value: Expr.Call
                            { Module: not null, DynamicRemote: false } ask, Clauses: [
                            {
                                Guard: null, Patterns: [Pattern.Literal { Value: Integer yes }], Body: Expr.Apply
                                { Function: Expr.Variable selected, Arguments: [] } apply
                            },
                            { Guard: null, Patterns: [Pattern.Literal { Value: Integer no }], Body: Expr.Literal }]
                        } dialog, var stop]
                    }
                }
                || !DistinctBindings(
                    id,
                    data,
                    frame,
                    callback,
                    app,
                    parent,
                    prompt,
                    action
                )
                || selected.Name != action.Name || yes.Value >= no.Value || !MatchesDetailArguments(close, app)
                || !MatchesDetailArguments(ask, prompt)
                || info is not
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom infoTag }, Pattern.Variable message]
                    }],
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Call { Module: null } dismiss,
                        Expr.Call { Module: not null, DynamicRemote: false } display, var stopInfo]
                    }
                }
                || !DistinctBindings(
                    id,
                    data,
                    frame,
                    callback,
                    app,
                    parent,
                    message
                ) || successTag.Equals(infoTag)
                || !MatchesDetailArguments(dismiss, app) || !MatchesDetailArguments(display, frame, message)
                || !AttributeLiteral.TryNormalizeConstant(stop, out Term stopped)
                || !AttributeLiteral.TryNormalizeConstant(stopInfo, out Term stoppedInfo) || !stopped.Equals(stoppedInfo)
                || !entries.TryGetValue((progress.Function, 2), out int progressEntry)
                || !entries.TryGetValue((pulse.Function, 2), out int pulseEntry)
                || !entries.TryGetValue((open.Function, 6), out int openEntry)
                || !entries.TryGetValue((close.Function, 1), out int closeEntry)
                || !entries.TryGetValue((dismiss.Function, 1), out int dismissEntry))
            {
                return false;
            }

            int invalidArgs = owner.nextLabel - 2;
            int questionLabel = Label();
            int yesLabel = Label();
            int stopLabel = Label();
            int pairLabel = Label();
            int successLabel = Label();
            int infoLabel = Label();
            int invalidDialog = Label();
            int invalidReply = Label();
            int[] arguments = [1, 3, 4, 5, 6, 7];
            int list = 0;
            foreach (int register in arguments)
            {
                owner.EmitNonemptyList(list, invalidArgs);
                owner.EmitListParts(
                    list,
                    XRegisterTag,
                    register,
                    XRegisterTag,
                    2
                );
                list = 2;
            }
            owner.EmitNilTest(XRegisterTag, 2, invalidArgs);
            owner.Op(Allocate, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.InitStackSlot(0);
            for (int index = arguments.Length - 1; index >= 0; index--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    arguments[index],
                    YRegisterTag,
                    arguments.Length - index
                );
            }
            owner.MoveRegister(
                XRegisterTag,
                6,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                4,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(progress, location);
            owner.EmitFieldCall(2, progressEntry);
            Load(3, 2);
            Load(5, 1);
            owner.EmitConstant(member, 3);
            owner.InitStackSlot(5);
            Load(6);
            var progressLocation = CallLocation(progress, location);
            owner.EmitCallLine(invoke, progressLocation);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                5
            );
            owner.EmitTupleArityDispatch(invalidReply, [(2, pairLabel), (3, questionLabel)]);
            var invokeLocation = CallLocation(invoke, progressLocation);

            Mark(questionLabel);
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitAtomTest(0, questionTag.Name, invalidReply);
            Load(2);
            ClearDetailSlots(
                1,
                2,
                3,
                4,
                6
            );
            owner.EmitCallLine(close, invokeLocation);
            owner.EmitFieldCall(1, closeEntry);
            ReadSavedDetail(1, XRegisterTag, 0);
            var closeLocation = CallLocation(close, invokeLocation);
            owner.EmitCallLine(ask, closeLocation);
            owner.EmitStaticRemoteCall(ask);
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, invalidDialog);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(yes);
            owner.Operand(LabelTag, yesLabel);
            owner.LiteralOperand(no);
            owner.Operand(LabelTag, stopLabel);
            Mark(yesLabel);
            ReadSavedDetail(2, XRegisterTag, 0);
            owner.InitStackSlot(5);
            var askLocation = CallLocation(ask, closeLocation);
            owner.EmitArithmeticLine(apply.SourceLine, ref askLocation);
            owner.Op(CallFun, BeamWire.UnsignedTag, 0);
            Mark(stopLabel);
            owner.EmitConstant(stopped, 0);
            owner.EmitFrameReturn(7);

            Mark(pairLabel);
            owner.EmitTuplePart(0, 0, 0);
            ReadSavedDetail(1, YRegisterTag, 0);
            var tags = new[] { (Tag: infoTag, Label: infoLabel), (Tag: successTag, Label: successLabel) }
                .OrderBy(pair => pair.Tag, Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))).ToArray();
            owner.EmitAtomSelection(
                0,
                invalidReply,
                tags[0].Tag.Name,
                tags[0].Label,
                tags[1].Tag.Name,
                tags[1].Label
            );
            Mark(successLabel);
            Load(6, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                5
            );
            TrimDetailFrame(1, 6);
            Load(2);
            owner.EmitCallLine(pulse, invokeLocation);
            owner.EmitFieldCall(2, pulseEntry);
            Load(4, 5);
            Load(0, 4);
            Load(1, 3);
            Load(2, 2);
            Load(3, 1);
            Load(5);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 6);
            owner.Operand(LabelTag, openEntry);
            owner.Operand(BeamWire.UnsignedTag, 6);

            Mark(infoLabel);
            Load(2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                6
            );
            owner.MoveRegister(
                YRegisterTag,
                4,
                YRegisterTag,
                5
            );
            TrimDetailFrame(5, 2);
            owner.EmitCallLine(dismiss, invokeLocation);
            owner.EmitFieldCall(1, dismissEntry);
            Load(1, 1);
            Load(0);
            TrimDetailFrame(2, 0);
            owner.EmitCallLine(display, CallLocation(dismiss, invokeLocation));
            owner.EmitStaticRemoteCall(display);
            owner.EmitConstant(stopped, 0);
            owner.EmitFrameReturn();
            Mark(invalidDialog);
            owner.EmitLine(dialog.SourceLine == 0 ? CallLocation(ask, closeLocation) : new(dialog.PreprocessorFile, dialog.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(invalidReply);
            owner.EmitLine(response.SourceLine == 0 ? invokeLocation : new(response.PreprocessorFile, response.SourceLine));
            owner.Op(CaseEnd, YRegisterTag, 5);

            return true;
        }

        private static bool MatchesDetailArguments(Expr.Call call, params Pattern.Variable[] parameters) =>
            call.Arguments.Count == parameters.Length && call.Arguments.Zip(parameters)
                .All(pair => pair.First is Expr.Variable variable && variable.Name == pair.Second.Name);

        private void ClearDetailSlots(params int[] slots)
        {
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, slots.Length);
            foreach (int slot in slots)
            {
                owner.Operand(YRegisterTag, slot);
            }
        }

        private void ReadSavedDetail(int field, int destinationTag, int destination)
        {
            owner.Op(GetTupleElement, YRegisterTag, 5);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(destinationTag, destination);
        }

        private void TrimDetailFrame(int drop, int keep)
        {
            owner.Op(TrimFrame, BeamWire.UnsignedTag, drop);
            owner.Operand(BeamWire.UnsignedTag, keep);
        }
    }
}
