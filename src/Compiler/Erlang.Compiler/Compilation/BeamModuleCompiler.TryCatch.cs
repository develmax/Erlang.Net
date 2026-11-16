using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ProtectedCall(Expr.Call Lookup, Expr.Call Transform, Expr.Call Remote, Atom Success,
        Atom OuterError, Atom InnerError, bool Unpack, bool UnwrapReason);

    private static ProtectedCall? ReadProtectedCall(Clause clause)
    {
        if (clause is not
            {
                Guard: null,
                Patterns: [Pattern.Variable first, Pattern.Variable second],
                Body: Expr.Try
                {
                    Clauses.Count: 0, After: null, Catches: [var handler], Body: var body
                }
            }
            || !HasUnconditionalParameters(clause)
            || handler is not
            {
                Patterns: [Pattern.Tuple { Items: [Pattern.Variable @class, Pattern.Variable reason, Pattern.Variable stack] }],
                Guard: Expr.GuardAlternatives { Items: [var error, var exit] },
                Body: Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom outer }, Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom inner }, Expr.Tuple
                        {
                            Items: [var returnedReason, Expr.Variable returnedStack]
                        }]
                    }]
                }
            }
            || !IsFresh(clause, @class) || !IsFresh(clause, reason) || !IsFresh(clause, stack)
            || @class.Name == reason.Name || @class.Name == stack.Name || reason.Name == stack.Name
            || !MatchesProtectedReason(returnedReason, clause, @class, reason, stack, out bool unwrapReason) || stack.Name != returnedStack.Name
            || !(IsClassTest(error, @class.Name, "error") && IsClassTest(exit, @class.Name, "exit")
                || IsClassTest(error, @class.Name, "exit") && IsClassTest(exit, @class.Name, "error")))
        {
            return null;
        }

        if (body is Expr.Tuple
            {
                Items: [Expr.Literal { Value: Atom success }, Expr.Call { Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Call { Module: null, Arguments: [
                        Expr.Call { Module: null, Arguments: [Expr.Variable type] } lookup,
                        Expr.Variable value] } transform] } remote]
            } && remote.Module != ErlangModuleNames.Module && type.Name == first.Name && value.Name == second.Name)
        {
            return new(lookup, transform, remote, success, outer, inner, false, unwrapReason);
        }

        if (body is Expr.Sequence
            {
                Items:
                [
                    Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable payload, Pattern.Variable rest] },
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable data] } read
                    },
                    Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom ok }, Expr.Call { Module: null, Arguments: [
                            Expr.Call { Module: null, Arguments: [Expr.Variable input] } shape,
                            Expr.Variable used] } convert]
                    }
                ]
            } && read.Module != ErlangModuleNames.Module && data.Name == second.Name && input.Name == first.Name
            && payload.Name == used.Name && IsFresh(clause, payload)
            && (rest.Name == VariableScopeNames.Wildcard || IsFresh(clause, rest) && rest.Name != payload.Name))
        {
            return new(shape, convert, read, ok, outer, inner, true, unwrapReason);
        }

        return null;
    }

    private static bool MatchesProtectedReason(Expr expression, Clause clause, Pattern.Variable exceptionClass, Pattern.Variable reason, Pattern.Variable stack, out bool unwrap)
    {
        unwrap = expression is Expr.Case
        {
            Value: Expr.Variable input,
            Clauses: [
                { Guard: null, Patterns: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom { Name: "error" } }, Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom { Name: "asn1" } }, Pattern.Variable inner] }] }],
                    Body: Expr.Variable payload },
                { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Variable original }
            ]
        } && input.Name == reason.Name && original.Name == reason.Name && inner.Name == payload.Name
            && IsFresh(clause, inner) && inner.Name != reason.Name && inner.Name != exceptionClass.Name && inner.Name != stack.Name;

        return unwrap || expression is Expr.Variable variable && variable.Name == reason.Name;
    }

    private static bool IsClassTest(Expr expression, string variable, string atom) =>
        expression is Expr.Binary
        {
            Operator: ErlangOperators.ExactEqual, Left: Expr.Variable left, Right: Expr.Literal { Value: Atom right }
        } && left.Name == variable && right.Name == atom;

    private static bool ReadWrappedCatch(Clause function, Clause handler)
    {
        if (handler is not
            {
                Patterns: [Pattern.Tuple { Items: [Pattern.Variable kind, Pattern.Variable reason, Pattern.Variable stack] }],
                Guard: Expr.GuardAlternatives { Items: [var first, var second] },
                Body: Expr.Case
                {
                    Value: Expr.Variable input,
                    Clauses:
                    [
                        { Guard: null, Patterns: [Pattern.Tuple { Items:
                            [Pattern.Literal { Value: Atom { Name: "error" } }, Pattern.Tuple { Items:
                                [Pattern.Literal { Value: Atom { Name: "asn1" } }, Pattern.Variable inner] }] }],
                          Body: var unwrapped },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: var wrapped }
                    ]
                }
            }
            || !IsFresh(function, kind) || !IsFresh(function, reason) || !IsFresh(function, stack)
            || !IsFresh(function, inner) || !IsFresh(function, fallback)
            || !DistinctBindings(kind, reason, stack, inner)
            || fallback.Name == kind.Name || fallback.Name == reason.Name || fallback.Name == stack.Name
            || input.Name != reason.Name
            || !(IsClassTest(first, kind.Name, "error") && IsClassTest(second, kind.Name, "exit")
                || IsClassTest(first, kind.Name, "exit") && IsClassTest(second, kind.Name, "error")))
        {
            return false;
        }

        return IsWrappedCatchResult(unwrapped, inner.Name, stack.Name)
            && IsWrappedCatchResult(wrapped, fallback.Name, stack.Name);
    }

    private static bool IsWrappedCatchResult(Expr result, string reason, string stack) =>
        result is Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "error" } },
            Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "asn1" } },
                Expr.Tuple { Items: [Expr.Variable payload, Expr.Variable trace] }] }] }
        && payload.Name == reason && trace.Name == stack;

    private void EmitWrappedCatch(int handler, int slot)
    {
        int caught = nextLabel++;
        int wrap = nextLabel++;
        int propagate = nextLabel++;
        Op(Label, BeamWire.UnsignedTag, handler);
        Op(TryCase, YRegisterTag, slot);
        MoveRegister(XRegisterTag, 1, YRegisterTag, slot);
        instructions.WriteByte(SelectValue);
        EmitTypedRegister(0, AtomTypeBits);
        Operand(LabelTag, propagate);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 4);
        Operand(AtomTag, Intern("error"));
        Operand(LabelTag, caught);
        Operand(AtomTag, Intern("exit"));
        Operand(LabelTag, caught);

        Op(Label, BeamWire.UnsignedTag, caught);
        EmitCatchTagTest(1, "error", wrap);
        EmitTuplePart(1, 1, 0);
        EmitCatchTagTest(0, "asn1", wrap);
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(YRegisterTag, slot);
        EmitWrappedCatchReturn(slot);
        Op(Label, BeamWire.UnsignedTag, wrap);
        EmitWrappedCatchReturn(slot);

        Op(Label, BeamWire.UnsignedTag, propagate);
        Op(Raise, XRegisterTag, 2);
        Operand(XRegisterTag, 1);
    }

    private void EmitCatchTagTest(int register, string tag, int failure)
    {
        Op(IsTaggedTuple, LabelTag, failure);
        Operand(XRegisterTag, register);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern(tag));
    }

    private void EmitWrappedCatchReturn(int slot)
    {
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        instructions.WriteByte(BuildStacktrace);
        Op(TestHeap, BeamWire.UnsignedTag, 9);
        Operand(BeamWire.UnsignedTag, 1);
        EmitProtectedPair(YRegisterTag, slot);
        EmitProtectedPair(AtomTag, Intern("asn1"));
        EmitProtectedPair(AtomTag, Intern("error"));
        EmitFrameReturn(slot + 1);
    }

    private void EmitProtectedCall(
        ProtectedCall flow,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((flow.Lookup.Function, 1), out int lookup)
            || !entries.TryGetValue((flow.Transform.Function, 2), out int transform))
        {
            throw new NotSupportedException(Unsupported);
        }

        int handler = nextLabel++;
        int caught = nextLabel++;
        int wrap = flow.UnwrapReason ? nextLabel++ : 0;
        int propagate = nextLabel++;
        int badMatch = flow.Unpack ? nextLabel++ : 0;
        int frame = flow.Unpack ? 3 : 2;
        int token = frame - 1;
        Op(Allocate, BeamWire.UnsignedTag, frame);
        Operand(BeamWire.UnsignedTag, 2);
        if (flow.Unpack)
        {
            InitStackSlot(0);
            MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
        }
        else
        {
            MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
        }
        Op(TryEnter, YRegisterTag, token);
        Operand(LabelTag, handler);

        if (flow.Unpack)
        {
            MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            EmitCallLine(flow.Remote, location);
            EmitProtectedRemote(flow.Remote);
            MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            Op(IsTuple, LabelTag, badMatch);
            Operand(XRegisterTag, 0);
            Op(TestArity, LabelTag, badMatch);
            Operand(XRegisterTag, 0);
            Operand(BeamWire.UnsignedTag, 2);
            MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
            InitStackSlot(1);
            EmitCallLine(flow.Lookup, CallLocation(flow.Remote, location));
            Op(LocalCall, BeamWire.UnsignedTag, 1);
            Operand(LabelTag, lookup);
            Op(GetTupleElement, YRegisterTag, 0);
            Operand(BeamWire.UnsignedTag, 0);
            Operand(XRegisterTag, 1);
            InitStackSlot(0);
            EmitCallLine(flow.Transform, CallLocation(flow.Lookup, location));
        }
        else
        {
            EmitCallLine(flow.Lookup, location);
            Op(LocalCall, BeamWire.UnsignedTag, 1);
            Operand(LabelTag, lookup);
            MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
            InitStackSlot(0);
            EmitCallLine(flow.Transform, CallLocation(flow.Lookup, location));
        }

        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, transform);
        if (!flow.Unpack)
        {
            EmitCallLine(flow.Remote, CallLocation(flow.Transform, location));
            EmitProtectedRemote(flow.Remote);
        }

        Op(TryEnd, YRegisterTag, token);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitProtectedPair(AtomTag, Intern(flow.Success.Name));
        EmitFrameReturn(frame);

        Op(Label, BeamWire.UnsignedTag, handler);
        Op(TryCase, YRegisterTag, token);
        MoveRegister(XRegisterTag, 1, YRegisterTag, token);
        instructions.WriteByte(SelectValue);
        EmitTypedRegister(0, AtomTypeBits);
        Operand(LabelTag, propagate);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 4);
        Operand(AtomTag, Intern("error"));
        Operand(LabelTag, caught);
        Operand(AtomTag, Intern("exit"));
        Operand(LabelTag, caught);

        Op(Label, BeamWire.UnsignedTag, caught);
        if (flow.UnwrapReason)
        {
            MoveRegister(YRegisterTag, token, XRegisterTag, 3);
            EmitTupleCheck(3, 2, wrap);
            EmitTuplePart(3, 0, 4);
            EmitAtomTest(4, "error", wrap);
            EmitTuplePart(3, 1, 3);
            EmitTupleCheck(3, 2, wrap);
            EmitTuplePart(3, 0, 4);
            EmitAtomTest(4, "asn1", wrap);
            Op(GetTupleElement, XRegisterTag, 3);
            Operand(BeamWire.UnsignedTag, 1);
            Operand(YRegisterTag, token);
            Op(Label, BeamWire.UnsignedTag, wrap);
        }
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        instructions.WriteByte(BuildStacktrace);
        Op(TestHeap, BeamWire.UnsignedTag, 9);
        Operand(BeamWire.UnsignedTag, 1);
        EmitProtectedPair(YRegisterTag, token);
        EmitProtectedPair(AtomTag, Intern(flow.InnerError.Name));
        EmitProtectedPair(AtomTag, Intern(flow.OuterError.Name));
        EmitFrameReturn(frame);

        Op(Label, BeamWire.UnsignedTag, propagate);
        Op(Raise, XRegisterTag, 2);
        Operand(XRegisterTag, 1);
        if (flow.Unpack)
        {
            Op(Label, BeamWire.UnsignedTag, badMatch);
            EmitCallLine(flow.Remote, CallLocation(flow.Transform, location));
            Op(BadMatch, XRegisterTag, 0);
        }
    }

    private void EmitProtectedRemote(Expr.Call call)
    {
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, 1));
    }

    private void InitStackSlot(int index)
    {
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(YRegisterTag, index);
    }

    private void EmitProtectedPair(int tag, int value)
    {
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(tag, value);
        Operand(XRegisterTag, 0);
    }
}
