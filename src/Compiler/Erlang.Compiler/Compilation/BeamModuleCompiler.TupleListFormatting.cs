using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string IntegerToList = "integer_to_list";

    private sealed record TupleListFormatting(
        Term Prefix,
        Integer Separator,
        Term Suffix,
        Expr.Call First,
        Expr.Call Second
    );

    private static TupleListFormatting? ReadTupleListFormatting(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }], Body: var body },
            { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable result }
        ] || !DistinctBindings(first, second) || !DistinctBindings(fallback) || fallback.Name != result.Name
            || !ReadFormattingAppend(body, out var prefix, out var tail)
            || !ReadFormattingAppend(tail, out var firstCall, out tail)
            || !ReadFormattingAppend(tail, out var separator, out tail)
            || !ReadFormattingAppend(tail, out var secondCall, out var suffix)
            || ReadIntegerFormattingCall(firstCall, first.Name) is not { } conversion
            || ReadIntegerFormattingCall(secondCall, second.Name) is not { } secondConversion
            || !AttributeLiteral.TryNormalizeConstant(prefix, out Term prefixTerm) || prefixTerm is not Cons
            || !AttributeLiteral.TryNormalizeConstant(separator, out Term separatorTerm)
            || separatorTerm is not Cons { Head: Integer character, Tail: Nil }
            || !AttributeLiteral.TryNormalizeConstant(suffix, out Term suffixTerm) || suffixTerm is not Cons)
        {
            return null;
        }

        return new(
            prefixTerm,
            character,
            suffixTerm,
            conversion,
            secondConversion
        );
    }

    private static bool ReadFormattingAppend(Expr expression, out Expr left, out Expr right)
    {
        if (expression is Expr.Binary { Operator: ErlangOperators.Append } append)
        {
            left = append.Left;
            right = append.Right;

            return true;
        }

        left = right = expression;

        return false;
    }

    private static Expr.Call? ReadIntegerFormattingCall(Expr expression, string name) =>
        expression is Expr.Call { Module: null, Function: IntegerToList, Arguments: [Expr.Variable value] } call
            && value.Name == name ? call : null;

    private bool ResolvesIntegerFormattingBif() =>
        sourceModule is not null
        && !sourceModule.Functions.Any(function => function.Name == IntegerToList && function.Arity == 1)
        && SourceCallResolution.ExternalModule(sourceModule, IntegerToList, 1) == ErlangModuleNames.Module;

    private void EmitTupleListFormatting(TupleListFormatting flow, BeamSourceLocation location)
    {
        if (!ResolvesIntegerFormattingBif())
        {
            throw new NotSupportedException(Unsupported);
        }

        int fallback = nextLabel++;
        EmitTupleCheck(0, 2, fallback);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        EmitTuplePart(0, 0, 0);
        EmitCallLine(flow.First, location);
        int convert = Import(ErlangModuleNames.Module, IntegerToList, 1);
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, convert);

        Op(GetTupleElement, YRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitCallLine(flow.Second, CallLocation(flow.First, location));
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, convert);

        EmitConstant(flow.Suffix, 1);
        int append = Import(ErlangModuleNames.Module, ErlangOperators.Append, 2);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, append);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        instructions.WriteByte(PutList);
        LiteralOperand(flow.Separator);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        Op(TrimFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 0);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, append);

        MoveRegister(
            XRegisterTag,
            0,
            XRegisterTag,
            1
        );
        EmitConstant(flow.Prefix, 0);
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, append);
        Operand(BeamWire.UnsignedTag, 0);
        Op(Label, BeamWire.UnsignedTag, fallback);
        instructions.WriteByte(Return);
    }
}
