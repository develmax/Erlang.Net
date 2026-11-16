using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ClosureProgress(Expr.Call Call, Term Argument);

    private sealed record ProtectedTermClosure(
        ClosureProgress Announce,
        ClosureProgress Begin,
        ClosureProgress Cleanup,
        Expr.Call Format,
        Expr.Call Render,
        Atom Result,
        Atom ErrorClass,
        Atom Reason,
        Expr.Binary Append,
        Term WarningPrefix,
        Expr.Call Warn
    );

    private static ProtectedTermClosure? ReadProtectedTermClosure(Expr body, IReadOnlyList<Pattern> captures)
    {
        if (captures is not [Pattern.Variable format, Pattern.Variable term, Pattern.Variable table, Pattern.Variable colors]
            || body is not Expr.Sequence
            {
                Items: [var announce, var begin, Expr.Try
                {
                    Body: Expr.Call
                    {
                        Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Variable template, Expr.List { Tail: null, Items: [Expr.Variable input] }]
                    } convert,
                    Clauses: [
                    {
                        Guard: null, Patterns: [Pattern.Variable rendered], Body: Expr.Tuple
                        {
                            Items: [
                    Expr.Literal { Value: Atom result }, Expr.Call
                            {
                                Module: null, Arguments: [
                        Expr.Variable text, Expr.Variable theme]
                            } render, Expr.Variable state]
                        }
                    }],
                    Catches: [
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            {
                                Items: [
                    Pattern.Literal { Value: Atom errorClass }, Pattern.Literal { Value: Atom reason },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                            }], Body: Expr.Sequence
                            {
                                Items: [
                    Expr.Match
                                {
                                    Pattern: Pattern.Variable warning, Value: Expr.Binary
                                    {
                                        Operator: ErlangOperators.Append,
                                        Left: Expr.Literal prefix, Right: Expr.Variable suffix
                                    } append
                                },
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable message, Expr.Variable errorTheme] } warn]
                            }
                        }],
                    After: { } cleanup
                }]
            }
            || !DistinctBindings(
                format,
                term,
                table,
                colors,
                rendered
            ) || !DistinctBindings(
                format,
                term,
                table,
                colors,
                warning
            )
            || template.Name != format.Name || input.Name != term.Name || state.Name != table.Name
            || text.Name != rendered.Name || theme.Name != colors.Name || errorTheme.Name != colors.Name
            || suffix.Name != format.Name || message.Name != warning.Name
            || ReadClosureProgress(announce) is not { } first || ReadClosureProgress(begin) is not { } second
            || ReadClosureProgress(cleanup) is not { } last)
        {
            return null;
        }

        return new(
            first,
            second,
            last,
            convert,
            render,
            result,
            errorClass,
            reason,
            append,
            prefix.Value,
            warn
        );
    }

    private static ClosureProgress? ReadClosureProgress(Expr expression) =>
        expression is Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var argument] } call
        && AttributeLiteral.TryNormalizeConstant(argument, out Term value) ? new(call, value) : null;

    private void EmitProtectedTermClosure(ProtectedTermClosure flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Render.Function, 2), out int render))
        {
            throw new NotSupportedException(Unsupported);
        }

        int handler = nextLabel++;
        int cleanup = nextLabel++;
        int raise = nextLabel++;
        int unwind = nextLabel++;
        Op(Allocate, BeamWire.UnsignedTag, 5);
        Operand(BeamWire.UnsignedTag, 4);
        InitStackSlot(3);
        MoveRegister(
            XRegisterTag,
            3,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            2,
            YRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            2
        );
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            4
        );
        EmitClosureProgress(flow.Announce, location);
        var announceLocation = CallLocation(flow.Announce.Call, location);
        EmitClosureProgress(flow.Begin, announceLocation);
        var beginLocation = CallLocation(flow.Begin.Call, announceLocation);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 0);
        Op(PutList, YRegisterTag, 4);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, 1);
        Op(TryEnter, YRegisterTag, 4);
        Operand(LabelTag, unwind);
        Op(TryEnter, YRegisterTag, 3);
        Operand(LabelTag, handler);
        MoveRegister(
            YRegisterTag,
            2,
            XRegisterTag,
            0
        );
        EmitCallLine(flow.Format, beginLocation);
        EmitStaticRemoteCall(flow.Format);
        Op(TryEnd, YRegisterTag, 3);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            1
        );
        ClearTermClosureSlots(0, 2);
        var formatLocation = CallLocation(flow.Format, beginLocation);
        EmitCallLine(flow.Render, formatLocation);
        EmitFieldCall(2, render);
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(
            0,
            (AtomTag, Intern(flow.Result.Name)),
            (XRegisterTag, 0),
            (YRegisterTag, 1)
        );
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            3
        );
        Op(Jump, LabelTag, cleanup);

        Op(Label, BeamWire.UnsignedTag, handler);
        Op(TryCase, YRegisterTag, 3);
        Op(ExactEqualTest, LabelTag, raise);
        EmitTypedRegister(0, AtomTypeBits);
        LiteralOperand(flow.ErrorClass);
        EmitAtomTest(1, flow.Reason.Name, raise);
        MoveRegister(
            YRegisterTag,
            2,
            XRegisterTag,
            1
        );
        ClearTermClosureSlots(1, 2);
        EmitConstant(flow.WarningPrefix, 0);
        var warningLocation = formatLocation;
        EmitArithmeticLine(flow.Append.SourceLine, ref warningLocation);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            1
        );
        InitStackSlot(0);
        EmitCallLine(flow.Warn, warningLocation);
        EmitStaticRemoteCall(flow.Warn);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            3
        );

        Op(Label, BeamWire.UnsignedTag, cleanup);
        Op(TryEnd, YRegisterTag, 4);
        MoveRegister(
            YRegisterTag,
            3,
            YRegisterTag,
            4
        );
        Op(TrimFrame, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        EmitClosureProgress(flow.Cleanup, CallLocation(flow.Warn, warningLocation));
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        EmitFrameReturn(1);

        Op(Label, BeamWire.UnsignedTag, raise);
        Op(Raise, XRegisterTag, 2);
        Operand(XRegisterTag, 1);
        Op(Label, BeamWire.UnsignedTag, unwind);
        Op(TryCase, YRegisterTag, 4);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            3
        );
        MoveRegister(
            XRegisterTag,
            2,
            YRegisterTag,
            4
        );
        EmitClosureProgress(flow.Cleanup, CallLocation(flow.Warn, warningLocation));
        Op(Raise, YRegisterTag, 4);
        Operand(YRegisterTag, 3);
    }

    private void ClearTermClosureSlots(int first, int second)
    {
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, first);
        Operand(YRegisterTag, second);
    }

    private void EmitClosureProgress(ClosureProgress progress, BeamSourceLocation previous)
    {
        EmitConstant(progress.Argument, 0);
        EmitCallLine(progress.Call, previous);
        EmitStaticRemoteCall(progress.Call);
    }
}
