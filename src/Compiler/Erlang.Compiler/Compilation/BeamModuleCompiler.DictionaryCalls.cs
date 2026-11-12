using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitDictionaryCall(
        IReadOnlyList<Clause> clauses,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (clauses is not [{ Guard: null, Patterns: [Pattern.Variable key, ..] } clause]
            || !HasUnconditionalParameters(clause))
        {
            return false;
        }

        if (clause is
            {
                Patterns: [_],
                Body: Expr.Sequence
                {
                    Items: [Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module,
                            DynamicRemote: false,
                            Function: AutoImportNames.Get,
                            Arguments: [var lookupKey]
                        } recipient, Right: Expr.Variable message } send, var returnExpression]
                }
            }
            && key.Name != VariableScopeNames.Wildcard && message.Name == key.Name
            && ResolvesErlangBif(recipient)
            && AttributeLiteral.TryNormalizeConstant(lookupKey, out Term lookupConstant)
            && AttributeLiteral.TryNormalizeConstant(returnExpression, out Term returnConstant))
        {
            Op(Allocate, BeamWire.UnsignedTag, 0);
            Operand(BeamWire.UnsignedTag, 1);
            MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            EmitDictionaryLookup(0, lookupConstant);
            EmitArithmeticLine(send.SourceLine, ref location);
            instructions.WriteByte(Send);
            EmitConstant(returnConstant, 0);
            EmitFrameReturn(0);

            return true;
        }

        if (clause is
            {
                Patterns: [_],
                Body: Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual,
                    Left: var lookup,
                    Right: Expr.Literal { Value: Atom compared }
                } comparison
            } && IsDictionaryLookup(lookup, key, entries))
        {
            EmitDictionaryLookup(0);
            Op(BifTwo, LabelTag, 0);
            Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, comparison.Operator, 2));
            Operand(XRegisterTag, 0);
            LiteralOperand(compared);
            Operand(XRegisterTag, 0);
            instructions.WriteByte(Return);

            return true;
        }

        if (clause.Body is Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var argument, ..] } tail
            && IsDictionaryLookup(argument, key, entries)
            && (clause.Patterns, tail.Arguments) is
                ([_], [_]) or ([_], [_, Expr.Literal]) or ([_, Pattern.Variable], [_, Expr.Variable])
            && (tail.Arguments is not [_, Expr.Variable forwarded]
                || ((Pattern.Variable)clause.Patterns[1]).Name == forwarded.Name))
        {
            EmitDictionaryLookup(0);
            if (tail.Arguments is [_, Expr.Literal literal])
            {
                EmitConstant(literal.Value, 1);
            }
            EmitCallLine(tail, location);
            Op(TailExternalCall, BeamWire.UnsignedTag, tail.Arguments.Count);
            Operand(BeamWire.UnsignedTag, Import(tail.Module, tail.Function, tail.Arguments.Count));

            return true;
        }

        if (clause is not
            {
                Patterns: [_],
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match { Pattern: Pattern.Literal { Value: Atom expected }, Value: var checkedValue } assertion,
                    Expr.Match
                    {
                        Pattern: Pattern.Variable result, Value: Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr.Variable source, var option]
                        } create
                    },
                    Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Put,
                        DynamicRemote: false, Arguments: [Expr.Variable destination, Expr.Variable value]
                    } store
                ]
                }
            }
            || !DistinctBindings(key, result) || source.Name != key.Name
            || destination.Name != key.Name || value.Name != result.Name
            || !IsDictionaryLookup(checkedValue, key, entries)
            || store.Module is null && entries.ContainsKey((AutoImportNames.Put, 2))
            || !AttributeLiteral.TryNormalizeConstant(option, out Term constant))
        {
            return false;
        }

        int failure = nextLabel++;
        EmitDictionaryLookup(1);
        Op(ExactEqualTest, LabelTag, failure);
        Operand(XRegisterTag, 1);
        LiteralOperand(expected);

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        EmitConstant(constant, 1);
        EmitCallLine(create, location);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(create.Module, create.Function, 2));

        MoveRegister(
            XRegisterTag,
            0,
            XRegisterTag,
            1
        );
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        EmitCallLine(store, CallLocation(create, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Put, 2));
        Operand(BeamWire.UnsignedTag, 1);

        Op(Label, BeamWire.UnsignedTag, failure);
        EmitLine(assertion.SourceLine == 0 ? location : new(assertion.PreprocessorFile, assertion.SourceLine));
        Op(BadMatch, XRegisterTag, 1);

        return true;
    }

    private static bool IsDictionaryLookup(
        Expr expression,
        Pattern.Variable key,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    ) =>
        expression is Expr.Call
        {
            Module: null or ErlangModuleNames.Module,
            DynamicRemote: false,
            Function: AutoImportNames.Get,
            Arguments: [Expr.Variable input]
        } call && input.Name == key.Name
        && (call.Module is not null || !entries.ContainsKey((AutoImportNames.Get, 1)));

    private void EmitDictionaryLookup(int destination, Term? key = null)
    {
        Op(BifOne, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Get, 1));
        if (key is null)
        {
            Operand(XRegisterTag, 0);
        }
        else
        {
            LiteralOperand(key);
        }

        Operand(XRegisterTag, destination);
    }
}
