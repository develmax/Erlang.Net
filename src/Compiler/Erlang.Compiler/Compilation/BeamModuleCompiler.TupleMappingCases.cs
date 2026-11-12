using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TupleMappingCase(
        Term[] Defaults,
        Atom LastKey,
        Expr.Call Map,
        Expr.Call Invoke,
        Expr.Call Retry,
        Term RetryOn,
        ComprehensionQualifier.Generator Generator,
        int MatchLine
    );

    private readonly Dictionary<(string Name, int Arity), TupleMappingCase> tupleMappingCases = [];

    private static TupleMappingCase? ReadTupleMappingCase(FunctionDefinition function)
    {
        if (function.Clauses is not [var clause] || !HasUnconditionalParameters(clause)
            || clause is not
            {
                Guard: null,
                Patterns: [Pattern.Variable id, Pattern.Variable module, Pattern.Variable member,
                    Pattern.Variable args, Pattern.Variable captured],
                Body: Expr.Sequence { Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.List { Items: [Pattern.Variable first, Pattern.Variable second,
                            Pattern.Variable third, Pattern.Variable fourth], Tail: null },
                        Value: Expr.ListComprehension
                        {
                            Items: [Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                Arguments: [Expr.Variable keyArg, Expr.Variable captureArg, Expr.Variable defaultArg] } map],
                            Qualifiers: [ComprehensionQualifier.Generator
                            {
                                Strict: false, Pattern: Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable defaultValue] },
                                Source: Expr.List { Items: [var d0, var d1, var d2,
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom lastKey },
                                        Expr.List { Items: [Expr.Variable listModule], Tail: null }] }], Tail: null }
                            } generator]
                        }
                    } match,
                    Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Atom receiver }, Expr.Tuple { Items: [
                                Expr.Variable child, Expr.Tuple { Items: [Expr.Variable m, Expr.Variable f, Expr.Variable a] },
                                Expr.Variable x0, Expr.Variable x1, Expr.Variable x2, Expr.Variable x3
                            ] }]
                        } invoke,
                        Clauses: [
                            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal left, Pattern.Literal right] }],
                                Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                    Arguments: [Expr.Literal { Value: Atom retryReceiver }, Expr.Variable retryChild] } retry },
                            { Guard: null, Patterns: [Pattern.Variable other], Body: Expr.Variable result }
                        ]
                    }
                ] }
            }
            || key.Name != keyArg.Name || defaultValue.Name != defaultArg.Name || captured.Name != captureArg.Name
            || module.Name != listModule.Name || child.Name != id.Name || m.Name != module.Name
            || f.Name != member.Name || a.Name != args.Name || first.Name != x0.Name || second.Name != x1.Name
            || third.Name != x2.Name || fourth.Name != x3.Name || retryChild.Name != id.Name
            || !receiver.Equals(retryReceiver) || other.Name != result.Name
            || !FreshBindings(clause, first, second, third, fourth, other)
            || !DistinctMappingBindings(key, defaultValue, captured))
        {
            return null;
        }

        Expr[] defaults = [d0, d1, d2];
        var literals = new Term[defaults.Length];
        for (int index = 0; index < defaults.Length; index++)
        {
            if (defaults[index] is not Expr.Tuple { Items.Count: 2 }
                || !AttributeLiteral.TryNormalizeConstant(defaults[index], out literals[index]))
            {
                return null;
            }
        }

        return new TupleMappingCase(
            literals, lastKey, map, invoke, retry, Term.Tuple(left.Value, right.Value), generator, match.SourceLine);
    }

    private static bool FreshBindings(Clause clause, params Pattern.Variable[] bindings)
    {
        var names = clause.Patterns.OfType<Pattern.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
        return bindings.All(variable => variable.Name != VariableScopeNames.Wildcard && names.Add(variable.Name));
    }

    private static bool DistinctMappingBindings(params Pattern.Variable[] bindings) =>
        bindings.All(variable => variable.Name != VariableScopeNames.Wildcard)
        && bindings.Select(variable => variable.Name).Distinct(StringComparer.Ordinal).Count() == bindings.Length;

    private static BeamSourceLocation MappingGeneratorLocation(TupleMappingCase mapping, BeamSourceLocation previous) =>
        mapping.Generator.GeneratorLine == 0 ? previous : new(mapping.Generator.GeneratorFile, mapping.Generator.GeneratorLine);

    private void EmitTupleMappingCase(TupleMappingCase mapping, BeamSourceLocation location, int helper)
    {
        const int frame = 4;
        int finish = nextLabel++;
        int mismatch = nextLabel++;
        Op(AllocateHeap, BeamWire.UnsignedTag, frame);
        Operand(BeamWire.UnsignedTag, 13); // Four cons cells, a tuple pair and a singleton list.
        Operand(BeamWire.UnsignedTag, 5);
        for (int slot = 0; slot < frame; slot++)
        {
            MoveRegister(XRegisterTag, frame - 1 - slot, YRegisterTag, slot);
        }

        Op(PutList, XRegisterTag, 1);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 0);
        EmitTagMatchTuple(0, (AtomTag, Intern(mapping.LastKey.Name)), (XRegisterTag, 0));
        Op(PutList, XRegisterTag, 0);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 0);
        foreach (Term value in mapping.Defaults.Reverse())
        {
            instructions.WriteByte(PutList);
            LiteralOperand(value);
            Operand(XRegisterTag, 0);
            Operand(XRegisterTag, 0);
        }

        MoveRegister(XRegisterTag, 4, XRegisterTag, 1);
        var generator = MappingGeneratorLocation(mapping, location);
        EmitLine(generator);
        EmitFieldCall(2, helper);

        EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
        for (int register = 3; register <= 5; register++)
        {
            EmitNonemptyList(2, mismatch);
            EmitListParts(2, XRegisterTag, register, XRegisterTag, 2);
        }

        Op(IsNil, LabelTag, mismatch);
        EmitTypedRegister(2, ConsTypeBits | (1 << 7));
        Op(TestHeap, BeamWire.UnsignedTag, 11); // The child tuple and its nested MFA.
        Operand(BeamWire.UnsignedTag, 6);
        EmitTagMatchTuple(0, (YRegisterTag, 2), (YRegisterTag, 1), (YRegisterTag, 0));
        EmitTagMatchTuple(1, (YRegisterTag, 3), (XRegisterTag, 0), (XRegisterTag, 1),
            (XRegisterTag, 3), (XRegisterTag, 4), (XRegisterTag, 5));
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 3);
        for (int slot = 0; slot < 3; slot++)
        {
            Operand(YRegisterTag, slot);
        }

        Term receiver = ((Expr.Literal)mapping.Invoke.Arguments[0]).Value;
        EmitConstant(receiver, 0);
        EmitCallLine(mapping.Invoke, generator);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(mapping.Invoke.Module!, mapping.Invoke.Function, 2));

        Op(ExactEqualTest, LabelTag, finish);
        Operand(XRegisterTag, 0);
        LiteralOperand(mapping.RetryOn);
        MoveRegister(YRegisterTag, 3, XRegisterTag, 1);
        EmitConstant(receiver, 0);
        EmitCallLine(mapping.Retry, CallLocation(mapping.Invoke, generator));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(mapping.Retry.Module!, mapping.Retry.Function, 2));
        Operand(BeamWire.UnsignedTag, frame);

        Op(Label, BeamWire.UnsignedTag, finish);
        EmitFrameReturn(frame);
        Op(Label, BeamWire.UnsignedTag, mismatch);
        EmitLine(new(location.File, mapping.MatchLine == 0 ? location.Line : mapping.MatchLine));
        Op(BadMatch, XRegisterTag, 0);
    }

    private void EmitTupleMappingHelper(string helper, TupleMappingCase mapping, BeamSourceLocation location, int entry)
    {
        var generator = MappingGeneratorLocation(mapping, location);
        StartFunction(helper, 2, generator, exported: false);
        int empty = nextLabel++;
        EmitNonemptyList(0, empty);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
        EmitListParts(0, XRegisterTag, 0, YRegisterTag, 0);
        EmitTuplePart(0, 0, 1);
        EmitTuplePart(0, 1, 2);
        MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
        MoveRegister(YRegisterTag, 1, XRegisterTag, 1);
        EmitCallLine(mapping.Map, generator);
        Op(ExternalCall, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, Import(mapping.Map.Module!, mapping.Map.Function, 3));
        MoveRegister(YRegisterTag, 0, XRegisterTag, 2);
        MoveRegister(YRegisterTag, 1, XRegisterTag, 1);
        MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
        Op(TrimFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
        EmitLine(generator);
        EmitFieldCall(2, entry);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);
        Op(Label, BeamWire.UnsignedTag, empty);
        instructions.WriteByte(Return);
    }
}
