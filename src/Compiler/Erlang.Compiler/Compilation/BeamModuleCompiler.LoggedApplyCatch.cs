using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitLoggedApplyCatch(
        Clause clause,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (clause is not
            {
                Guard: null,
                Patterns: [Pattern.Variable who, Pattern.Variable module, Pattern.Variable function, Pattern.Variable arguments],
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.Call
                    {
                        Module: not null, DynamicRemote: false, Arguments: [
                        var message, Expr.List
                        {
                            Tail: null, Items: [
                            Expr.Variable logged, Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module,
                                Function: AutoImportNames.Self, DynamicRemote: false, Arguments: []
                            } self
                        ]
                        }
                    ]
                    } log,
                    Expr.Try
                    {
                        Body: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Apply,
                            DynamicRemote: false, Arguments: [Expr.Variable mod, Expr.Variable fun, Expr.Variable args]
                        } apply,
                        Clauses: [], After: null, Catches: [var narrow, var wide]
                    }
                ]
                }
            } || !DistinctBindings(
                who,
                module,
                function,
                arguments
            )
            || logged.Name != who.Name || mod.Name != module.Name || fun.Name != function.Name || args.Name != arguments.Name
            || self.Module is null && entries.ContainsKey((AutoImportNames.Self, 0))
            || apply.Module is null && entries.ContainsKey((AutoImportNames.Apply, 3))
            || !AttributeLiteral.TryNormalizeConstant(message, out Term logMessage)
            || narrow is not
            {
                Patterns: [Pattern.Tuple
                {
                    Items: [
                    Pattern.Literal { Value: Atom { Name: ErlangExceptionClasses.Exit } },
                    Pattern.Variable reason, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ]
                }],
                Guard: Expr.GuardAlternatives { Items.Count: > 1 } guard
            }
            || wide is not
            {
                Patterns: [Pattern.Tuple
                {
                    Items: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable other, Pattern.Variable stack
                ]
                }],
                Guard: null
            }
            || !IsFresh(clause, reason) || !IsFresh(clause, other) || !IsFresh(clause, stack) || !DistinctBindings(other, stack)
            || ReadExitNotification(
                narrow.Body,
                who,
                arguments,
                reason,
                null,
                entries
            ) is not { } notify
            || ReadExitNotification(
                wide.Body,
                who,
                arguments,
                other,
                stack,
                entries
            ) is not { } notifyStack)
        {
            return false;
        }

        var reasons = new HashSet<Atom>();
        foreach (Expr alternative in guard.Items)
        {
            if (alternative is not Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable tested, Right: Expr.Literal { Value: Atom value }
                }
                || tested.Name != reason.Name || !reasons.Add(value))
            {
                return false;
            }
        }

        const int frame = 5;
        const int token = frame - 1;
        int handler = nextLabel++;
        int selected = nextLabel++;
        int fallback = nextLabel++;
        Op(AllocateHeap, BeamWire.UnsignedTag, frame);
        Operand(BeamWire.UnsignedTag, 2 * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, clause.Patterns.Count);
        InitStackSlot(token);
        for (int index = 0; index < clause.Patterns.Count; index++)
        {
            MoveRegister(
                XRegisterTag,
                clause.Patterns.Count - 1 - index,
                YRegisterTag,
                index
            );
        }
        Op(BifZero, BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
        Operand(XRegisterTag, 0);
        Op(PutList, XRegisterTag, 0);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, 0);
        Op(PutList, YRegisterTag, 3);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        EmitConstant(logMessage, 0);
        EmitCallLine(log, location);
        EmitStaticRemoteCall(log);

        Op(TryEnter, YRegisterTag, token);
        Operand(LabelTag, handler);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            2
        );
        MoveRegister(
            YRegisterTag,
            1,
            XRegisterTag,
            1
        );
        MoveRegister(
            YRegisterTag,
            2,
            XRegisterTag,
            0
        );
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, 1);
        Operand(YRegisterTag, 2);
        var logLocation = CallLocation(log, location);
        EmitCallLine(apply, logLocation);
        Op(ExternalCall, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Apply, 3));
        Op(TryEnd, YRegisterTag, token);
        EmitFrameReturn(frame);

        Op(Label, BeamWire.UnsignedTag, handler);
        Op(TryCase, YRegisterTag, token);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            token
        );
        Op(ExactEqualTest, LabelTag, fallback);
        EmitTypedRegister(0, AtomTypeBits);
        Operand(AtomTag, Intern(ErlangExceptionClasses.Exit));
        Op(SelectValue, XRegisterTag, 1);
        Operand(LabelTag, fallback);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, reasons.Count * 2);
        foreach (Atom value in reasons.Order(Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))))
        {
            LiteralOperand(value);
            Operand(LabelTag, selected);
        }

        Op(Label, BeamWire.UnsignedTag, selected);
        EmitExitNotification(notify, CallLocation(apply, logLocation), token);
        Op(Label, BeamWire.UnsignedTag, fallback);
        MoveRegister(
            XRegisterTag,
            2,
            XRegisterTag,
            0
        );
        instructions.WriteByte(BuildStacktrace);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(1, (YRegisterTag, token), (XRegisterTag, 0));
        EmitExitNotification(notifyStack, CallLocation(notify.Exit, location), token);

        return true;
    }

    private static (Expr.Call Notify, Expr.Call Exit)? ReadExitNotification(
        Expr body,
        Pattern.Variable who,
        Pattern.Variable arguments,
        Pattern.Variable reason,
        Pattern.Variable? stack,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (body is not Expr.Sequence
            {
                Items: [
            Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable owner, var value, Expr.Variable args] } notify,
            Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Exit,
                    DynamicRemote: false, Arguments: [Expr.Variable exited]
                } exit
        ]
            } || owner.Name != who.Name || args.Name != arguments.Name || exited.Name != reason.Name
            || exit.Module is null && entries.ContainsKey((AutoImportNames.Exit, 1)))
        {
            return null;
        }

        bool matches = stack is null
            ? value is Expr.Variable plain && plain.Name == reason.Name
            : value is Expr.Tuple { Items: [Expr.Variable wrapped, Expr.Variable trace] }
                && wrapped.Name == reason.Name && trace.Name == stack.Name;

        return matches ? (notify, exit) : null;
    }

    private void EmitExitNotification((Expr.Call Notify, Expr.Call Exit) flow, BeamSourceLocation previous, int trim)
    {
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            2
        );
        MoveRegister(
            YRegisterTag,
            3,
            XRegisterTag,
            0
        );
        Op(TrimFrame, BeamWire.UnsignedTag, trim);
        Operand(BeamWire.UnsignedTag, 1);
        EmitCallLine(flow.Notify, previous);
        EmitStaticRemoteCall(flow.Notify);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        EmitCallLine(flow.Exit, CallLocation(flow.Notify, previous));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Exit, 1));
        Operand(BeamWire.UnsignedTag, 1);
    }

    private void EmitStaticRemoteCall(Expr.Call call)
    {
        Op(ExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, call.Arguments.Count));
    }
}
