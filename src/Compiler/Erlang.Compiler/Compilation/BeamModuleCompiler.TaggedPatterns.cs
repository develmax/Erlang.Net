using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TaggedChoice(Atom ReferenceTag, Atom ChoiceTag, Expr.Call Input, Expr.Call Recur);

    private static TaggedChoice? ReadTaggedChoice(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom reference }, Pattern.Variable name] }],
                Body: Expr.Call { Module: null, Arguments: [Expr.Call { Module: null, Arguments: [Expr.Variable used] } input] } recur },
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom choice },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                Body: Expr.Literal { Value: Atom { Name: "true" } } },
            { Guard: null, Patterns: [Pattern.Variable], Body: Expr.Literal { Value: Atom { Name: "false" } } }
        ] && name.Name == used.Name && name.Name != VariableScopeNames.Wildcard)
        {
            return new(reference, choice, input, recur);
        }

        return null;
    }

    private void EmitTaggedChoice(
        TaggedChoice choice,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((choice.Input.Function, 1), out int input)
            || !entries.TryGetValue((choice.Recur.Function, 1), out int recur))
        {
            throw new NotSupportedException(Unsupported);
        }

        int tagged = nextLabel++;
        int reference = nextLabel++;
        int fallback = nextLabel++;
        EmitTupleArityDispatch(fallback, [(2, reference), (3, tagged)]);

        Op(Label, BeamWire.UnsignedTag, tagged);
        EmitTupleElement(0, 0);
        Op(BifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, "=:=", 2));
        Operand(XRegisterTag, 0);
        Operand(AtomTag, Intern(choice.ChoiceTag.Name));
        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, reference);
        EmitReferenceCall(choice.ReferenceTag, choice.Input, input, recur, fallback, location);

        Op(Label, BeamWire.UnsignedTag, fallback);
        EmitConstant(Term.A("false"), 0);
        instructions.WriteByte(Return);
    }

    private sealed record TaggedField(Atom Reference, Atom Field, Atom Constant, Term Value,
        Expr.Call Input, Expr.Call Tail, Expr.Call Element);

    private static TaggedField? ReadTaggedField(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom reference }, Pattern.Variable name] }],
                Body: Expr.Call { Module: null, Arguments: [Expr.Call { Module: null, Arguments: [Expr.Variable used] } input] } tail },
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom field },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable selected,
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                Body: Expr.Variable returned },
            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom constant },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                Body: var body },
            { Guard: null, Patterns: [Pattern.Variable fallback],
                Body: Expr.Call { Module: null or ErlangModuleNames.Module, Function: "element", DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable tuple] } element }
        ] && name.Name == used.Name && name.Name != VariableScopeNames.Wildcard
            && selected.Name == returned.Name && selected.Name != VariableScopeNames.Wildcard
            && index.Value == 2 && fallback.Name == tuple.Name && fallback.Name != VariableScopeNames.Wildcard
            && AttributeLiteral.TryNormalizeConstant(body, out Term value))
        {
            return new(reference, field, constant, value, input, tail, element);
        }

        return null;
    }

    private void EmitTaggedField(
        TaggedField field,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((field.Input.Function, 1), out int input)
            || !entries.TryGetValue((field.Tail.Function, 1), out int tail)
            || field.Element.Module is null && entries.ContainsKey(("element", 2)))
        {
            throw new NotSupportedException(Unsupported);
        }

        int extract = nextLabel++;
        int constant = nextLabel++;
        int reference = nextLabel++;
        int fallback = nextLabel++;
        EmitTupleArityDispatch(fallback, [(2, reference), (3, constant), (5, extract)]);

        Op(Label, BeamWire.UnsignedTag, extract);
        EmitTupleTagTest(field.Field, fallback);
        EmitTupleElement(2, 0);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, constant);
        EmitTupleTagTest(field.Constant, fallback);
        EmitConstant(field.Value, 0);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, reference);
        EmitReferenceCall(field.Reference, field.Input, input, tail, fallback, location);

        Op(Label, BeamWire.UnsignedTag, fallback);
        EmitCallLine(field.Element, location);
        Op(BifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, "element", 2));
        Operand(BeamWire.IntegerTag, 2);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);
    }

    private void EmitTupleArityDispatch(int fallback, IReadOnlyList<(int Arity, int Label)> cases,
        int register = 0, int registerTag = XRegisterTag)
    {
        Op(IsTuple, LabelTag, fallback);
        Operand(registerTag, register);
        instructions.WriteByte(SelectTupleArity);
        EmitTypedRegister(register, TupleTypeBits, registerTag: registerTag);
        Operand(LabelTag, fallback);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, cases.Count * 2);
        foreach (var (arity, label) in cases)
        {
            Operand(BeamWire.UnsignedTag, arity);
            Operand(LabelTag, label);
        }
    }

    private void EmitReferenceCall(Atom tag, Expr.Call call, int input, int tail, int fallback, BeamSourceLocation location)
    {
        EmitTupleTagTest(tag, fallback);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleElement(1, 0);
        EmitCallLine(call, location);
        Op(LocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, input);
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, tail);
        Operand(BeamWire.UnsignedTag, 0);
    }

    private void EmitTupleTagTest(Atom tag, int fallback)
    {
        EmitTupleElement(0, 1);
        Op(ExactEqualTest, LabelTag, fallback);
        Operand(XRegisterTag, 1);
        Operand(AtomTag, Intern(tag.Name));
    }

    private void EmitTupleElement(int index, int register)
    {
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, index);
        Operand(XRegisterTag, register);
    }

    private static byte? ReadIdentityGuard(Clause clause)
    {
        if (clause is not
            {
                Patterns: [Pattern.Variable parameter],
                Guard: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Arguments: [Expr.Variable tested] } guard,
                Body: Expr.Variable result
            } || parameter.Name == VariableScopeNames.Wildcard || parameter.Name != tested.Name || parameter.Name != result.Name)
        {
            return null;
        }

        return guard.Function switch
        {
            "is_integer" => IsInteger,
            "is_atom" => IsAtom,
            "is_binary" => IsBinary,
            _ => null
        };
    }
}
