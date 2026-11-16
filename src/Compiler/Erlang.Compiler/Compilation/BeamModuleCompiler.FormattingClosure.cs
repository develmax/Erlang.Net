using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record FormattingClosure(
        Expr.Call Protected,
        Expr.Call Transform,
        Expr.Call Finish,
        Expr.Call Warn,
        Term Message,
        Expr.Binary? Append,
        Atom ErrorClass,
        Atom Reason
    );

    private sealed record BinaryTextClosure(
        Expr.Call Digits,
        Expr.Call Format,
        Expr.Call Finish,
        Integer First,
        Integer Second,
        Term Suffix
    );

    private static BinaryTextClosure? ReadBinaryTextClosure(Expr body, IReadOnlyList<Pattern> captures)
    {
        if (captures is not [Pattern.Variable binary, Pattern.Variable colors]
            || body is not Expr.Sequence
            {
                Items: [
                Expr.Match
                {
                    Pattern: Pattern.Variable text, Value: Expr.Binary
                    {
                        Operator: ErlangOperators.Append,
                        Left: Expr.Literal { Value: Cons { Head: Integer first, Tail: Cons { Head: Integer second, Tail: Nil } } },
                        Right: Expr.Binary
                        {
                            Operator: ErlangOperators.Append, Left: Expr.Call
                            {
                                Module: null,
                                Arguments: [Expr.Variable input, Expr.Literal]
                            } digits, Right: Expr.Literal suffix
                        }
                    }
                },
                Expr.Call
                {
                    Module: null, Arguments: [
                    Expr.Call
                    {
                        Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal, Expr.List { Tail: null, Items: [Expr.Variable formatted] }]
                    } format,
                    Expr.Variable theme]
                } finish]
            }
            || !DistinctBindings(binary, colors, text) || input.Name != binary.Name
            || formatted.Name != text.Name || theme.Name != colors.Name)
        {
            return null;
        }

        return new(
            digits,
            format,
            finish,
            first,
            second,
            suffix.Value
        );
    }

    private void EmitBinaryTextClosure(BinaryTextClosure flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Digits.Function, 2), out int digits)
            || !entries.TryGetValue((flow.Finish.Function, 2), out int finish))
        {
            throw new NotSupportedException(Unsupported);
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            0
        );
        EmitConstant(flow.Digits.Arguments[1], 1);
        EmitCallLine(flow.Digits, location);
        EmitFieldCall(2, digits);
        EmitConstant(flow.Suffix, 1);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
        Op(TestHeap, BeamWire.UnsignedTag, 3 * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTextCharacter(flow.Second);
        EmitTextCharacter(flow.First);
        EmitFormattingArgumentList(0);
        EmitConstant(flow.Format.Arguments[0], 0);
        var digitsLocation = CallLocation(flow.Digits, location);
        EmitCallLine(flow.Format, digitsLocation);
        EmitStaticRemoteCall(flow.Format);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            1
        );
        EmitCallLine(flow.Finish, CallLocation(flow.Format, digitsLocation));
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, finish);
        Operand(BeamWire.UnsignedTag, 1);
    }

    private void EmitTextCharacter(Integer character)
    {
        instructions.WriteByte(PutList);
        LiteralOperand(character);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
    }

    private static FormattingClosure? ReadFormattingClosure(Expr body, IReadOnlyList<Pattern> captures)
    {
        if (body is not Expr.Try
            {
                After: null, Body: Expr.Call { DynamicRemote: false } protectedCall,
                Clauses: [
                    {
                        Guard: null, Patterns: [Pattern.Variable result], Body:
                    Expr.Call
                        {
                            Module: null, Arguments: [Expr.Call { Module: not null, DynamicRemote: false } transform,
                        Expr.Variable successColors]
                        } finish
                    }],
                Catches: [
                    {
                        Guard: null, Patterns: [Pattern.Tuple
                        {
                            Items: [
                    Pattern.Literal { Value: Atom errorClass }, Pattern.Literal { Value: Atom reason },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        }],
                        Body: var warning
                    }]
            }
            || !DistinctBindings(result) || captures.OfType<Pattern.Variable>().Any(variable => variable.Name == result.Name))
        {
            return null;
        }

        if (captures is [Pattern.Variable format, Pattern.Variable value, Pattern.Variable colors]
            && protectedCall is
            {
                Module: not null, Arguments: [Expr.Variable template,
                Expr.List { Tail: null, Items: [Expr.Variable item] }]
            }
            && transform.Arguments is [Expr.Variable transformed]
            && warning is Expr.Sequence
            {
                Items: [
                Expr.Match
                {
                    Pattern: Pattern.Variable message, Value: Expr.Binary
                    {
                        Operator: ErlangOperators.Append,
                        Left: Expr.Literal prefix, Right: Expr.Variable suffix
                    } append
                },
                Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable text, Expr.Variable warningColors] } warn]
            }
            && DistinctBindings(
                format,
                value,
                colors,
                message,
                result
            )
            && format.Name == template.Name && format.Name == suffix.Name && value.Name == item.Name
            && result.Name == transformed.Name && message.Name == text.Name
            && colors.Name == successColors.Name && colors.Name == warningColors.Name)
        {
            return new(
                protectedCall,
                transform,
                finish,
                warn,
                prefix.Value,
                append,
                errorClass,
                reason
            );
        }

        if (captures is [Pattern.Variable binary, Pattern.Variable theme]
            && protectedCall is
            {
                Module: null or ErlangModuleNames.Module, Function: AutoImportNames.BinaryToTerm,
                Arguments: [Expr.Variable input]
            }
            && transform.Arguments is [Expr.Literal, Expr.List { Tail: null, Items: [Expr.Variable converted] }]
            && DistinctBindings(binary, theme, result) && binary.Name == input.Name && result.Name == converted.Name
            && theme.Name == successColors.Name
            && ReadConstantClosureWarning(warning, binary, theme) is { } constant)
        {
            return new(
                protectedCall,
                transform,
                finish,
                constant.Call,
                constant.Message,
                null,
                errorClass,
                reason
            );
        }

        return null;
    }

    private static (Expr.Call Call, Term Message)? ReadConstantClosureWarning(
        Expr warning, Pattern.Variable binary, Pattern.Variable theme)
    {
        if (warning is Expr.Call
            {
                Module: not null, DynamicRemote: false, Arguments: [
            Expr.Literal literal, Expr.Variable colors]
            } direct && colors.Name == theme.Name)
        {
            return (direct, literal.Value);
        }
        if (warning is Expr.Sequence
            {
                Items: [
            Expr.Match { Pattern: Pattern.Variable message, Value: Expr.Literal value },
            Expr.Call
                {
                    Module: not null, DynamicRemote: false, Arguments: [
                Expr.Variable text, Expr.Variable errorColors]
                } bound]
            }
            && DistinctBindings(binary, theme, message) && text.Name == message.Name && errorColors.Name == theme.Name)
        {
            return (bound, value.Value);
        }

        return null;
    }

    private void EmitFormattingClosure(FormattingClosure flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        bool appendWarning = flow.Append is not null;
        if (!entries.TryGetValue((flow.Finish.Function, 2), out int finish)
            || !appendWarning && !ResolvesErlangBif(flow.Protected))
        {
            throw new NotSupportedException(Unsupported);
        }

        int handler = nextLabel++;
        int raise = nextLabel++;
        int frame = appendWarning ? 3 : 2;
        int token = frame - 1;
        Op(appendWarning ? AllocateHeap : Allocate, BeamWire.UnsignedTag, frame);
        if (appendWarning)
        {
            Operand(BeamWire.UnsignedTag, ConsHeapWords);
        }
        Operand(BeamWire.UnsignedTag, frame);
        MoveRegister(
            XRegisterTag,
            token,
            YRegisterTag,
            0
        );
        if (appendWarning)
        {
            MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            EmitFormattingArgumentList(1);
        }
        Op(TryEnter, YRegisterTag, token);
        Operand(LabelTag, handler);
        EmitCallLine(flow.Protected, location);
        EmitStaticRemoteCall(appendWarning ? flow.Protected : flow.Protected with { Module = ErlangModuleNames.Module });
        Op(TryEnd, YRegisterTag, token);
        if (appendWarning)
        {
            MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                2
            );
            Op(TrimFrame, BeamWire.UnsignedTag, 2);
            Operand(BeamWire.UnsignedTag, 1);
        }
        else
        {
            Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            Operand(BeamWire.UnsignedTag, 1);
            EmitFormattingArgumentList(0);
            EmitConstant(flow.Transform.Arguments[0], 0);
        }
        var protectedLocation = CallLocation(flow.Protected, location);
        EmitCallLine(flow.Transform, protectedLocation);
        EmitStaticRemoteCall(flow.Transform);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            1
        );
        EmitCallLine(flow.Finish, CallLocation(flow.Transform, protectedLocation));
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, finish);
        Operand(BeamWire.UnsignedTag, appendWarning ? 1 : frame);

        Op(Label, BeamWire.UnsignedTag, handler);
        Op(TryCase, YRegisterTag, token);
        Op(ExactEqualTest, LabelTag, raise);
        EmitTypedRegister(0, AtomTypeBits);
        LiteralOperand(flow.ErrorClass);
        EmitAtomTest(1, flow.Reason.Name, raise);
        var warningLocation = protectedLocation;
        if (flow.Append is { } append)
        {
            MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            InitStackSlot(1);
            EmitConstant(flow.Message, 0);
            EmitArithmeticLine(append.SourceLine, ref warningLocation);
            Op(ExternalCall, BeamWire.UnsignedTag, 2);
            Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
        }
        else
        {
            MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            EmitConstant(flow.Message, 0);
        }
        EmitCallLine(flow.Warn, warningLocation);
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(flow.Warn.Module!, flow.Warn.Function, 2));
        Operand(BeamWire.UnsignedTag, frame);
        Op(Label, BeamWire.UnsignedTag, raise);
        Op(Raise, XRegisterTag, 2);
        Operand(XRegisterTag, 1);
    }

    private void EmitFormattingArgumentList(int register)
    {
        Op(PutList, XRegisterTag, register);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, 1);
    }
}
