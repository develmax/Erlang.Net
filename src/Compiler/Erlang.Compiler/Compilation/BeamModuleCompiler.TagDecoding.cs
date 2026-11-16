using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TagDecoding(TaggedPayload Plain, TaggedPayload Wrapped, Expr.Call Recur);
    private sealed record TaggedPayload(Expr.Call Test, Expr.Call Tag, Expr.Call Decode, int UnpackLine);

    private static Expr.Call? ReadTagMatch(FunctionDefinition function)
    {
        if (function.Clauses is not [var one, var many, var wrapped, var list, var empty, var wrong]
            || function.Clauses.Any(clause => clause.Guard is not null)
            || !MatchesTagStep(one, function.Name, wrapped: false, terminal: true)
            || !MatchesTagStep(many, function.Name, wrapped: false, terminal: false)
            || !MatchesTagStep(wrapped, function.Name, wrapped: true, terminal: false)
            || list is not
            {
                Patterns: [AliasPattern { Left: Pattern.List { Items: [Pattern.Tuple
                    { Items: [Pattern.Variable tag, Pattern.Variable ignored] }],
                    Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard } }, Right: Pattern.Variable original },
                    Pattern.List { Items: [Pattern.Variable repeated], Tail: null }],
                Body: Expr.Variable returned
            }
            || !DistinctBindings(tag, ignored, original) || repeated.Name != tag.Name || returned.Name != original.Name
            || empty is not { Patterns: [Pattern.Variable input, var nil], Body: Expr.Variable unchanged }
            || input.Name == VariableScopeNames.Wildcard || input.Name != unchanged.Name || !IsEmptyListPattern(nil)
            || wrong is not
            {
                Patterns: [AliasPattern { Left: Pattern.Tuple { Items: [Pattern.Variable got, Pattern.Variable payload] },
                    Right: Pattern.Variable tlv }, Pattern.List { Items: [Pattern.Variable expected], Tail: Pattern.Variable rest }],
                Body: Expr.Call { Function: "exit", Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [var reason] } exit
            }
            || !DistinctBindings(got, payload, tlv, expected, rest)
            || !MatchesWrongTagReason(reason, expected.Name, got.Name, tlv.Name))
        {
            return null;
        }

        return exit;
    }

    private static bool MatchesTagStep(Clause clause, string function, bool wrapped, bool terminal)
    {
        if (clause.Patterns is not [var input, Pattern.List { Items: [Pattern.Variable repeated] } tags])
        {
            return false;
        }
        if (wrapped)
        {
            if (input is not Pattern.List { Items: [var element], Tail: null })
            {
                return false;
            }
            input = element;
        }
        if (input is not Pattern.Tuple { Items: [Pattern.Variable tag, Pattern.Variable value] }
            || !DistinctBindings(tag, value) || tag.Name != repeated.Name)
        {
            return false;
        }

        if (terminal)
        {
            return tags.Tail is null && clause.Body is Expr.Variable direct && direct.Name == value.Name;
        }

        return tags.Tail is Pattern.Variable tail && DistinctBindings(tag, value, tail)
            && clause.Body is Expr.Call { Module: null, DynamicRemote: false,
                Arguments: [Expr.Variable payload, Expr.Variable remaining] } call
            && call.Function == function && payload.Name == value.Name && remaining.Name == tail.Name;
    }

    private static bool MatchesWrongTagReason(Expr reason, string expected, string got, string tlv) =>
        reason is Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "error" } },
            Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "asn1" } },
                Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "wrong_tag" } },
                    Expr.Tuple { Items: [
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "expected" } }, Expr.Variable expectedValue] },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "got" } },
                            Expr.Variable actualTag, Expr.Variable actualTlv] }
                    ] }] }] }] }
        && expectedValue.Name == expected && actualTag.Name == got && actualTlv.Name == tlv;

    private void EmitTagMatch(Expr.Call exit, int entry, BeamSourceLocation location)
    {
        int functionInfo = nextLabel - 2;
        int listFallback = nextLabel++;
        int tuple = nextLabel++;
        int recurse = nextLabel++;
        int empty = nextLabel++;
        int wrongTag = nextLabel++;

        EmitNonemptyList(0, tuple);
        EmitListParts(0, XRegisterTag, 2, XRegisterTag, 3);
        EmitTupleCheck(2, 2, empty);
        EmitTuplePart(2, 0, 4);
        EmitNilTest(XRegisterTag, 3, listFallback);
        EmitNonemptyList(1, empty);
        EmitListParts(1, XRegisterTag, 3, XRegisterTag, 5);
        EmitRegisterEquality(3, 4, listFallback);
        EmitTuplePart(2, 1, 0);
        MoveRegister(XRegisterTag, 5, XRegisterTag, 1);
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);

        Op(Label, BeamWire.UnsignedTag, listFallback);
        EmitNonemptyList(1, empty);
        EmitListParts(1, XRegisterTag, 2, XRegisterTag, 3);
        EmitNilTest(XRegisterTag, 3, empty);
        EmitRegisterEquality(2, 4, empty);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, tuple);
        EmitTupleCheck(0, 2, empty);
        EmitNonemptyList(1, empty);
        EmitTuplePart(0, 0, 2);
        EmitTuplePart(0, 1, 3);
        EmitListParts(1, XRegisterTag, 4, XRegisterTag, 5);
        EmitNilTest(XRegisterTag, 5, recurse);
        EmitRegisterEquality(4, 2, empty);
        MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, recurse);
        EmitRegisterEquality(4, 2, empty);
        MoveRegister(XRegisterTag, 5, XRegisterTag, 1);
        MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);

        Op(Label, BeamWire.UnsignedTag, empty);
        EmitNilTest(XRegisterTag, 1, wrongTag);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, wrongTag);
        EmitTupleCheck(0, 2, functionInfo);
        EmitNonemptyList(1, functionInfo);
        Op(TestHeap, BeamWire.UnsignedTag, 19);
        Operand(BeamWire.UnsignedTag, 2);
        EmitTuplePart(0, 0, 2);
        Op(GetHead, XRegisterTag, 1);
        Operand(XRegisterTag, 1);
        EmitTagMatchTuple(1, (AtomTag, Intern("expected")), (XRegisterTag, 1));
        EmitTagMatchTuple(0, (AtomTag, Intern("got")), (XRegisterTag, 2), (XRegisterTag, 0));
        EmitProtectedPair(XRegisterTag, 1);
        EmitProtectedPair(AtomTag, Intern("wrong_tag"));
        EmitProtectedPair(AtomTag, Intern("asn1"));
        EmitProtectedPair(AtomTag, Intern("error"));
        EmitCallLine(exit, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, exit.Function, 1));
    }

    private void EmitTagMatchTuple(int destination, params (int Tag, int Value)[] operands)
    {
        Op(PutTuple, XRegisterTag, destination);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, operands.Length);
        foreach (var operand in operands)
        {
            Operand(operand.Tag, operand.Value);
        }
    }

    private static TagDecoding? ReadTagDecoding(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            { Guard: null, Patterns: [Pattern.Variable shape1, Pattern.Variable content1, var nil], Body: var plain },
            { Guard: null, Patterns: [Pattern.Variable shape2,
                Pattern.Tuple { Items: [Pattern.Variable tag2, Pattern.Variable content2] },
                Pattern.List { Items: [Pattern.Variable repeated2], Tail: null }], Body: var wrapped },
            { Guard: null, Patterns: [Pattern.Variable shape3,
                Pattern.Tuple { Items: [Pattern.Variable tag3, Pattern.List { Items: [Pattern.Variable child], Tail: null }] },
                Pattern.List { Items: [Pattern.Variable repeated3], Tail: Pattern.Variable rest }],
                Body: Expr.Call { Module: null, Arguments: [Expr.Variable input, Expr.Variable value, Expr.Variable tail] } recur }
        ] || !IsEmptyListPattern(nil) || !DistinctBindings(shape1, content1) || !DistinctBindings(shape2, tag2, content2)
            || !DistinctBindings(shape3, tag3, child, rest) || tag2.Name != repeated2.Name || tag3.Name != repeated3.Name
            || input.Name != shape3.Name || value.Name != child.Name || tail.Name != rest.Name
            || ReadTaggedPayload(plain, shape1, content1, null) is not { } direct
            || ReadTaggedPayload(wrapped, shape2, content2, tag2) is not { } unpacked)
        {
            return null;
        }

        return new(direct, unpacked, recur);
    }

    private static TaggedPayload? ReadTaggedPayload(Expr body, Pattern.Variable shape, Pattern.Variable content, Pattern.Variable? boundTag)
    {
        if (body is not Expr.Case
            {
                Value: Expr.Call { Module: null, Arguments: [Expr.Variable tested] } test,
                Clauses: [
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }], Body: var success },
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                        Body: Expr.Call { Module: null, Arguments: [Expr.Variable input, Expr.Tuple { Items: [
                            Expr.Call { Module: null, Arguments: [Expr.Variable tagged] } tag, Expr.Variable packed] }] } decode }
                ]
            } || tested.Name != shape.Name || input.Name != shape.Name || tagged.Name != shape.Name || packed.Name != content.Name)
        {
            return null;
        }

        string payload = content.Name;
        int line = 0;
        if (boundTag is not null)
        {
            if (success is not Expr.Sequence { Items: [
                Expr.Match { Pattern: Pattern.List { Items: [Pattern.Variable child], Tail: null }, Value: Expr.Variable source } match,
                var next] } || source.Name != content.Name || !DistinctBindings(child, shape, content, boundTag))
            {
                return null;
            }
            payload = child.Name;
            line = match.SourceLine;
            success = next;
        }

        if (success is not Expr.Call { Module: null, Arguments: [Expr.Variable direct, Expr.Variable decoded] } call
            || call.Function != decode.Function || direct.Name != shape.Name || decoded.Name != payload)
        {
            return null;
        }

        return new(test, tag, decode, line);
    }

    private void EmitTagDecoding(TagDecoding flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Recur.Function, 3), out int recur))
        {
            throw new NotSupportedException(Unsupported);
        }

        int functionInfo = nextLabel - 2;
        int plainValue = nextLabel++;
        int tagged = nextLabel++;
        int wrappedValue = nextLabel++;
        int nested = nextLabel++;
        int badMatch = nextLabel++;
        int badPlain = booleanChoiceFunctions.Contains(flow.Plain.Test.Function) ? 0 : nextLabel++;
        int badWrapped = booleanChoiceFunctions.Contains(flow.Wrapped.Test.Function) ? 0 : nextLabel++;

        EmitNilTest(XRegisterTag, 2, tagged);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
        EmitTaggedPayload(flow.Plain, location, entries, plainValue, badPlain, null);

        Op(Label, BeamWire.UnsignedTag, tagged);
        EmitTupleCheck(1, 2, functionInfo);
        EmitNonemptyList(2, functionInfo);
        EmitTuplePart(1, 0, 3);
        EmitTuplePart(1, 1, 4);
        EmitListParts(2, XRegisterTag, 5, XRegisterTag, 6);
        EmitNilTest(XRegisterTag, 6, nested);
        EmitRegisterEquality(5, 3, functionInfo);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 5);
        MoveRegister(XRegisterTag, 4, YRegisterTag, 0);
        MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
        EmitTaggedPayload(flow.Wrapped, CallLocation(flow.Plain.Tag, location), entries, wrappedValue, badWrapped, badMatch);

        Op(Label, BeamWire.UnsignedTag, nested);
        EmitNonemptyList(4, functionInfo);
        EmitListParts(4, XRegisterTag, 7, XRegisterTag, 4);
        EmitNilTest(XRegisterTag, 4, functionInfo);
        EmitRegisterEquality(5, 3, functionInfo);
        MoveRegister(XRegisterTag, 6, XRegisterTag, 2);
        MoveRegister(XRegisterTag, 7, XRegisterTag, 1);
        Op(TailLocalCall, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, recur);

        Op(Label, BeamWire.UnsignedTag, badMatch);
        var previous = CallLocation(flow.Wrapped.Tag, location);
        EmitArithmeticLine(flow.Wrapped.UnpackLine, ref previous);
        Op(BadMatch, YRegisterTag, 0);
        EmitInvalidPayloadCase(badPlain);
        EmitInvalidPayloadCase(badWrapped);
    }

    private void EmitTaggedPayload(TaggedPayload payload, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries, int primitive, int invalid, int? badMatch)
    {
        if (!entries.TryGetValue((payload.Test.Function, 1), out int test)
            || !entries.TryGetValue((payload.Tag.Function, 1), out int tag)
            || !entries.TryGetValue((payload.Decode.Function, 2), out int decode))
        {
            throw new NotSupportedException(Unsupported);
        }

        EmitCallLine(payload.Test, location);
        EmitFieldCall(1, test);
        EmitAtomTest(0, "true", primitive);
        if (badMatch is int failure)
        {
            Op(IsNonemptyList, LabelTag, failure);
            Operand(YRegisterTag, 0);
            Op(GetList, YRegisterTag, 0);
            Operand(XRegisterTag, 0);
            Operand(XRegisterTag, 1);
            EmitNilTest(XRegisterTag, 1, failure);
            MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
        }
        else
        {
            MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
        }
        MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
        EmitTagTail(decode);

        Op(Label, BeamWire.UnsignedTag, primitive);
        if (invalid != 0)
        {
            EmitAtomTest(0, "false", invalid);
        }
        MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
        EmitCallLine(payload.Tag, CallLocation(payload.Test, location));
        EmitFieldCall(1, tag);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutTuple, XRegisterTag, 1);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(XRegisterTag, 0);
        Operand(YRegisterTag, 0);
        MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
        EmitTagTail(decode);
    }

    private void EmitTagTail(int entry)
    {
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 2);
    }

    private void EmitNilTest(int tag, int register, int failure)
    {
        Op(IsNil, LabelTag, failure);
        Operand(tag, register);
    }

    private void EmitRegisterEquality(int left, int right, int failure)
    {
        Op(ExactEqualTest, LabelTag, failure);
        Operand(XRegisterTag, left);
        Operand(XRegisterTag, right);
    }

    private void EmitInvalidPayloadCase(int label)
    {
        if (label != 0)
        {
            Op(Label, BeamWire.UnsignedTag, label);
            Op(CaseEnd, XRegisterTag, 0);
        }
    }
}
