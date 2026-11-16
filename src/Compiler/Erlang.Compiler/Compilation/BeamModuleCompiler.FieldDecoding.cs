using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record FieldDecoding(Expr.Call Convert, Expr.Call Append, Expr.Call Test, Expr.Call ConvertOptional,
        Expr.Call Missing, Expr.Call EmptyMissing);

    private static FieldDecoding? ReadFieldDecoding(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            { Guard: null, Patterns: [var empty, Pattern.Variable children],
                Body: Expr.Tuple { Items: [var nil, Expr.Variable unchanged] } },
            { Guard: null, Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Literal { Value: Atom { Name: "required" } },
                Pattern.Variable shape] }], Tail: Pattern.Variable fields },
                Pattern.List { Items: [Pattern.Variable child], Tail: Pattern.Variable tail }],
                Body: Expr.Call { Module: null, Arguments: [Expr.Variable rest,
                    Expr.Call { Module: null, Arguments: [Expr.Variable selected, Expr.Variable input] } convert,
                    Expr.Variable remaining] } append },
            var optional,
            var exhausted
        ] || !IsEmptyListPattern(empty) || !AttributeLiteral.TryNormalizeConstant(nil, out Term nilTerm) || nilTerm is not Nil
            || !DistinctBindings(children) || children.Name != unchanged.Name || !DistinctBindings(shape, fields, child, tail)
            || rest.Name != fields.Name || selected.Name != shape.Name || input.Name != child.Name || remaining.Name != tail.Name
            || ReadOptionalField(optional, append.Function, convert.Function) is not { } present
            || ReadExhaustedField(exhausted, append.Function, present.Missing.Function) is not { } missing)
        {
            return null;
        }

        return new(convert, append, present.Test, present.Convert, present.Missing, missing);
    }

    private static (Expr.Call Test, Expr.Call Convert, Expr.Call Missing)? ReadOptionalField(Clause clause, string append, string convert)
    {
        if (clause is not
            {
                Guard: null,
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable presence, Pattern.Variable shape] }], Tail: Pattern.Variable fields },
                    AliasPattern { Left: Pattern.List { Items: [AliasPattern {
                        Left: Pattern.Tuple { Items: [Pattern.Variable tag, Pattern.Variable { Name: VariableScopeNames.Wildcard }] },
                        Right: Pattern.Variable child }], Tail: Pattern.Variable tail }, Right: Pattern.Variable all }],
                Body: Expr.Case { Value: Expr.Call { Module: null, Arguments: [Expr.Variable tested, Expr.Variable key] } test,
                    Clauses: [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }],
                            Body: Expr.Call { Module: null, Arguments: [Expr.Variable rest1,
                                Expr.Call { Module: null, Arguments: [Expr.Variable selected, Expr.Variable input] } read,
                                Expr.Variable remaining] } matched },
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                            Body: Expr.Call { Module: null, Arguments: [Expr.Variable rest2,
                                Expr.Call { Module: null, Arguments: [Expr.Variable mode] } missing,
                                Expr.Variable original] } unmatched }
                    ] }
            }
            || !DistinctBindings(presence, shape, fields, all, child, tag, tail)
            || tested.Name != shape.Name || key.Name != tag.Name || selected.Name != shape.Name || input.Name != child.Name
            || rest1.Name != fields.Name || rest2.Name != fields.Name || remaining.Name != tail.Name
            || mode.Name != presence.Name || original.Name != all.Name || matched.Function != append
            || unmatched.Function != append || read.Function != convert)
        {
            return null;
        }

        return (test, read, missing);
    }

    private static Expr.Call? ReadExhaustedField(Clause clause, string append, string missing)
    {
        if (clause is
            {
                Guard: null,
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable presence, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }], Tail: Pattern.Variable fields },
                    var empty],
                Body: Expr.Call { Module: null, Arguments: [Expr.Variable rest,
                    Expr.Call { Module: null, Arguments: [Expr.Variable mode] } absent, var nil] } call
            } && IsEmptyListPattern(empty) && DistinctBindings(presence, fields) && rest.Name == fields.Name
            && mode.Name == presence.Name && call.Function == append && absent.Function == missing
            && AttributeLiteral.TryNormalizeConstant(nil, out Term nilTerm) && nilTerm is Nil)
        {
            return absent;
        }

        return null;
    }

    private void EmitFieldDecoding(FieldDecoding flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Convert.Function, 2), out int convert)
            || !entries.TryGetValue((flow.Append.Function, 3), out int append)
            || !entries.TryGetValue((flow.Test.Function, 2), out int test)
            || !entries.TryGetValue((flow.Missing.Function, 1), out int missing))
        {
            throw new NotSupportedException(Unsupported);
        }

        int functionInfo = nextLabel - 2;
        int optional = nextLabel++;
        int absent = nextLabel++;
        int exhausted = nextLabel++;
        int empty = nextLabel++;
        int invalid = booleanChoiceFunctions.Contains(flow.Test.Function) ? 0 : nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(0, XRegisterTag, 2, XRegisterTag, 3);
        EmitTupleCheck(2, 3, functionInfo);
        EmitTuplePart(2, 1, 4);
        EmitTuplePart(2, 2, 2);
        EmitAtomTest(4, "required", optional);
        EmitNonemptyList(1, exhausted);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(XRegisterTag, 3, YRegisterTag, 1);
        EmitListParts(1, XRegisterTag, 1, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        EmitCallLine(flow.Convert, location);
        EmitFieldCall(2, convert);
        EmitAppendField(append, 2);

        Op(Label, BeamWire.UnsignedTag, optional);
        EmitNonemptyList(1, exhausted);
        EmitListParts(1, XRegisterTag, 5, XRegisterTag, 6);
        EmitTupleCheck(5, 2, functionInfo);
        Op(Allocate, BeamWire.UnsignedTag, 6);
        Operand(BeamWire.UnsignedTag, 7);
        MoveRegister(XRegisterTag, 5, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 6, YRegisterTag, 1);
        MoveRegister(XRegisterTag, 4, YRegisterTag, 2);
        MoveRegister(XRegisterTag, 2, YRegisterTag, 3);
        MoveRegister(XRegisterTag, 3, YRegisterTag, 4);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 5);
        EmitTuplePart(5, 0, 1);
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        EmitCallLine(flow.Test, CallLocation(flow.Convert, location));
        EmitFieldCall(2, test);
        EmitAtomTest(0, "true", absent);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
        MoveRegister(YRegisterTag, 3, XRegisterTag, 0);
        MoveRegister(YRegisterTag, 1, YRegisterTag, 5);
        TrimFieldFrame();
        EmitCallLine(flow.ConvertOptional, CallLocation(flow.Test, location));
        EmitFieldCall(2, convert);
        EmitAppendField(append, 2, optionalOrder: true);

        Op(Label, BeamWire.UnsignedTag, absent);
        if (invalid != 0)
        {
            EmitAtomTest(0, "false", invalid);
        }
        MoveRegister(YRegisterTag, 2, XRegisterTag, 0);
        TrimFieldFrame();
        EmitCallLine(flow.Missing, CallLocation(flow.ConvertOptional, location));
        EmitFieldCall(1, missing);
        EmitAppendField(append, 2, optionalOrder: true);

        Op(Label, BeamWire.UnsignedTag, exhausted);
        Op(IsNil, LabelTag, functionInfo);
        Operand(XRegisterTag, 1);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 5);
        MoveRegister(XRegisterTag, 3, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 4, XRegisterTag, 0);
        EmitCallLine(flow.EmptyMissing, CallLocation(flow.Missing, location));
        EmitFieldCall(1, missing);
        MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
        EmitConstant(Nil.Value, 2);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
        EmitFieldTail(append, 1);

        Op(Label, BeamWire.UnsignedTag, empty);
        Op(IsNil, LabelTag, functionInfo);
        Operand(XRegisterTag, 0);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 2);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 1);
        instructions.WriteByte(Return);

        if (invalid != 0)
        {
            Op(Label, BeamWire.UnsignedTag, invalid);
            Op(CaseEnd, XRegisterTag, 0);
        }
    }

    private void TrimFieldFrame()
    {
        Op(TrimFrame, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 2);
    }

    private void EmitFieldCall(int arity, int entry)
    {
        Op(LocalCall, BeamWire.UnsignedTag, arity);
        Operand(LabelTag, entry);
    }

    private void EmitAppendField(int entry, int frame, bool optionalOrder = false)
    {
        MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
        MoveRegister(YRegisterTag, optionalOrder ? 1 : 0, XRegisterTag, 2);
        MoveRegister(YRegisterTag, optionalOrder ? 0 : 1, XRegisterTag, 0);
        EmitFieldTail(entry, frame);
    }

    private void EmitFieldTail(int entry, int frame)
    {
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, frame);
    }
}
