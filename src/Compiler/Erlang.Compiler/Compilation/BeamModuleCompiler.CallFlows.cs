using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Compiler.BeamContainerWriter;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int TupleTypeBits = 0x0800;
    private const int AtomTypeBits = 0x0001;
    private const int PidTypeBits = 0x0100;
    private const int FunTypeBits = 0x0010;
    private const int TypeUnitFlag = 1 << 15;
    private const int AnyTypeBits = 0x1fff;
    private readonly List<(int Bits, long? Lower, long? Upper, int? Unit)> typeBits = [(AnyTypeBits, null, null, null)];

    private sealed record CallCase(Expr.Call Input, Atom TupleTag, Atom OtherTag, Term OtherResult);
    private sealed record PreservedCallCase(Atom ArgumentTag, Expr.Call Input, Atom ResultTag, Term Fallback);

    private static PreservedCallCase? ReadPreservedCallCase(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom argumentTag }, Pattern.Variable argument],
                Body: Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable used] } call,
                    Clauses: [var success, var failure] } } first,
            { Guard: null, Patterns: [Pattern.Variable firstFallback, Pattern.Variable secondFallback] } fallback
        ] || call.Module == ErlangModuleNames.Module
            || argument.Name == VariableScopeNames.Wildcard || argument.Name != used.Name
            || firstFallback.Name == secondFallback.Name && firstFallback.Name != VariableScopeNames.Wildcard
            || success is not { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom resultTag }, Pattern.Variable value] }],
                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedTag }, Expr.Variable returned,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom savedTag }, Expr.Variable saved] }] } }
            || !IsFresh(first, value) || resultTag != returnedTag || value.Name != returned.Name
            || argumentTag != savedTag || argument.Name != saved.Name
            || failure is not { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returnedError }
            || !IsFresh(first, error) || error.Name != returnedError.Name
            || !AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term constant))
        {
            return null;
        }

        return new(argumentTag, call, resultTag, constant);
    }

    private void EmitPreservedCallCase(PreservedCallCase flow, BeamSourceLocation location)
    {
        int error = nextLabel++;
        int fallback = nextLabel++;
        Op(ExactEqualTest, LabelTag, fallback);
        Operand(XRegisterTag, 0);
        Operand(AtomTag, Intern(flow.ArgumentTag.Name));
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        Op(Move, XRegisterTag, 1);
        Operand(YRegisterTag, 0);
        Op(Move, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        EmitCallLine(flow.Input, location);
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(flow.Input.Module!, flow.Input.Function, 1));
        Op(IsTaggedTuple, LabelTag, error);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern(flow.ResultTag.Name));
        Op(TestHeap, BeamWire.UnsignedTag, 7);
        Operand(BeamWire.UnsignedTag, 1);
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 0);
        Op(PutTuple, XRegisterTag, 1);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern(flow.ArgumentTag.Name));
        Operand(YRegisterTag, 0);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 3);
        Operand(AtomTag, Intern(flow.ResultTag.Name));
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        EmitFrameReturn(1);
        Op(Label, BeamWire.UnsignedTag, error);
        EmitFrameReturn(1);
        Op(Label, BeamWire.UnsignedTag, fallback);
        EmitConstant(flow.Fallback, 0);
        instructions.WriteByte(Return);
    }

    private sealed record WrappedCallCase(Expr.Call Input, Atom Tag, Term LastElement);

    private static WrappedCallCase? ReadWrappedCallCase(Clause clause)
    {
        if (clause.Body is not Expr.Case
            {
                Value: Expr.Call { Module: not null, DynamicRemote: false } call,
                Clauses: [var success, var fallback]
            }
            || call.Module == ErlangModuleNames.Module
            || success is not
            {
                Guard: null,
                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable value] }],
                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedTag }, Expr.Variable returned, var last] }
            }
            || tag != returnedTag || value.Name != returned.Name || !IsFresh(clause, value)
            || fallback is not { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returnedError }
            || error.Name != returnedError.Name || !IsFresh(clause, error)
            || !AttributeLiteral.TryNormalizeConstant(last, out Term constant))
        {
            return null;
        }

        return new(call, tag, constant);
    }

    private void EmitWrappedCallCase(
        WrappedCallCase flow,
        Clause clause,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        EmitCall(clause, location, flow.Input, entries, keepFrame: true);
        int error = nextLabel++;
        Op(IsTaggedTuple, LabelTag, error);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern(flow.Tag.Name));
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 0);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 3);
        Operand(AtomTag, Intern(flow.Tag.Name));
        Operand(XRegisterTag, 0);
        LiteralOperand(flow.LastElement);
        EmitFrameReturn();
        Op(Label, BeamWire.UnsignedTag, error);
        EmitFrameReturn();
    }

    private sealed record CallChain(Expr.Call Input, Expr.Call Output);

    private static CallChain? ReadCallChain(Clause clause)
    {
        if (clause.Body is Expr.Sequence
            {
                Items:
                [
                    Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Call { Module: null, Arguments.Count: 0 } input },
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable used] } output
                ]
            } && bound.Name == used.Name && output.Module != ErlangModuleNames.Module && IsFresh(clause, bound))
        {
            return new(input, output);
        }

        return null;
    }

    private static CallCase? ReadCallCase(Clause clause)
    {
        if (clause.Body is not Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false } call, Clauses: [var tuple, var other] }
            || call.Module == ErlangModuleNames.Module
            || tuple is not { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable value] }], Body: Expr.Variable returned }
            || value.Name != returned.Name || !IsFresh(clause, value)
            || other is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom otherTag }] }
            || !AttributeLiteral.TryNormalizeConstant(other.Body, out Term otherResult))
        {
            return null;
        }

        return new(call, tag, otherTag, otherResult);
    }

    private static bool IsFresh(Clause clause, Pattern.Variable variable) =>
        variable.Name != VariableScopeNames.Wildcard
        && !clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == variable.Name);

    private void EmitCallChain(
        CallChain chain,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((chain.Input.Function, 0), out int entry))
        {
            throw new NotSupportedException(Unsupported);
        }

        EmitEmptyFrame();
        EmitCallLine(chain.Input, location);
        Op(LocalCall, BeamWire.UnsignedTag, 0);
        Operand(LabelTag, entry);
        EmitCallLine(chain.Output, CallLocation(chain.Input, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(chain.Output.Module!, chain.Output.Function, 1));
        Operand(BeamWire.UnsignedTag, 0);
    }

    private void EmitCallCase(CallCase flow, BeamSourceLocation location)
    {
        var constants = new List<(int Destination, Term Value)>();
        for (int index = 0; index < flow.Input.Arguments.Count; index++)
        {
            if (!AttributeLiteral.TryNormalizeConstant(flow.Input.Arguments[index], out Term value))
            {
                throw new NotSupportedException(Unsupported);
            }
            constants.Add((index, value));
        }

        EmitEmptyFrame();
        EmitIndependentCallMoves(constants);
        EmitCallLine(flow.Input, location);
        Op(ExternalCall, BeamWire.UnsignedTag, constants.Count);
        Operand(BeamWire.UnsignedTag, Import(flow.Input.Module!, flow.Input.Function, constants.Count));

        int atomBranch = nextLabel++;
        int failure = nextLabel++;
        Op(IsTuple, LabelTag, atomBranch);
        Operand(XRegisterTag, 0);
        Op(IsTaggedTuple, LabelTag, failure);
        EmitTypedRegister(0, TupleTypeBits);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern(flow.TupleTag.Name));
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 0);
        EmitFrameReturn();

        Op(Label, BeamWire.UnsignedTag, atomBranch);
        Op(ExactEqualTest, LabelTag, failure);
        Operand(XRegisterTag, 0);
        Operand(AtomTag, Intern(flow.OtherTag.Name));
        EmitConstant(flow.OtherResult, 0);
        EmitFrameReturn();

        Op(Label, BeamWire.UnsignedTag, failure);
        Op(CaseEnd, XRegisterTag, 0);
    }

    private void EmitTypedRegister(
        int register,
        int type,
        long? lower = null,
        long? upper = null,
        int registerTag = XRegisterTag,
        int? unit = null
    )
    {
        var entry = (type, lower, upper, unit);
        int index = typeBits.IndexOf(entry);
        if (index < 0)
        {
            index = typeBits.Count;
            typeBits.Add(entry);
        }
        instructions.WriteByte(ExtendedTypedRegisterOperand);
        Operand(registerTag, register);
        Operand(BeamWire.UnsignedTag, index);
    }

    private void EmitTypes(Stream output)
    {
        Word(output, 4); // OTP 29 type table format version.
        Word(output, typeBits.Count);
        foreach (var (type, lower, upper, unit) in typeBits)
        {
            int bits = type | (lower.HasValue ? 0x2000 : 0) | (upper.HasValue ? 0x4000 : 0)
                | (unit.HasValue ? TypeUnitFlag : 0);
            output.WriteByte((byte)(bits >> 8));
            output.WriteByte((byte)bits);
            if (lower is long minimum)
            {
                WriteTypeBound(output, minimum);
            }
            if (upper is long maximum)
            {
                WriteTypeBound(output, maximum);
            }
            if (unit is int alignment)
            {
                output.WriteByte((byte)(alignment - 1));
            }
        }
    }

    private static void WriteTypeBound(Stream output, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        output.Write(bytes);
    }

    private void EmitEmptyFrame()
    {
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 0);
    }

    private void EmitFrameReturn(int frameSize = 0)
    {
        Op(Deallocate, BeamWire.UnsignedTag, frameSize);
        instructions.WriteByte(Return);
    }

    private void EmitCallLine(Expr.Call call, BeamSourceLocation previous)
    {
        var current = CallLocation(call, previous);
        if (current != previous)
        {
            EmitLine(current);
        }
    }

    private static BeamSourceLocation CallLocation(Expr.Call call, BeamSourceLocation previous) =>
        call.SourceLine == 0 ? previous : new(call.PreprocessorFile, call.SourceLine);
}
