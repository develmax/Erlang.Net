using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalCaseFrameChecks
{
    internal const string TestName = "compiler/beam/emits-source-case-with-independent-frame-owners";
    private const string IncorrectLayout = "Only the returning call arm must allocate a frame.";
    private const string IncorrectResult = "The planned source case changed a value, effect, or failure.";
    private const string Source = """
        -module(beam_terminal_case_frame).
        -record(join_options, {encoding, mode=normal}).
        -export([select/2, from_map/2, open_map/2, ordered/2, ordered_default/2, suffixes/1, unroll/1, records/3, effects/2, recycle/2, annotate/2, nested/2, flatten_fields/2, tagged/2, prefix/2, boolean/2, prepare_call/2, node_kind/1, join_literal/4, record_join/5, order_build/2, build_pure/2, select_member/2, select_open/2, shared_route/2, constructed_calls/2, direct_call_regions/2, prefer_outgoing/2, retire_before/2, keep_result_through_heap/2, split_projected/2, split_tuple/2, build_from_projection/2, privately_projected/2, known_empty_return/2, distinct_projected_inputs/2, sink_entry_fields/2, sink_built_inputs/2, sink_return/1, relocate_captured/2, nested_matched_result/2, nested_bound_result/2, branch_matched_result/2]).
        -record(packed, {first, kept}).
        select(Kind, Input) ->
            case Kind of
                execute -> [[{result, [ready | produce(Input)]}]];
                keep -> [[{result, Input}]]
            end.
        produce(Input) -> erlang:put(source_frame_probe, Input), Input.

        from_map(Input, Option) -> closed_map(Input#{seed => true}, Option).
        closed_map(Input, Option) ->
            case Input of
                #{value := Value} -> {produce(Input), Value, Option};
                #{} -> {Input, Option}
            end.

        open_map(Input, Option) ->
            case Input of
                #{value := Value} -> {produce_after(Input), Value, Option};
                #{} -> {Input, Option}
            end.

        produce_after(Input) -> produce(Input).

        ordered(Input, execute) -> {effect, produce_after(Input)};
        ordered(_, keep = Key) -> {steady, Key}.

        ordered_default(Input, execute) -> {effect, produce_after(Input)};
        ordered_default(Input, Other) -> {Other, Input}.

        suffixes([] = List) -> [List];
        suffixes([_ | Tail] = List) -> [List | suffixes(Tail)].

        unroll({step, Current, Next}) -> [{item, Current} | unroll(Next)];
        unroll(Last) -> [{item, Last}].

        records([Head | Tail], Meta, Acc) -> {node, Meta, Head, records(Tail, Meta, Acc)};
        records([], _, Acc) -> Acc.

        effects(skip, Input) -> {skipped, Input};
        effects(run, Input) ->
            First = produce_after(Input),
            Second = measured(First),
            produce_after(done),
            {First, Second, Input};
        effects(tail, Input) ->
            produce_after(prior),
            produce_after(Input).

        measured(Value) -> {measured, Value}.

        recycle(keep, Input) -> {skipped, Input};
        recycle(run, Input) ->
            First = produce_after(first),
            Next = measured(Input),
            {First, Next}.

        annotate([Head | Tail], Meta) ->
            {cell, Meta, annotate_term(Head, Meta), annotate(Tail, Meta)};
        annotate(Other, Meta) ->
            annotate_term(Other, Meta).

        annotate_term(Value, Meta) -> {value, Meta, Value}.

        nested({branch, {box, Name, none}}, _) -> {empty, Name};
        nested({branch, {box, Name, Port}}, _) -> {present, Name, Port};
        nested({branch, Payload}, Version) -> {fallback, Version, produce_after(Payload)};
        nested({other, Payload}, Version) -> {other, Payload, Version};
        nested({wide, Payload, Extra}, _) -> {wide, Extra, Payload};
        nested(Payload, Version) -> {default, Payload, Version}.

        flatten_fields([Head | Tail], Meta) ->
            [annotate_term(Head, Meta) | flatten_fields(Tail, Meta)];
        flatten_fields(Other, _) ->
            Other.

        tagged({1, Payload}, _) -> {leaf, Payload};
        tagged({2, Payload}, Version) -> {called, produce_after(Payload), Version};
        tagged({3, Payload, Extra}, Version) -> {wide, Payload, Extra, Version};
        tagged(Payload, _) -> {unknown, Payload}.

        prefix({one, Payload}, _) -> {first, Payload};
        prefix({two, Payload}, Version) -> {called, produce_after(Payload), Version};
        prefix({Tag, Payload}, Version) when Tag =/= three -> {generic, Tag, Payload, Version};
        prefix({three, Payload}, Version) -> {three, Payload, Version};
        prefix({wide, Payload, Extra}, _) -> {wide, Extra, Payload};
        prefix(Other, _) -> {unknown, Other}.

        boolean(enabled, Value) -> closed_boolean(true, Value);
        boolean(disabled, Value) -> closed_boolean(false, Value).

        closed_boolean(true, Value) -> {on, produce_after(Value)};
        closed_boolean(false, Value) -> {off, produce_after(Value)}.

        prepare_call(wrap, Value) -> measured({wrapped, [Value]});
        prepare_call(execute, Value) -> {called, produce_after(Value)}.

        node_kind({leaf, _}) -> leaf;
        node_kind({branch, _, _}) -> branch;
        node_kind({interface, _, _}) -> interface;
        node_kind({root, _, _, _}) -> root.

        join_literal(Title, Rest, Body, Encoding) ->
            Text = case Encoding of
                       old -> "old";
                       new -> "new"
                   end,
            [{title, Title}, {value, "prefix:" ++ Text}, {body, Body}] ++ Rest.

        record_join(Key, Mode, Title, Rest, Body) ->
            render_join_options(make_join_options(Key, Mode), Title, Rest, Body).

        make_join_options(Key, Mode) ->
            Encoding = case Key of old -> old; _ -> new end,
            Options = #join_options{encoding=Encoding},
            case Mode of
                valid -> Options;
                tuned -> Options#join_options{mode=tuned};
                invalid -> erlang:exit(invalid_configuration)
            end.

        render_join_options(Options, Title, Rest, Body) ->
            join_record_value(Title, Rest, Body, Options#join_options.encoding).

        join_record_value(Title, Rest, Body, Encoding) ->
            Text = case Encoding of
                       old -> "old";
                       new -> "new"
                   end,
            [{title, Title}, {value, "prefix:" ++ Text}, {body, Body}] ++ Rest.

        order_build(run, Value) -> [{left, [produce_after(Value)]}, {right, [Value]}];
        order_build(keep, Value) -> Value.

        build_pure(Value, Tail) -> {outer, [{left, {prefix, Value}}, {right, Tail} | Tail]}.

        select_member(Values, Key) -> selected_member(lists:keymember(Key, 1, Values), Key).
        selected_member(true, Key) -> {present, produce_after(Key)};
        selected_member(false, Key) -> {absent, Key}.

        select_open(Selector, Key) -> open_member(Selector, Key).
        open_member(true, Key) -> {present, produce_after(Key)};
        open_member(false, Key) -> {absent, Key}.

        shared_route(a, Value) -> {effect, produce_after(Value)};
        shared_route(b, Value) -> identity(Value);
        shared_route(c, Value) -> identity(Value).

        identity(Value) -> Value.

        constructed_calls(run, Value) ->
            Prior = produce_after(Value),
            Finished = consume_wrapped({wrapped, Prior}),
            {done, Finished};
        constructed_calls(keep, Value) -> Value.

        direct_call_regions(Value, Meta) ->
            Prior = produce_after(Value),
            consume_wrapped([{marked, Prior} | Meta]).

        consume_wrapped(Value) -> put(source_frame_probe, Value), Value.

        prefer_outgoing(Value, Tag) ->
            Produced = produce_after(Value),
            take_argument(Tag, [{wrapped, Produced}]).

        take_argument(Tag, Argument) -> {Tag, Argument}.

        retire_before(Value, Tag) ->
            produce_after(Value),
            Returned = consume_wrapped(Tag),
            identity([{returned, Returned}]).

        keep_result_through_heap(Value, Tag) ->
            Previous = produce_after(Value),
            consume_wrapped(Tag, [Tag | Previous]),
            {Tag, Previous}.

        consume_wrapped(Tag, Value) -> put(source_frame_probe, {Tag, Value}), ignored.

        split_projected([Head | Tail], Tag) ->
            Measured = produce_after(Head),
            consume_wrapped(Tag, Measured),
            {Tail, Measured};
        split_projected([], Tag) ->
            Measured = produce_after(Tag),
            {empty, Measured}.

        split_tuple({First, Second}, Tag) ->
            Measured = produce_after(First),
            consume_wrapped(Tag, Measured),
            {Second, Measured};
        split_tuple(Other, Tag) -> {other, Other, Tag}.

        build_from_projection({event, Head, Tail}, Tag) ->
            Measured = consume_wrapped(Tag, [Head | Tail]),
            {Tag, Tail, Measured};
        build_from_projection(Other, Tag) -> {other, Other, Tag}.

        privately_projected(run, Value) -> closed_projected([Value], Value);
        privately_projected(empty, Value) -> closed_projected([], Value).

        known_empty_return(run, Value) -> closed_nil([Value], Value);
        known_empty_return(empty, Value) -> closed_nil([], Value).

        closed_nil([Head | Tail], Tag) ->
            consume_wrapped(Tag, Head),
            Tail;
        closed_nil([], _) -> [].

        sink_entry_fields({tagged, First, Left, Right}, Marker) ->
            consume_wrapped(Marker, First),
            consume_wrapped(Left, Right),
            ok.

        sink_built_inputs({tagged, First, Left, Right}, Marker) ->
            Interim = consume_wrapped(Marker, First),
            consume_wrapped([Left, Right, Interim]).

        nested_matched_result(Value, Kept) ->
            {First, Second} = pair_after(Value),
            {{packed, First, Kept}, [Second]}.

        nested_bound_result(Value, Kept) when is_list(Kept) ->
            {First, Second} = pair_after(Value),
            Inner = #packed{first = First, kept = Kept},
            List = [Inner, Second],
            {state, List, First}.

        branch_matched_result([{wrapped, Value} | _], Kept) ->
            {First, Second} = pair_after(Value),
            {#packed{first = First, kept = Kept}, [Second]};
        branch_matched_result(_, Kept) ->
            {fallback, Kept}.

        pair_after({good, First, Second}) ->
            put(source_frame_probe, First),
            {First, Second};
        pair_after(other) ->
            unexpected.

        relocate_captured(Name, Discard) ->
            consume_wrapped(Discard),
            Saved = consume_wrapped({tagged, Name}),
            consume_wrapped(Saved),
            consume_wrapped(Name),
            {done, consume_wrapped(Saved)}.

        sink_return({tagged, First, Left, Right}) ->
            Interim = produce_after(First),
            {Left, Right, Interim}.

        distinct_projected_inputs(State, {event, Left, Right}) ->
            Observed = consume_wrapped([Left, Right]),
            {State, Observed};
        distinct_projected_inputs(State, Other) -> {State, Other}.

        closed_projected([Head | Tail], Tag) ->
            Measured = produce_after(Head),
            consume_wrapped(Tag, Measured),
            {Tail, Measured};
        closed_projected([], Tag) ->
            Measured = produce_after(Tag),
            {empty, Measured}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, "beam_terminal_case_frame.erl");
        object code = Code(beam);
        object[] body = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        object[] first = ReadFunction("select");
        object[] frames = first.Where(operation => Opcode(operation) == Allocate).ToArray();
        if (first.Length == 0 || Opcode(first[0]) != SelectValue || frames is not [var allocation]
            || (BigInteger)Property(((IEnumerable)Property(allocation, "Operands")).Cast<object>().First(), "Value") != 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] closedMap = ReadFunction("closed_map");
        object[] openMap = ReadFunction("open_map");
        if (closedMap.Length == 0 || Opcode(closedMap[0]) != GetMapElements
            || closedMap.Any(instruction => Opcode(instruction) == IsMap)
            || openMap.Length == 0 || Opcode(openMap[0]) != IsMap || !openMap.Any(instruction => Opcode(instruction) == CaseEnd))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] ordered = ReadFunction("ordered");
        object[] defaulted = ReadFunction("ordered_default");
        if (ordered.Length == 0 || Opcode(ordered[0]) != SelectValue
            || defaulted.Length == 0 || Opcode(defaulted[0]) != ExactEqualTest
            || ordered.Count(instruction => Opcode(instruction) == Allocate) != 1
            || defaulted.Count(instruction => Opcode(instruction) == Allocate) != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] suffixes = ReadFunction("suffixes");
        object[] unroll = ReadFunction("unroll");
        if (suffixes.Length == 0 || Opcode(suffixes[0]) != IsNonemptyList
            || unroll.Length == 0 || Opcode(unroll[0]) != IsTaggedTuple
            || !suffixes.Any(instruction => Opcode(instruction) == GetTail)
            || unroll.Count(instruction => Opcode(instruction) == GetTupleElement) != 2)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] records = ReadFunction("records");
        object[] effects = ReadFunction("effects");
        if (!records.Any(instruction => Opcode(instruction) == GetList)
            || effects.Length == 0 || Opcode(effects[0]) != SelectValue
            || !effects.Any(instruction => Opcode(instruction) == InitYRegisters)
            || !effects.Any(instruction => Opcode(instruction) == TailLocalCallWithFrame))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] recycled = ReadFunction("recycle");
        object[] recycledFrames = recycled.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (recycledFrames is not [var reused]
            || (BigInteger)Property(((IEnumerable)Property(reused, "Operands")).Cast<object>().First(), "Value") != 1
            || !recycled.Any(instruction => Opcode(instruction) == Swap))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] annotated = ReadFunction("annotate");
        if (annotated.Length == 0 || Opcode(annotated[0]) != IsNonemptyList
            || !annotated.Any(instruction => Opcode(instruction) == GetList)
            || !annotated.Any(instruction => Opcode(instruction) == TailLocalCall))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] nested = ReadFunction("nested");
        if (!nested.Any(instruction => Opcode(instruction) == IsTaggedTuple)
            || !nested.Any(instruction => Opcode(instruction) == SelectTupleArity))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] flattened = ReadFunction("flatten_fields");
        if (!flattened.Any(instruction => Opcode(instruction) == TrimFrame))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] tagged = ReadFunction("tagged");
        if (!tagged.Any(instruction => Opcode(instruction) == SelectTupleArity)
            || !tagged.Any(instruction => Opcode(instruction) == SelectValue)
            || tagged.Count(instruction => Opcode(instruction) == Allocate) != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] prefix = ReadFunction("prefix");
        if (!prefix.Any(instruction => Opcode(instruction) == SelectTupleArity)
            || !prefix.Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] closedBoolean = ReadFunction("closed_boolean");
        if (closedBoolean.Length == 0 || Opcode(closedBoolean[0]) != ExactEqualTest
            || closedBoolean.Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] prepare = ReadFunction("prepare_call");
        if (!prepare.Any(instruction => Opcode(instruction) == TailLocalCall)
            || !prepare.Any(instruction => Opcode(instruction) == TestHeap)
            || prepare.Count(instruction => Opcode(instruction) == Allocate) != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] nodeKind = ReadFunction("node_kind");
        if (nodeKind.Any(instruction => Opcode(instruction) == Allocate))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] joined = ReadFunction("join_literal");
        object[] joinedFrames = joined.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (joinedFrames is not [var joinedFrame]
            || (BigInteger)Property(((IEnumerable)Property(joinedFrame, "Operands")).Cast<object>().First(), "Value") != 3
            || joined.Count(instruction => Opcode(instruction) == ExternalCall) != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] recordJoin = ReadFunction("join_record_value");
        if (!recordJoin.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || recordJoin.Any(instruction => Opcode(instruction) is SelectValue or CaseEnd))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] constructed = ReadFunction("order_build");
        int head = FindConstructedTuple(constructed, "left");
        int tail = FindConstructedTuple(constructed, "right");
        if (tail < 0 || head < tail)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] pure = ReadFunction("build_pure");
        int prefixNode = FindConstructedTuple(pure, "prefix");
        int rightNode = FindConstructedTuple(pure, "right");
        int leftNode = FindConstructedTuple(pure, "left");
        if (prefixNode < 0 || rightNode <= prefixNode || leftNode <= rightNode
            || FindConstructedTuple(pure, "outer") <= leftNode
            || pure.Any(instruction => Opcode(instruction) == Allocate))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] selectedMember = ReadFunction("selected_member");
        if (!selectedMember.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || selectedMember.Any(instruction => Opcode(instruction) == SelectValue)
            || !ReadFunction("open_member").Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        if (ReadFunction("shared_route").Count(instruction => Opcode(instruction) == TailLocalCall) != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] callRegions = ReadFunction("constructed_calls");
        int firstCall = Array.FindIndex(callRegions, instruction => Opcode(instruction) == LocalCall);
        int secondCall = Array.FindLastIndex(callRegions, instruction => Opcode(instruction) == LocalCall);
        int argumentTuple = FindConstructedTuple(callRegions, "wrapped");
        if (firstCall < 0 || argumentTuple <= firstCall || secondCall <= argumentTuple
            || FindConstructedTuple(callRegions, "done") <= secondCall)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] outgoing = ReadFunction("prefer_outgoing");
        object list = outgoing.Single(instruction => Opcode(instruction) == PutList);
        object last = ((IEnumerable)Property(list, "Operands")).Cast<object>().Last();
        object[] before = ReadFunction("retire_before");
        int release = Array.FindIndex(before, instruction => Opcode(instruction) == TrimFrame);
        if ((string)Property(last, "Kind") != "x" || (BigInteger)Property(last, "Value") != 1
            || release <= Array.FindIndex(before, instruction => Opcode(instruction) == LocalCall)
            || release >= Array.FindLastIndex(before, instruction => Opcode(instruction) == LocalCall))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] liveRoot = ReadFunction("keep_result_through_heap");
        int afterCall = Array.FindIndex(liveRoot, instruction => Opcode(instruction) == LocalCall);
        int beforeCall = Array.FindLastIndex(liveRoot, instruction => Opcode(instruction) == LocalCall);
        int between = Array.FindIndex(liveRoot, instruction => Opcode(instruction) == PutList);
        if (afterCall < 0 || between <= afterCall || beforeCall <= between
            || !liveRoot.Skip(afterCall + 1).Take(between - afterCall - 1).Any(instruction =>
                Opcode(instruction) == TestHeap
                && (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().Last(), "Value") == 1))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] splitEntry = ReadFunction("split_projected");
        int consTest = Array.FindIndex(splitEntry, instruction => Opcode(instruction) == IsNonemptyList);
        int firstFrame = Array.FindIndex(splitEntry, instruction => Opcode(instruction) == Allocate);
        int nilTest = Array.FindIndex(splitEntry, instruction => Opcode(instruction) == IsNil);
        int lastFrame = Array.FindLastIndex(splitEntry, instruction => Opcode(instruction) == Allocate);
        if (consTest < 0 || firstFrame <= consTest || nilTest <= firstFrame || lastFrame <= nilTest)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] tupleEntry = ReadFunction("split_tuple");
        int tupleTest = Array.FindIndex(tupleEntry, instruction => Opcode(instruction) == IsTuple);
        int arityTest = Array.FindIndex(tupleEntry, instruction => Opcode(instruction) == TestArity);
        if (tupleTest < 0 || arityTest <= tupleTest
            || Array.FindIndex(tupleEntry, instruction => Opcode(instruction) == Allocate) <= arityTest)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] projectedInputs = ReadFunction("build_from_projection");
        int tagTest = Array.FindIndex(projectedInputs, instruction => Opcode(instruction) == IsTaggedTuple);
        int readField = Array.FindLastIndex(projectedInputs, instruction => Opcode(instruction) == GetTupleElement);
        int listBuild = Array.FindIndex(projectedInputs, instruction => Opcode(instruction) == PutList);
        int callWithBuiltInput = Array.FindIndex(projectedInputs, instruction => Opcode(instruction) == LocalCall);
        int initialHeap = Array.FindIndex(projectedInputs, instruction => Opcode(instruction) == AllocateHeap);
        if (tagTest < 0 || initialHeap <= tagTest || readField <= initialHeap
            || listBuild <= readField || callWithBuiltInput <= listBuild)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] closedEntry = ReadFunction("closed_projected");
        if (!closedEntry.Any(instruction => Opcode(instruction) == IsNonemptyList)
            || closedEntry.Any(instruction => Opcode(instruction) == IsNil)
            || !ReadFunction("split_projected").Any(instruction => Opcode(instruction) == IsNil))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] nilReturn = ReadFunction("closed_nil");
        if (!nilReturn.Any(instruction => Opcode(instruction) == IsNonemptyList)
            || nilReturn.Any(instruction => Opcode(instruction) == IsNil
                || Opcode(instruction) == Move
                && ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
                    .Any(operand => (string)Property(operand, "Kind") == "nil")))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] sunkEntry = ReadFunction("sink_entry_fields");
        int effect = Array.FindIndex(sunkEntry, instruction => Opcode(instruction) == LocalCall);
        int nextEffect = Array.FindLastIndex(sunkEntry, instruction => Opcode(instruction) == LocalCall);
        object entryAllocation = sunkEntry.Single(instruction => Opcode(instruction) == Allocate);
        object entryLive = ((IEnumerable)Property(entryAllocation, "Operands")).Cast<object>().Last();
        if ((BigInteger)Property(entryLive, "Value") != 2 || effect < 0 || nextEffect <= effect
            || sunkEntry.Skip(effect + 1).Take(nextEffect - effect - 1)
                .Count(instruction => Opcode(instruction) == GetTupleElement) != 2)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] sunkHeap = ReadFunction("sink_built_inputs");
        int firstEffect = Array.FindIndex(sunkHeap, instruction => Opcode(instruction) == LocalCall);
        int lateHeap = Array.FindIndex(sunkHeap, instruction => Opcode(instruction) == TestHeap);
        int lateField = Array.FindLastIndex(sunkHeap, instruction => Opcode(instruction) == GetTupleElement);
        if (firstEffect < 0 || lateHeap <= firstEffect || lateField <= lateHeap)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] sunkReturn = ReadFunction("sink_return");
        int producingCall = Array.FindIndex(sunkReturn, instruction => Opcode(instruction) == LocalCall);
        int returnHeap = Array.FindIndex(sunkReturn, instruction => Opcode(instruction) == TestHeap);
        if (producingCall < 0 || returnHeap <= producingCall
            || sunkReturn.Skip(returnHeap + 1).Count(instruction => Opcode(instruction) == GetTupleElement) != 2
            || (BigInteger)Property(((IEnumerable)Property(sunkReturn[returnHeap], "Operands")).Cast<object>().Last(), "Value") != 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] relocated = ReadFunction("relocate_captured");
        int compactFrame = Array.FindIndex(
            relocated,
            instruction =>
            Opcode(instruction) == TrimFrame
            && ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
                .Select(operand => (BigInteger)Property(operand, "Value")).SequenceEqual([BigInteger.One, BigInteger.One])
        );
        if (compactFrame < 1 || Opcode(relocated[compactFrame - 1]) != Move
            || ((IEnumerable)Property(relocated[compactFrame - 1], "Operands")).Cast<object>().ToArray() is not [var low, var high]
            || (string)Property(low, "Kind") != "y" || (BigInteger)Property(low, "Value") != BigInteger.Zero
            || (string)Property(high, "Kind") != "y" || (BigInteger)Property(high, "Value") != BigInteger.One)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] matchedResult = ReadFunction("nested_matched_result");
        int matchedAllocation = Array.FindIndex(matchedResult, instruction => Opcode(instruction) == Allocate);
        if (matchedAllocation < 0
            || (BigInteger)Property(((IEnumerable)Property(matchedResult[matchedAllocation], "Operands")).Cast<object>().First(), "Value")
                != BigInteger.One)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] boundResult = ReadFunction("nested_bound_result");
        int boundAllocation = Array.FindIndex(boundResult, instruction => Opcode(instruction) == Allocate);
        int guardTest = Array.FindIndex(boundResult, instruction => Opcode(instruction) == IsList);
        if (guardTest < 0 || boundAllocation <= guardTest
            || (BigInteger)Property(((IEnumerable)Property(boundResult[boundAllocation], "Operands")).Cast<object>().First(), "Value")
                != BigInteger.One)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(beam);

        int FindConstructedTuple(object[] instructions, string tag)
        {
            int atomIndex = Array.IndexOf(atoms, tag);

            return Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == PutTuple
                && ((IEnumerable)Property(instruction, "Operands")).Cast<object>().Any(operand =>
                    (string)Property(operand, "Kind") == "atom"
                    && (BigInteger)Property(operand, "Value") == atomIndex)
            );
        }
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term marker = Term.A("marker");
            Term input = Term.List(marker);
            Term result = Term.A("result");
            if (!Term.List(Term.List(Term.Tuple(result, input))).Equals(await module.Invoke(
                context,
                "select",
                Term.A("keep"),
                input
            ))
                || !Term.List(Term.List(Term.Tuple(result, Term.List(Term.A("ready"), marker)))).Equals(await module.Invoke(
                    context,
                    "select",
                    Term.A("execute"),
                    input
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "select",
                    marker,
                    input
                );
                throw new InvalidOperationException(IncorrectResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), marker)))
            {
            }

            Term map = new MapTerm([new(Term.A("value"), marker)]);
            Term seeded = new MapTerm([new(Term.A("value"), marker), new(Term.A("seed"), Term.A("true"))]);
            if (!Term.Tuple(seeded, marker, input).Equals(await module.Invoke(
                context,
                "from_map",
                map,
                input
            ))
                || !Term.Tuple(map, marker, input).Equals(await module.Invoke(
                    context,
                    "open_map",
                    map,
                    input
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "open_map",
                    marker,
                    input
                );
                throw new InvalidOperationException(IncorrectResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), marker)))
            {
            }

            if (!Term.Tuple(Term.A("effect"), input).Equals(await module.Invoke(
                context,
                "ordered",
                input,
                Term.A("execute")
            ))
                || !Term.Tuple(Term.A("steady"), Term.A("keep")).Equals(await module.Invoke(
                    context,
                    "ordered",
                    input,
                    Term.A("keep")
                ))
                || !Term.Tuple(marker, input).Equals(await module.Invoke(
                    context,
                    "ordered_default",
                    input,
                    marker
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "ordered",
                    input,
                    marker
                );
                throw new InvalidOperationException(IncorrectResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("function_clause")))
            {
            }

            Term item = Term.A("item");
            Term step = Term.A("step");
            if (!Term.List(input, Nil.Value).Equals(await module.Invoke(context, "suffixes", input))
                || !Term.List(Term.Tuple(item, marker), Term.Tuple(item, input)).Equals(await module.Invoke(
                    context,
                    "unroll",
                    Term.Tuple(step, marker, input)
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(
                Term.A("node"),
                item,
                marker,
                marker
            ).Equals(await module.Invoke(
                context,
                "records",
                input,
                item,
                marker
            ))
                || !Term.Tuple(input, Term.Tuple(Term.A("measured"), input), input).Equals(await module.Invoke(
                    context,
                    "effects",
                    Term.A("run"),
                    input
                ))
                || !input.Equals(await module.Invoke(
                    context,
                    "effects",
                    Term.A("tail"),
                    input
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Term.A("first"), Term.Tuple(Term.A("measured"), input)).Equals(await module.Invoke(
                context,
                "recycle",
                Term.A("run"),
                input
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(
                Term.A("cell"),
                item,
                Term.Tuple(Term.A("value"), item, marker),
                Term.Tuple(Term.A("value"), item, Nil.Value)
            ).Equals(await module.Invoke(
                context,
                "annotate",
                input,
                item
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term branch = Term.A("branch");
            Term box = Term.A("box");
            Term version = Term.A("version");
            if (!Term.Tuple(Term.A("empty"), marker).Equals(await module.Invoke(
                context,
                "nested",
                Term.Tuple(branch, Term.Tuple(box, marker, Term.A("none"))),
                version
            ))
                || !Term.Tuple(Term.A("present"), marker, input).Equals(await module.Invoke(
                    context,
                    "nested",
                    Term.Tuple(branch, Term.Tuple(box, marker, input)),
                    version
                ))
                || !Term.Tuple(Term.A("fallback"), version, input).Equals(await module.Invoke(
                    context,
                    "nested",
                    Term.Tuple(branch, input),
                    version
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.List(Term.Tuple(Term.A("value"), item, marker)).Equals(await module.Invoke(
                context,
                "flatten_fields",
                input,
                item
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Term.A("leaf"), marker).Equals(await module.Invoke(
                context,
                "tagged",
                Term.Tuple(Term.I(1), marker),
                version
            ))
                || !Term.Tuple(Term.A("called"), marker, version).Equals(await module.Invoke(
                    context,
                    "tagged",
                    Term.Tuple(Term.I(2), marker),
                    version
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Term.A("called"), input, version).Equals(await module.Invoke(
                context,
                "prefix",
                Term.Tuple(Term.A("two"), input),
                version
            ))
                || !Term.Tuple(Term.A("three"), input, version).Equals(await module.Invoke(
                    context,
                    "prefix",
                    Term.Tuple(Term.A("three"), input),
                    version
                ))
                || !Term.Tuple(
                    Term.A("generic"),
                    marker,
                    input,
                    version
                ).Equals(await module.Invoke(
                    context,
                    "prefix",
                    Term.Tuple(marker, input),
                    version
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Term.A("on"), input).Equals(await module.Invoke(
                context,
                "boolean",
                Term.A("enabled"),
                input
            ))
                || !Term.Tuple(Term.A("off"), marker).Equals(await module.Invoke(
                    context,
                    "boolean",
                    Term.A("disabled"),
                    marker
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Term.A("measured"), Term.Tuple(Term.A("wrapped"), input)).Equals(await module.Invoke(
                context,
                "prepare_call",
                Term.A("wrap"),
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.A("interface").Equals(await module.Invoke(
                context,
                "node_kind",
                Term.Tuple(Term.A("interface"), marker, input)
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term suffix = Term.List(Term.A("tail"));
            Term encoded = Term.List("prefix:new".Select(character => (Term)Term.I(character)).ToArray());
            if (!Term.List(
                Term.Tuple(Term.A("title"), marker),
                Term.Tuple(Term.A("value"), encoded),
                Term.Tuple(Term.A("body"), input),
                Term.A("tail")
            ).Equals(await module.Invoke(
                context,
                "join_literal",
                marker,
                suffix,
                input,
                Term.A("new")
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "join_literal",
                    marker,
                    suffix,
                    input,
                    marker
                );
                throw new InvalidOperationException(IncorrectResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), marker)))
            {
            }

            Term expected = Term.List(
                Term.Tuple(Term.A("title"), marker),
                Term.Tuple(Term.A("value"), encoded),
                Term.Tuple(Term.A("body"), input),
                Term.A("tail")
            );
            if (!expected.Equals(await module.Invoke(
                context,
                "record_join",
                Term.A("new"),
                Term.A("tuned"),
                marker,
                suffix,
                input
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term both = Term.List(
                Term.Tuple(Term.A("left"), input),
                Term.Tuple(Term.A("right"), input)
            );
            if (!both.Equals(await module.Invoke(
                context,
                "order_build",
                Term.A("run"),
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term pureResult = Term.Tuple(
                Term.A("outer"),
                new Cons(
                Term.Tuple(Term.A("left"), Term.Tuple(Term.A("prefix"), marker)),
                new Cons(Term.Tuple(Term.A("right"), marker), marker)
            )
            );
            if (!pureResult.Equals(await module.Invoke(
                context,
                "build_pure",
                marker,
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term keys = Term.List(Term.Tuple(marker, Nil.Value));
            if (!Term.Tuple(Term.A("present"), marker).Equals(await module.Invoke(
                context,
                "select_member",
                keys,
                marker
            ))
                || !Term.Tuple(Term.A("absent"), marker).Equals(await module.Invoke(
                    context,
                    "select_member",
                    Nil.Value,
                    marker
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!marker.Equals(await module.Invoke(
                context,
                "shared_route",
                Term.A("b"),
                marker
            ))
                || !marker.Equals(await module.Invoke(
                    context,
                    "shared_route",
                    Term.A("c"),
                    marker
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term wrapped = Term.Tuple(Term.A("wrapped"), marker);
            if (!Term.Tuple(Term.A("done"), wrapped).Equals(await module.Invoke(
                context,
                "constructed_calls",
                Term.A("run"),
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(result, Term.List(wrapped)).Equals(await module.Invoke(
                context,
                "prefer_outgoing",
                marker,
                result
            ))
                || !Term.List(Term.Tuple(Term.A("returned"), result)).Equals(await module.Invoke(
                    context,
                    "retire_before",
                    marker,
                    result
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(result, input).Equals(await module.Invoke(
                context,
                "keep_result_through_heap",
                input,
                result
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Nil.Value, marker).Equals(await module.Invoke(
                context,
                "split_projected",
                input,
                result
            ))
                || !Term.Tuple(Term.A("empty"), result).Equals(await module.Invoke(
                    context,
                    "split_projected",
                    Nil.Value,
                    result
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(input, marker).Equals(await module.Invoke(
                context,
                "split_tuple",
                Term.Tuple(marker, input),
                result
            ))
                || !Term.Tuple(Term.A("other"), marker, result).Equals(await module.Invoke(
                    context,
                    "split_tuple",
                    marker,
                    result
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(result, input, Term.A("ignored")).Equals(await module.Invoke(
                context,
                "build_from_projection",
                Term.Tuple(Term.A("event"), marker, input),
                result
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Nil.Value, marker).Equals(await module.Invoke(
                context,
                "privately_projected",
                Term.A("run"),
                marker
            ))
                || !Term.Tuple(Term.A("empty"), marker).Equals(await module.Invoke(
                    context,
                    "privately_projected",
                    Term.A("empty"),
                    marker
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Nil.Value.Equals(await module.Invoke(
                context,
                "known_empty_return",
                Term.A("empty"),
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(result, Term.List(marker, input)).Equals(await module.Invoke(
                context,
                "distinct_projected_inputs",
                result,
                Term.Tuple(Term.A("event"), marker, input)
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.A("ok").Equals(await module.Invoke(
                context,
                "sink_entry_fields",
                Term.Tuple(
                    Term.A("tagged"),
                    marker,
                    input,
                    result
                ),
                result
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(input, result, marker).Equals(await module.Invoke(
                context,
                "sink_return",
                Term.Tuple(
                    Term.A("tagged"),
                    marker,
                    input,
                    result
                )
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(Term.A("done"), Term.Tuple(Term.A("tagged"), input)).Equals(await module.Invoke(
                context,
                "relocate_captured",
                input,
                result
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            if (!Term.Tuple(
                Term.Tuple(Term.A("packed"), input, marker),
                Term.List(result)
            ).Equals(await module.Invoke(
                context,
                "nested_matched_result",
                Term.Tuple(Term.A("good"), input, result),
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term keptList = Term.List(marker);
            if (!Term.Tuple(
                Term.A("state"),
                Term.List(Term.Tuple(Term.A("packed"), input, keptList), result),
                input
            ).Equals(await module.Invoke(
                context,
                "nested_bound_result",
                Term.Tuple(Term.A("good"), input, result),
                keptList
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term wrappedResultInput = Term.Tuple(Term.A("wrapped"), Term.Tuple(Term.A("good"), input, result));
            if (!Term.Tuple(Term.Tuple(Term.A("packed"), input, marker), Term.List(result)).Equals(await module.Invoke(
                context,
                "branch_matched_result",
                Term.List(wrappedResultInput),
                marker
            )) || !Term.Tuple(Term.A("fallback"), marker).Equals(await module.Invoke(
                context,
                "branch_matched_result",
                Nil.Value,
                marker
            )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });

        object[] ReadFunction(string name)
        {
            int start = Array.FindIndex(
                body,
                instruction => Opcode(instruction) == FunctionInfo
                && ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray() is [_, var atom, _]
                && atoms[(int)(BigInteger)Property(atom, "Value")] == name
            );

            return start < 0 ? [] : body.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) is not (Line or Label)).ToArray();
        }
    }

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
