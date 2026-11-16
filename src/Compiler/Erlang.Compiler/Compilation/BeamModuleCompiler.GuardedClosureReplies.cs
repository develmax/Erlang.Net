using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record GuardedBinaryReply(
        Expr.Call Call,
        Atom Tag,
        Expr.Call Convert,
        string Payload,
        string Fallback
    );

    private sealed record GuardedLocalReply(
        Expr.Call Call,
        Atom Tag,
        Expr.Call Tail,
        string Payload,
        string Fallback
    );

    private static GuardedLocalReply? ReadGuardedLocalReply(Expr expression)
    {
        if (expression is not Expr.Case
        {
            Value: Expr.Call { Module: not null and not ErlangModuleNames.Module, DynamicRemote: false } call,
            Clauses: [
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                    Guard: null,
                    Body: Expr.Call { Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable forwarded, ..] } tail
                },
                { Patterns: [Pattern.Variable other], Guard: null, Body: Expr.Variable returned }
            ]
        } || payload.Name == VariableScopeNames.Wildcard || other.Name == VariableScopeNames.Wildcard
            || payload.Name != forwarded.Name || other.Name != returned.Name
            || !call.Arguments.All(argument => argument is Expr.Variable || AttributeLiteral.TryNormalizeConstant(argument, out _))
            || !tail.Arguments.Skip(1).All(argument => AttributeLiteral.TryNormalizeConstant(argument, out _)))
        {
            return null;
        }

        return new(call, tag, tail, payload.Name, other.Name);
    }

    private static (Expr.Call Outer, Expr.Call Inner, Expr.Variable Receiver)? ReadGuardedListCall(Expr expression) =>
        expression is Expr.Call
        {
            Module: not null and not ErlangModuleNames.Module,
            DynamicRemote: false,
            Arguments: [
                Expr.Variable receiver,
                Expr.List
                {
                    Tail: null,
                    Items: [Expr.Call
                    {
                        Module: not null and not ErlangModuleNames.Module,
                        DynamicRemote: false,
                        Arguments: []
                    } inner]
                }
            ]
        } outer ? (outer, inner, receiver) : null;

    private GuardedBinaryReply? ReadGuardedBinaryReply(Expr expression)
    {
        if (expression is not Expr.Case
        {
            Value: Expr.Call { Module: not null and not ErlangModuleNames.Module, DynamicRemote: false } call,
            Clauses: [
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                    Guard: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsBinary, Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.BinaryToList, Arguments: [Expr.Variable converted]
                    } conversion] }
                },
                { Patterns: [Pattern.Variable other], Guard: null, Body: Expr.Variable returned }
            ]
        } || payload.Name == VariableScopeNames.Wildcard || other.Name == VariableScopeNames.Wildcard
            || tested.Name != payload.Name || converted.Name != payload.Name || other.Name != returned.Name
            || !tag.Equals(resultTag) || !ResolvesErlangBif(guard) || !ResolvesErlangBif(conversion)
            || !call.Arguments.All(IsClosureTupleOperand))
        {
            return null;
        }

        return new(call, tag, conversion, payload.Name, other.Name);
    }

    private void EmitGuardedListCall(
        Clause body,
        Expr.Call outer,
        Expr.Call inner,
        Expr.Variable receiver,
        BeamSourceLocation location
    )
    {
        int register = ArgumentRegister(body, receiver.Name);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, register + 1);
        MoveRegister(XRegisterTag, register, YRegisterTag, 0);
        EmitCallLine(inner, location);
        EmitStaticRemoteCall(inner);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, XRegisterTag, 0);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, 1);
        MoveToX0(YRegisterTag, 0);
        EmitCallLine(outer, CallLocation(inner, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(outer.Module!, outer.Function, 2));
        Operand(BeamWire.UnsignedTag, 1);
    }

    private void EmitGuardedBinaryReply(
        Clause body,
        GuardedBinaryReply reply,
        int fallback,
        BeamSourceLocation location
    )
    {
        EmitGuardedReplyCall(body, reply.Call, reply.Tag, fallback, location);
        EmitTuplePart(0, 1, 1);
        Op(IsBinary, LabelTag, fallback);
        Operand(XRegisterTag, 1);
        MoveToX0(XRegisterTag, 1);
        EmitCallLine(reply.Convert, CallLocation(reply.Call, location));
        EmitStaticRemoteCall(reply.Convert with { Module = ErlangModuleNames.Module });
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(0, (AtomTag, Intern(reply.Tag.Name)), (XRegisterTag, 0));
        Op(Deallocate, BeamWire.UnsignedTag, 0);
        instructions.WriteByte(Return);
        EmitGuardedReplyFallback(fallback);
    }

    private void EmitGuardedLocalReply(
        Clause body,
        GuardedLocalReply reply,
        int fallback,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        int arity = reply.Tail.Arguments.Count;
        if (!entries.TryGetValue((reply.Tail.Function, arity), out int target))
        {
            throw new NotSupportedException(Unsupported);
        }

        EmitGuardedReplyCall(body, reply.Call, reply.Tag, fallback, location);
        EmitTuplePart(0, 1, 0);
        var constants = new List<(int Destination, Term Value)>();
        for (int index = 1; index < arity; index++)
        {
            AttributeLiteral.TryNormalizeConstant(reply.Tail.Arguments[index], out Term value);
            constants.Add((index, value));
        }
        EmitIndependentCallMoves(constants);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, arity);
        Operand(LabelTag, target);
        Operand(BeamWire.UnsignedTag, 0);
        EmitGuardedReplyFallback(fallback);
    }

    private void EmitGuardedReplyCall(Clause body, Expr.Call call, Atom tag, int fallback, BeamSourceLocation location)
    {
        int live = call.Arguments.OfType<Expr.Variable>()
            .Select(argument => ArgumentRegister(body, argument.Name) + 1)
            .DefaultIfEmpty(0)
            .Max();
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, live);
        EmitGuardedCallArguments(body, call);
        EmitCallLine(call, location);
        EmitStaticRemoteCall(call);
        Op(IsTaggedTuple, LabelTag, fallback);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        LiteralOperand(tag);
    }

    private void EmitGuardedReplyFallback(int fallback)
    {
        Op(Label, BeamWire.UnsignedTag, fallback);
        Op(Deallocate, BeamWire.UnsignedTag, 0);
        instructions.WriteByte(Return);
    }
}
