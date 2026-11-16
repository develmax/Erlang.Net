using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitAtomReplacementDispatch(FunctionDefinition function)
    {
        if (function.Arity != 1 || function.Clauses.Count < 3
            || function.Clauses[^1] is not
            {
                Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Variable result
            }
            || input.Name == VariableScopeNames.Wildcard || input.Name != result.Name)
        {
            return false;
        }

        var replacements = new SortedDictionary<string, (Atom Key, Atom Value)>(StringComparer.Ordinal);
        var results = new HashSet<Atom>();
        foreach (Clause clause in function.Clauses.SkipLast(1))
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Literal { Value: Atom value }
                }
                || key.Equals(value) || !results.Add(value) || !replacements.TryAdd(key.Name, (key, value)))
            {
                return false;
            }
        }

        var branches = replacements.Values.Reverse()
            .Select(pair => (pair.Key, pair.Value, Label: nextLabel++)).ToArray();
        int fallback = nextLabel++;
        EmitMembershipSelect(0, fallback, branches.Reverse().Select(branch => ((Term)branch.Key, branch.Label)));
        foreach (var branch in branches)
        {
            Op(Label, BeamWire.UnsignedTag, branch.Label);
            EmitConstant(branch.Value, 0);
            instructions.WriteByte(Return);
        }

        Op(Label, BeamWire.UnsignedTag, fallback);
        instructions.WriteByte(Return);
        return true;
    }

    private bool TryEmitAtomAliasDispatch(FunctionDefinition function,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (function.Arity != 1 || function.Clauses.Count < 5
            || function.Clauses[0] is not
            {
                Guard: null, Patterns: [Pattern.Literal { Value: Atom alias }],
                Body: Expr.Call { Module: null, Arguments: [var argument] } call
            }
            || call.Function != function.Name || !entries.TryGetValue((call.Function, 1), out int entry)
            || !AttributeLiteral.TryNormalizeConstant(argument, out Term targetTerm) || targetTerm is not Integer target)
        {
            return false;
        }

        var cases = new Dictionary<Integer, Integer>();
        foreach (Clause clause in function.Clauses.Skip(1))
        {
            if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Integer key }], Body: var body }
                || !AttributeLiteral.TryNormalizeConstant(body, out Term result) || result is not Integer value
                || !cases.TryAdd(key, value))
            {
                return false;
            }
        }
        if (!cases.ContainsKey(target))
        {
            return false;
        }

        int failure = nextLabel - 2;
        int dispatch = nextLabel++;
        Op(IsAtom, LabelTag, dispatch);
        Operand(XRegisterTag, 0);
        Op(ExactEqualTest, LabelTag, failure);
        EmitTypedRegister(0, AtomTypeBits);
        LiteralOperand(alias);
        EmitConstant(target, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, entry);
        Op(Label, BeamWire.UnsignedTag, dispatch);

        var branches = cases.OrderByDescending(pair => pair.Key.Value)
            .Select(pair => (pair.Key, pair.Value, Label: nextLabel++)).ToArray();
        Op(SelectValue, XRegisterTag, 0);
        Operand(LabelTag, failure);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, branches.Length * 2);
        foreach (var branch in branches.Reverse())
        {
            LiteralOperand(branch.Key);
            Operand(LabelTag, branch.Label);
        }
        foreach (var branch in branches)
        {
            Op(Label, BeamWire.UnsignedTag, branch.Label);
            if (branch.Key.Value != branch.Value.Value)
            {
                EmitConstant(branch.Value, 0);
            }
            instructions.WriteByte(Return);
        }

        return true;
    }

    private static Expr.Call? ReadArgumentListError(Clause clause)
    {
        if (clause is not
            {
                Guard: null,
                Body: Expr.Call
                {
                    Module: ErlangModuleNames.Module,
                    Function: "error",
                    DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Atom }, Expr.List { Tail: null } arguments]
                } call
            } || arguments.Items.Count != clause.Patterns.Count)
        {
            return null;
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < arguments.Items.Count; index++)
        {
            if (clause.Patterns[index] is not Pattern.Variable parameter
                || parameter.Name == VariableScopeNames.Wildcard || !names.Add(parameter.Name)
                || arguments.Items[index] is not Expr.Variable argument || argument.Name != parameter.Name)
            {
                return null;
            }
        }

        return call;
    }

    private void EmitArgumentListError(Expr.Call call, BeamSourceLocation location)
    {
        var arguments = (Expr.List)call.Arguments[1];
        int arity = arguments.Items.Count;
        Op(TestHeap, BeamWire.UnsignedTag, arity * 2);
        Operand(BeamWire.UnsignedTag, arity);
        for (int index = arity - 1; index >= 0; index--)
        {
            Op(PutList, XRegisterTag, index);
            if (index == arity - 1)
            {
                LiteralOperand(Nil.Value);
            }
            else
            {
                Operand(XRegisterTag, 1);
            }
            Operand(XRegisterTag, 1);
        }
        EmitConstant(call.Arguments[0], 0);
        EmitCallLine(call, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, "error", 2));
    }

    private bool TryEmitAtomIntegerDispatch(FunctionDefinition function, BeamSourceLocation location)
    {
        if (function.Arity != 2 || function.Clauses.Count < 3
            || ReadArgumentListError(function.Clauses[^1]) is not { } error)
        {
            return false;
        }

        var keys = new HashSet<Atom>();
        var cases = new List<(Atom Key, Integer Argument, Integer Result)>();
        foreach (Clause clause in function.Clauses.SkipLast(1))
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Literal { Value: Integer argument }],
                    Body: Expr.Literal { Value: Integer result }
                } || !keys.Add(key))
            {
                return false;
            }

            cases.Add((key, argument, result));
        }

        var comparer = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
        var branches = cases.OrderByDescending(branch => branch.Key, comparer)
            .Select(branch => (branch.Key, branch.Argument, branch.Result, Label: nextLabel++)).ToArray();
        int failure = nextLabel++;
        Op(SelectValue, XRegisterTag, 0);
        Operand(LabelTag, failure);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, branches.Length * 2);
        foreach (var branch in branches.Reverse())
        {
            EmitPatternOperand(branch.Key);
            Operand(LabelTag, branch.Label);
        }
        foreach (var branch in branches)
        {
            Op(Label, BeamWire.UnsignedTag, branch.Label);
            Op(ExactEqualTest, LabelTag, failure);
            Operand(XRegisterTag, 1);
            LiteralOperand(branch.Argument);
            EmitConstant(branch.Result, 0);
            instructions.WriteByte(Return);
        }
        Op(Label, BeamWire.UnsignedTag, failure);
        EmitArgumentListError(error, location);

        return true;
    }
}
