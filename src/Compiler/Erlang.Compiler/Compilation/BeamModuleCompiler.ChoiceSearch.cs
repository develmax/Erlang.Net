using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ChoiceSearch(Expr.Call Test, Expr.Call Recur);
    private sealed record ChoiceAcceptance(Atom Reference, Atom Choice, Expr.Call Lookup, Expr.Call Recur,
        Expr.Call Search, Expr.Call Tag);

    private readonly HashSet<string> booleanChoiceFunctions = new(StringComparer.Ordinal);
    private readonly HashSet<string> choiceResultFunctions = new(StringComparer.Ordinal);

    private static ChoiceSearch? ReadChoiceSearch(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [
            { Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null }, Pattern.Variable],
                Body: Expr.Literal { Value: Atom { Name: "false" } } },
            { Guard: null, Patterns: [Pattern.List { Items: [Pattern.Tuple { Items: [
                Pattern.Variable name, Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable shape] }],
                Tail: Pattern.Variable fields }, Pattern.Variable tag],
                Body: Expr.Case { Value: Expr.Call { Module: null, Arguments: [Expr.Variable tested, Expr.Variable key] } test,
                    Clauses: [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }],
                            Body: Expr.Tuple { Items: [Expr.Variable selected, Expr.Variable returned] } },
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                            Body: Expr.Call { Module: null, Arguments: [Expr.Variable rest, Expr.Variable nextKey] } recur }
                    ] } }
        ] && DistinctBindings(name, shape, fields, tag) && tested.Name == shape.Name && key.Name == tag.Name
            && selected.Name == name.Name && returned.Name == shape.Name && rest.Name == fields.Name && nextKey.Name == tag.Name)
        {
            return new(test, recur);
        }

        return null;
    }

    private static ChoiceAcceptance? ReadChoiceAcceptance(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom reference }, Pattern.Variable name] },
                Pattern.Variable key1],
                Body: Expr.Call { Module: null, Arguments: [
                    Expr.Call { Module: null, Arguments: [Expr.Variable used] } lookup, Expr.Variable passed] } recur },
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom choice },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable fields] }, Pattern.Variable key2],
                Body: Expr.Binary { Operator: ErlangOperators.ExactNotEqual,
                    Left: Expr.Call { Module: null, Arguments: [Expr.Variable searched, Expr.Variable searchKey] } search,
                    Right: Expr.Literal { Value: Atom { Name: "false" } } } },
            { Guard: null, Patterns: [Pattern.Variable shape, Pattern.Variable key3],
                Body: Expr.Binary { Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Call { Module: null, Arguments: [Expr.Variable inspected] } tag, Right: Expr.Variable compared } }
        ] && DistinctBindings(name, key1) && DistinctBindings(fields, key2) && DistinctBindings(shape, key3)
            && name.Name == used.Name && key1.Name == passed.Name && fields.Name == searched.Name
            && key2.Name == searchKey.Name && shape.Name == inspected.Name && key3.Name == compared.Name)
        {
            return new(reference, choice, lookup, recur, search, tag);
        }

        return null;
    }

    private static bool DistinctBindings(params Pattern.Variable[] variables) =>
        variables.All(variable => variable.Name != VariableScopeNames.Wildcard)
        && variables.Select(variable => variable.Name).Distinct(StringComparer.Ordinal).Count() == variables.Length;

    private void EmitChoiceSearch(ChoiceSearch flow, BeamSourceLocation location, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Test.Function, 2), out int test)
            || !entries.TryGetValue((flow.Recur.Function, 2), out int recur))
        {
            throw new NotSupportedException(Unsupported);
        }

        int functionInfo = nextLabel - 2;
        int next = nextLabel++;
        int empty = nextLabel++;
        int invalid = booleanChoiceFunctions.Contains(flow.Test.Function) ? 0 : nextLabel++;
        Op(IsNonemptyList, LabelTag, empty);
        Operand(XRegisterTag, 0);
        Op(GetList, XRegisterTag, 0);
        Operand(XRegisterTag, 2);
        Operand(XRegisterTag, 3);
        Op(IsTuple, LabelTag, functionInfo);
        Operand(XRegisterTag, 2);
        Op(TestArity, LabelTag, functionInfo);
        Operand(XRegisterTag, 2);
        Operand(BeamWire.UnsignedTag, 3);
        Op(Allocate, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(XRegisterTag, 3, YRegisterTag, 2);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 3);
        Op(GetTupleElement, XRegisterTag, 2);
        Operand(BeamWire.UnsignedTag, 0);
        Operand(YRegisterTag, 1);
        Op(GetTupleElement, XRegisterTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, 0);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
        EmitCallLine(flow.Test, location);
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, test);
        EmitAtomTest(0, "true", next);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 0);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, 1);
        Operand(YRegisterTag, 0);
        EmitFrameReturn(4);

        Op(Label, BeamWire.UnsignedTag, next);
        if (invalid != 0)
        {
            EmitAtomTest(0, "false", invalid);
        }
        MoveRegister(YRegisterTag, 3, XRegisterTag, 1);
        MoveRegister(YRegisterTag, 2, XRegisterTag, 0);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, recur);
        Operand(BeamWire.UnsignedTag, 4);
        Op(Label, BeamWire.UnsignedTag, empty);
        Op(IsNil, LabelTag, functionInfo);
        Operand(XRegisterTag, 0);
        EmitConstant(Term.A("false"), 0);
        instructions.WriteByte(Return);

        if (invalid != 0)
        {
            Op(Label, BeamWire.UnsignedTag, invalid);
            Op(CaseEnd, XRegisterTag, 0);
        }
    }

    private void EmitChoiceAcceptance(ChoiceAcceptance flow, BeamSourceLocation location, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Lookup.Function, 1), out int lookup)
            || !entries.TryGetValue((flow.Recur.Function, 2), out int recur)
            || !entries.TryGetValue((flow.Search.Function, 2), out int search)
            || !entries.TryGetValue((flow.Tag.Function, 1), out int tag))
        {
            throw new NotSupportedException(Unsupported);
        }

        int choice = nextLabel++;
        int reference = nextLabel++;
        int fallback = nextLabel++;
        EmitTupleArityDispatch(fallback, [(2, reference), (3, choice)]);
        Op(Label, BeamWire.UnsignedTag, choice);
        EmitTupleElement(0, 2);
        EmitAtomTest(2, flow.Choice.Name, fallback);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        EmitTupleElement(2, 0);
        EmitCallLine(flow.Search, location);
        Op(LocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, search);
        Op(BifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.ExactNotEqual, 2));
        if (choiceResultFunctions.Contains(flow.Search.Function))
        {
            EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
        }
        else
        {
            Operand(XRegisterTag, 0);
        }
        Operand(AtomTag, Intern("false"));
        Operand(XRegisterTag, 0);
        EmitFrameReturn();

        Op(Label, BeamWire.UnsignedTag, reference);
        EmitTupleElement(0, 2);
        EmitAtomTest(2, flow.Reference.Name, fallback);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
        EmitTupleElement(1, 0);
        EmitCallLine(flow.Lookup, CallLocation(flow.Search, location));
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, lookup);
        MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, recur);
        Operand(BeamWire.UnsignedTag, 1);

        Op(Label, BeamWire.UnsignedTag, fallback);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
        EmitCallLine(flow.Tag, CallLocation(flow.Lookup, location));
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, tag);
        Op(BifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
        Operand(XRegisterTag, 0);
        Operand(YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);
    }

    private void EmitAtomTest(int register, string atom, int failure)
    {
        Op(ExactEqualTest, LabelTag, failure);
        Operand(XRegisterTag, register);
        Operand(AtomTag, Intern(atom));
    }
}
