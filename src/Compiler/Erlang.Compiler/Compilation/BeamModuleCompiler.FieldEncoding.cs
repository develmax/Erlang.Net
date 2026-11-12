using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record FieldEncoding(Expr.Call Convert, Expr.Call Recur);

    private static FieldEncoding? ReadFieldEncoding(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            { Guard: null, Patterns: [var emptyFields, var emptyValues], Body: var empty },
            var optional, var defaulted, var encode
        ] || !IsEmptyListPattern(emptyFields) || !IsEmptyListPattern(emptyValues)
            || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyResult) || emptyResult is not Nil
            || encode is not
            {
                Guard: null,
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable shape] }], Tail: Pattern.Variable fields },
                    Pattern.List { Items: [Pattern.Variable value], Tail: Pattern.Variable values }],
                Body: Expr.List { Items: [Expr.Call { Module: null, Arguments: [Expr.Variable selected, Expr.Variable input] } convert],
                    Tail: Expr.Call { Module: null, Arguments: [Expr.Variable remainingFields, Expr.Variable remainingValues] } recur }
            }
            || !DistinctBindings(shape, fields, value, values)
            || selected.Name != shape.Name || input.Name != value.Name
            || remainingFields.Name != fields.Name || remainingValues.Name != values.Name
            || !IsSkippedField(optional, "optional", recur.Function, false)
            || !IsSkippedField(defaulted, "default_false", recur.Function, true))
        {
            return null;
        }

        return new(convert, recur);
    }

    private static bool IsEmptyListPattern(Pattern pattern) =>
        pattern is Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil };

    private static bool IsSkippedField(Clause clause, string presence, string recur, bool hasDefault)
    {
        if (clause is not
            {
                Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Literal { Value: Atom mode },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }] }], Tail: Pattern.Variable fields },
                    Pattern.List { Items: [var head], Tail: Pattern.Variable values }],
                Body: Expr.Call { Module: null, Arguments: [Expr.Variable restFields, Expr.Variable restValues] } call
            } || mode.Name != presence || call.Function != recur || !DistinctBindings(fields, values)
            || fields.Name != restFields.Name || values.Name != restValues.Name)
        {
            return false;
        }

        if (!hasDefault)
        {
            return clause.Guard is null && head is Pattern.Literal { Value: Atom { Name: "asn1_NOVALUE" } };
        }

        return head is Pattern.Variable value && DistinctBindings(fields, values, value)
            && clause.Guard is Expr.GuardAlternatives { Items: [var defaultTest, var falseTest] }
            && IsClassTest(defaultTest, value.Name, "asn1_DEFAULT") && IsClassTest(falseTest, value.Name, "false");
    }

    private void EmitFieldEncoding(FieldEncoding flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Convert.Function, 2), out int convert)
            || !entries.TryGetValue((flow.Recur.Function, 2), out int recur))
        {
            throw new NotSupportedException(Unsupported);
        }

        int functionInfo = nextLabel - 2;
        int optional = nextLabel++;
        int defaulted = nextLabel++;
        int skip = nextLabel++;
        int encode = nextLabel++;
        int empty = nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(0, XRegisterTag, 2, XRegisterTag, 3);
        EmitTupleCheck(2, 3, functionInfo);
        EmitTuplePart(2, 1, 4);
        EmitAtomSelection(4, encode, "default_false", defaulted, "optional", optional);

        Op(Label, BeamWire.UnsignedTag, optional);
        EmitNonemptyList(1, functionInfo);
        EmitListParts(1, XRegisterTag, 4, XRegisterTag, 5);
        EmitAtomTest(4, "asn1_NOVALUE", encode);
        Op(Jump, LabelTag, skip);

        Op(Label, BeamWire.UnsignedTag, defaulted);
        EmitNonemptyList(1, functionInfo);
        EmitListParts(1, XRegisterTag, 4, XRegisterTag, 5);
        EmitAtomSelection(4, encode, "asn1_DEFAULT", skip, "false", skip);
        Op(Label, BeamWire.UnsignedTag, skip);
        MoveRegister(XRegisterTag, 5, XRegisterTag, 1);
        MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, recur);

        Op(Label, BeamWire.UnsignedTag, encode);
        EmitNonemptyList(1, functionInfo);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(XRegisterTag, 3, YRegisterTag, 1);
        EmitListParts(1, XRegisterTag, 1, YRegisterTag, 0);
        EmitTuplePart(2, 2, 0);
        EmitCallLine(flow.Convert, location);
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, convert);
        Op(Swap, YRegisterTag, 1);
        Operand(XRegisterTag, 0);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
        Op(TrimFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        EmitCallLine(flow.Recur, CallLocation(flow.Convert, location));
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, recur);
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);

        Op(Label, BeamWire.UnsignedTag, empty);
        Op(IsNil, LabelTag, functionInfo);
        Operand(XRegisterTag, 0);
        Op(IsNil, LabelTag, functionInfo);
        Operand(XRegisterTag, 1);
        EmitConstant(Nil.Value, 0);
        instructions.WriteByte(Return);
    }

    private void EmitNonemptyList(int register, int failure)
    {
        Op(IsNonemptyList, LabelTag, failure);
        Operand(XRegisterTag, register);
    }

    private void EmitListParts(int input, int headTag, int head, int tailTag, int tail)
    {
        Op(GetList, XRegisterTag, input);
        Operand(headTag, head);
        Operand(tailTag, tail);
    }

    private void EmitTupleCheck(int input, int arity, int failure)
    {
        Op(IsTuple, LabelTag, failure);
        Operand(XRegisterTag, input);
        Op(TestArity, LabelTag, failure);
        Operand(XRegisterTag, input);
        Operand(BeamWire.UnsignedTag, arity);
    }

    private void EmitTuplePart(
        int input,
        int position,
        int output,
        int inputTag = XRegisterTag
    )
    {
        Op(GetTupleElement, inputTag, input);
        Operand(BeamWire.UnsignedTag, position);
        Operand(XRegisterTag, output);
    }

    private void EmitAtomSelection(int register, int failure, string first, int firstLabel, string second, int secondLabel)
    {
        Op(SelectValue, XRegisterTag, register);
        Operand(LabelTag, failure);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 4);
        Operand(AtomTag, Intern(first));
        Operand(LabelTag, firstLabel);
        Operand(AtomTag, Intern(second));
        Operand(LabelTag, secondLabel);
    }
}
