using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedResultRecordContinuationChecks
{
    internal const string TestName = "compiler/beam/checked-adjacent-call-result-preserves-record-failure";
    private const string Source = """
        -module(beam_checked_record_call_liveness).
        -export([from_result/3, across_effect/3, grouped/2]).
        -record(entry, {m, f, args}).

        from_result(Key, Reason, State) ->
            case lookup({key, Key}) of
                missing ->
                    {keep, State};
                Entry ->
                    Module = Entry#entry.m,
                    Function = Entry#entry.f,
                    Arguments = Entry#entry.args,
                    dispatch(Module, Function, [Reason | Arguments]),
                    {keep, State}
            end.

        across_effect(Key, Reason, State) ->
            case lookup({key, Key}) of
                missing ->
                    {keep, State};
                Entry ->
                    notify(Key),
                    Module = Entry#entry.m,
                    Function = Entry#entry.f,
                    Arguments = Entry#entry.args,
                    dispatch(Module, Function, [Reason | Arguments]),
                    {keep, State}
            end.

        grouped({entry_message, Key, Reason}, State) ->
            case lookup({key, Key}) of
                missing ->
                    {keep, State};
                Entry ->
                    Module = Entry#entry.m,
                    Function = Entry#entry.f,
                    Arguments = Entry#entry.args,
                    dispatch(Module, Function, [Reason | Arguments]),
                    {keep, State}
            end;
        grouped({notice, Key, Reason}, State) ->
            note({notice, Key, Reason}),
            {keep, State};
        grouped({short, Key}, State) ->
            note({short, Key}),
            {keep, State};
        grouped(Message, State) ->
            note({fallback, Message}),
            {keep, State}.

        lookup(Key) ->
            note({lookup, Key}),
            get({control, Key}).

        dispatch(Module, Function, Arguments) ->
            note({dispatch, Module, Function, Arguments}).

        notify(Key) ->
            note({notify, Key}).

        note(Event) ->
            put(trace, [Event | get(trace)]),
            ok.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, "beam_checked_record_call_liveness.erl");
        VerifyInstructions(beam);

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("fixture");
            Term state = Term.A("state");
            Term message = Term.A("message");
            Term trace = Term.A("trace");
            Term lookupKey = Term.Tuple(Term.A("key"), key);
            Term control = Term.Tuple(Term.A("control"), lookupKey);
            Term lookedUp = Term.Tuple(Term.A("lookup"), lookupKey);
            Term notification = Term.Tuple(Term.A("notify"), key);
            Term module = Term.A("callee");
            Term function = Term.A("callback");
            Term previous = Term.A("previous");
            Term record = Term.Tuple(
                Term.A("entry"),
                module,
                function,
                Term.List(previous)
            );
            Term dispatched = Term.Tuple(
                Term.A("dispatch"),
                module,
                function,
                Term.List(message, previous)
            );
            Term invalid = Term.Tuple(Term.A("foreign_record"), Term.A("payload"));
            Term missing = Term.A("missing");
            Term kept = Term.Tuple(Term.A("keep"), state);
            Term rejected = Term.Tuple(Term.A("badrecord"), invalid);

            await Verify(
                "from_result",
                record,
                kept,
                Term.List(dispatched, lookedUp)
            );
            await Verify(
                "from_result",
                missing,
                kept,
                Term.List(lookedUp)
            );
            await Verify(
                "from_result",
                invalid,
                rejected,
                Term.List(lookedUp)
            );
            await Verify(
                "across_effect",
                record,
                kept,
                Term.List(dispatched, notification, lookedUp)
            );
            await Verify(
                "across_effect",
                invalid,
                rejected,
                Term.List(notification, lookedUp)
            );

            return Nil.Value;

            async Task Verify(
                string entry,
                Term input,
                Term expected,
                Term expectedTrace
            )
            {
                await Put(control, input);
                await Put(trace, Nil.Value);

                Term result;
                try
                {
                    result = await image.Invoke(
                        context,
                        entry,
                        key,
                        message,
                        state
                    );
                }
                catch (ErlangException error) when (input.Equals(invalid) && error.Reason.Equals(rejected))
                {
                    result = error.Reason;
                }

                Term events = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.Get,
                    trace
                );
                if (!expected.Equals(result) || !expectedTrace.Equals(events))
                {
                    throw new InvalidOperationException(BeamCheckedResultRecordContinuationDiagnostics.ResultOrEffectOrder);
                }
            }

            async Task Put(Term name, Term value)
            {
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.Put,
                    name,
                    value
                );
            }
        });
    }

    private static void VerifyInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] body = FunctionBody(code, "from_result");
        int allocate = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        int producer = Array.FindIndex(body, instruction => Opcode(instruction) == LocalCall);
        int check = Array.FindIndex(body, instruction => Opcode(instruction) == IsTaggedTuple);
        int trim = Array.FindIndex(body, instruction => Opcode(instruction) == TrimFrame);
        int failure = Array.FindIndex(body, instruction => Opcode(instruction) == BadRecord);
        if (allocate < 0 || producer <= allocate || check <= producer || trim <= check + 1 || failure <= trim
            || Value(Operands(body[allocate])[0]) != 2 || !IsXZero(Operands(body[check])[1])
            || Opcode(body[check + 1]) != TestHeap || Value(Operands(body[check + 1])[0]) != 2
            || Value(Operands(body[check + 1])[1]) != 1 || !IsXZero(Operands(body[failure])[0]))
        {
            throw new InvalidOperationException(BeamCheckedResultRecordContinuationDiagnostics.ResultFrame);
        }

        object[][] projections = body.Skip(check + 2).Take(trim - check - 2)
            .Where(instruction => Opcode(instruction) == GetTupleElement).Select(Operands).ToArray();
        if (projections.Length != 3
            || projections.Any(operands => !IsXZero(operands[0]) || Property(operands[2], "Kind") is not "x")
            || !projections.Select(operands => Value(operands[1])).SequenceEqual([3, 1, 2])
            || !Operands(body[trim]).Select(Value).SequenceEqual([1, 1]))
        {
            throw new InvalidOperationException(BeamCheckedResultRecordContinuationDiagnostics.ProjectionLiveness);
        }

        object[] grouped = FunctionBody(code, "grouped");
        int groupedFailure = Array.FindIndex(grouped, instruction => Opcode(instruction) == BadRecord);
        int lastCall = Array.FindLastIndex(grouped, instruction => Opcode(instruction) == LocalCall);
        if (lastCall < 0 || groupedFailure <= lastCall || !IsXZero(Operands(grouped[groupedFailure])[0])
            || Opcode(grouped[groupedFailure - 1]) != Line)
        {
            throw new InvalidOperationException(BeamCheckedResultRecordContinuationDiagnostics.ColdRecordOrder);
        }
    }

    private static object[] FunctionBody(object code, string function)
    {
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == function
        );
        if (start < 0)
        {
            throw new InvalidOperationException(BeamCheckedResultRecordContinuationDiagnostics.MissingEntry);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static bool IsXZero(object operand) => Property(operand, "Kind") is "x" && Value(operand) == 0;

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
