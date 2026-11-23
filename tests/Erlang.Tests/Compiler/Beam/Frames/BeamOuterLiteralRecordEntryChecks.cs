using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOuterLiteralRecordEntryChecks
{
    internal const string TestName = "compiler/beam/outer-record-literal-entry-before-frame";
    private const string MissingDispatch = "A disjoint known record ID did not dispatch before branch allocation.";
    private const string IncorrectDispatch = "Repeated record IDs were treated as disjoint entry arms.";
    private const string NestedUpdateResult = "Use the checked nested update result directly in the outer update.";
    private const string EarlyProjection = "Defer the second effect's field read from the already retained record.";
    private const string DiscardedEffectValue = "Select the finite effect arms without keeping their unused results.";
    private const string BranchFieldRoots = "Use one checked tuple slot across an effect when a branch needs several of its fields.";
    private const string SelectedResultSlot = "Store both selected results in the continuation slot before cleanup.";
    private const string SelectedFailures = "Lay out mixed cold failures by the selected control flow.";
    private const string ColumnOrder = "Test the selected constructor fields before the remaining pattern arguments and reuse the consumed selector.";
    private const string Source = """
        -module(beam_outer_literal_record_entry).
        -export([run/2, overlap/2, ordered/2, update/2, effects/2, branch_fields/2, joined/2, cold/2]).
        -record(event,{id,body}).
        -record(command,{kind,item}).
        -record(state,{data,spare}).
        -record(item,{quantity,payload}).
        -record(branch_data,{key,value,timer}).
        run(#event{id=1,body=#command{kind=accept,item=Item}}, State=#state{}) ->
            service:emit(Item),
            {ok,State};
        run(#event{id=4,body=#command{kind=accept,item=Item}}, State=#state{}) ->
            service:discard(Item),
            {ok,State};
        run(Event=#event{}, State=#state{}) ->
            service:fallback(Event),
            {ok,State}.
        overlap(#event{id=1,body=#command{kind=accept,item=Item}}, State=#state{}) ->
            service:emit(Item),
            {ok,State};
        overlap(#event{id=1,body=#command{kind=drop,item=Item}}, State=#state{}) ->
            service:discard(Item),
            {ok,State};
        overlap(Event=#event{}, State=#state{}) ->
            service:fallback(Event),
            {ok,State}.

        ordered(#event{id = 1, body = #command{kind = accept, item = Item}},
                #state{data = {mapping, Key}}) ->
            {found, Item, Key};
        ordered(#event{id = 2, body = #command{kind = accept}}, #state{}) ->
            cleared;
        ordered(#event{}, #state{}) ->
            ignored.

        update(#event{id = 1, body = #command{kind = accept}}, State = #state{data = Item, spare = Spare}) ->
            service:check(Item),
            service:check(Spare),
            {ok, State#state{data = Item#item{quantity = 0}}};
        update(#event{id = 2, body = #command{kind = accept}}, State = #state{}) ->
            {ok, State};
        update(#event{}, State = #state{}) ->
            {unhandled, State}.

        effects(#event{id = 1, body = #command{kind = accept, item = Selector}},
                State = #state{data = Timer, spare = Target}) ->
            case Selector of
                0 -> erlang:cancel_timer(Timer), service:check(Target), ignored;
                1 -> service:check(Timer), service:check(Target)
            end,
            {ok, State};
        effects(#event{id = 2, body = #command{kind = accept}}, State = #state{}) ->
            {ok, State};
        effects(#event{}, State = #state{}) ->
            {unhandled, State}.

        branch_fields(#event{id = 1, body = #command{kind = accept, item = Selector}},
                      State = #state{data = #branch_data{key = Key, value = Value, timer = Timer}, spare = Target}) ->
            case Selector of
                1 -> service:check(Target), service:pair(Key, Value);
                0 -> service:check(Timer), service:check(Target), ignored
            end,
            {ok, State};
        branch_fields(#event{id = 2, body = #command{kind = accept}}, State = #state{}) ->
            {ok, State};
        branch_fields(#event{}, State = #state{}) ->
            {unhandled, State}.

        joined(#event{id = 1, body = #command{kind = accept, item = Selector}},
               State = #state{data = Record = #branch_data{key = Key, timer = Timer}, spare = Target}) ->
            Chosen =
                case Selector of
                    1 -> service:check(Target), service:make(Key);
                    0 -> service:check(Target), service:check(Record), none
                end,
            case Timer of
                none -> ok;
                _ -> erlang:cancel_timer(Timer)
            end,
            {ok, State#state{data = Record#branch_data{timer = Chosen}}};
        joined(#event{id = 2, body = #command{kind = accept}}, State = #state{}) ->
            {ok, State};
        joined(#event{}, State = #state{}) ->
            {unhandled, State}.

        cold(#event{id = 1, body = #command{kind = accept, item = Selector}},
             State = #state{data = #branch_data{key = Key, value = Value, timer = Timer}, spare = Target}) ->
            case Selector of
                1 -> service:check(Target), service:pair(Key, Value);
                0 -> service:check(Timer), service:check(Target), ignored
            end,
            {ok, State};
        cold(#event{id = 2, body = #command{kind = accept, item = Selector}},
             State = #state{data = Record = #branch_data{key = Key, timer = Timer}, spare = Target}) ->
            Chosen =
                case Selector of
                    1 -> service:check(Target), service:make(Key);
                    0 -> service:check(Target), service:check(Record), none
                end,
            case Timer of
                none -> ok;
                _ -> erlang:cancel_timer(Timer)
            end,
            {ok, State#state{data = Record#branch_data{timer = Chosen}}};
        cold(#event{id = 3, body = #command{kind = accept}}, State = #state{data = Item, spare = Spare}) ->
            service:check(Item),
            service:check(Spare),
            {ok, State#state{data = Item#item{quantity = 0}}};
        cold(#event{}, State = #state{}) ->
            {unhandled, State}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_outer_literal_record_entry.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        if (!SelectsIntegerBeforeFrame("run"))
        {
            throw new InvalidOperationException(MissingDispatch);
        }

        if (SelectsIntegerBeforeFrame("overlap"))
        {
            throw new InvalidOperationException(IncorrectDispatch);
        }

        object[] ordered = Body("ordered");
        int selection = Array.FindIndex(ordered, instruction => Opcode(instruction) == SelectValue);
        if (selection < 0)
        {
            throw new InvalidOperationException(MissingDispatch);
        }

        object[] checks = ordered[(selection + 1)..];
        int nested = Array.FindIndex(checks, instruction => Opcode(instruction) == IsTaggedTuple && IsX(Operands(instruction)[1], 2));
        if (nested < 5 || !checks[(nested - 5)..(nested + 1)].Select(Opcode).SequenceEqual(
                new byte[] { IsTaggedTuple, GetTupleElement, ExactEqualTest, IsTaggedTuple, GetTupleElement, IsTaggedTuple })
            || !IsX(Operands(checks[nested - 5])[1], 3)
            || !IsX(Operands(checks[nested - 4])[2], 2)
            || !IsX(Operands(checks[nested - 2])[1], 1)
            || !IsX(Operands(checks[nested - 1])[2], 2))
        {
            throw new InvalidOperationException(ColumnOrder);
        }

        object[] update = Body("update");
        int inner = Array.FindIndex(update, instruction => Opcode(instruction) == UpdateRecord);
        int lastEffect = Array.FindLastIndex(update, instruction => Opcode(instruction) == ExternalCall);
        if (lastEffect < 0 || inner <= lastEffect || inner + 1 >= update.Length
            || !update[(lastEffect + 1)..inner].Any(instruction => Opcode(instruction) == IsTaggedTuple)
            || Opcode(update[inner + 1]) != UpdateRecord
            || !IsX(Operands(update[inner + 1])[^1], 0)
            || Property(Operands(update[inner + 1])[^1], "TypeIndex") is null)
        {
            throw new InvalidOperationException(NestedUpdateResult);
        }

        int firstEffect = Array.FindIndex(update, instruction => Opcode(instruction) == ExternalCall);
        object allocation = update.Single(instruction => Opcode(instruction) == Allocate);
        if (firstEffect < 0 || firstEffect >= lastEffect
            || (BigInteger)Property(Operands(allocation)[0], "Value") != 2
            || !update[(firstEffect + 1)..lastEffect].Any(instruction =>
                Opcode(instruction) == GetTupleElement && Operands(instruction) is [var source, var field, var target]
                && (string)Property(source, "Kind") == "y"
                && (BigInteger)Property(source, "Value") == 1
                && (BigInteger)Property(field, "Value") == 2
                && IsX(target, 0)))
        {
            throw new InvalidOperationException(EarlyProjection);
        }

        object[] effects = Body("effects");
        if (effects.Count(instruction => Opcode(instruction) == SelectValue) != 2
            || effects.Count(instruction => Opcode(instruction) == CaseEnd) != 1)
        {
            throw new InvalidOperationException(DiscardedEffectValue);
        }

        object[] branchFields = Body("branch_fields");
        object branchFrame = branchFields.Single(instruction => Opcode(instruction) == Allocate);
        if ((BigInteger)Property(Operands(branchFrame)[0], "Value") != 3
            || !branchFields.Any(instruction => IsRootProjection(instruction, 1, 0))
            || !branchFields.Any(instruction => IsRootProjection(instruction, 2, 1)))
        {
            throw new InvalidOperationException(BranchFieldRoots);
        }

        object[] joined = Body("joined");
        object resultFrame = joined.Single(instruction => Opcode(instruction) == Allocate);
        int resultUpdate = Array.FindIndex(joined, instruction => Opcode(instruction) == UpdateRecord);
        if ((BigInteger)Property(Operands(resultFrame)[0], "Value") != 3 || resultUpdate < 0
            || joined.Count(instruction => Opcode(instruction) == SelectValue) != 2
            || joined.Count(instruction => Opcode(instruction) == Move && IsY(Operands(instruction)[1], 0)) != 2
            || !IsY(Operands(joined[resultUpdate])[^1], 0))
        {
            throw new InvalidOperationException(SelectedResultSlot);
        }

        object[] cold = Body("cold");
        int warmReturn = Array.FindLastIndex(cold, instruction => Opcode(instruction) == Return);
        int recordError = Array.FindIndex(cold, instruction => Opcode(instruction) == BadRecord);
        var caseErrors = cold.Where(instruction => Opcode(instruction) == CaseEnd).ToArray();
        if (recordError <= warmReturn || Array.FindIndex(cold, instruction => Opcode(instruction) == CaseEnd) <= recordError
            || caseErrors is not [var firstCase, var secondCase]
            || !IsX(Operands(firstCase)[0], 1) || !IsX(Operands(secondCase)[0], 0))
        {
            throw new InvalidOperationException(SelectedFailures);
        }

        bool SelectsIntegerBeforeFrame(string name) => Body(name).TakeWhile(instruction => Opcode(instruction) is not (Allocate or AllocateHeap))
                .Any(instruction => Opcode(instruction) == SelectValue
                    && ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
                        .Any(operand => (string)Property(operand, "Kind") == "integer"));

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int first = (int)Property(function, "FirstInstructionIndex");
            int last = (int)Property(function, "EndInstructionIndex");

            return instructions[first..last];
        }
    }

    private static bool IsRootProjection(object instruction, int field, int target) =>
        Opcode(instruction) == GetTupleElement && Operands(instruction) is [var source, var index, var destination]
            && (string)Property(source, "Kind") == "y"
            && (BigInteger)Property(source, "Value") == 1
            && (BigInteger)Property(index, "Value") == field
            && IsX(destination, target);

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static bool IsY(object operand, int register) =>
        (string)Property(operand, "Kind") == "y" && (BigInteger)Property(operand, "Value") == register;

    private static bool IsX(object operand, int register) =>
        (string)Property(operand, "Kind") == "x" && (BigInteger)Property(operand, "Value") == register;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
