using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeferredRecordCaseSelectorChecks
{
    internal const string TestName = "compiler/beam/defers-a-checked-record-case-selector-across-a-call";
    private const string ModuleName = "beam_deferred_record_case_selector";
    private const string WrongShape = "The checked record field did not stay in its carrier until the case.";
    private const string WrongValue = "Deferring the checked selector changed a result or effect order.";
    private const string Source = """
        -module(beam_deferred_record_case_selector).
        -export([build/2, eager/2, observed/0]).
        -record(state, {counter, result}).

        build({open, Value, _, _, _}, #state{counter=Counter}=State) ->
            A = effect(Value),
            {N, Next} = case Counter of
                [] -> {1,[1]};
                [H|T] -> {H,[H+1|T]}
            end,
            State#state{result={N,Next,A}};
        build({two, A}, State) -> {two,A,State};
        build({three, A, B}, State) -> {three,A,B,State};
        build(Event, State) -> {fallback,Event,State}.

        eager({open, _, _, _, _}, #state{counter=Counter}=State) ->
            A = effect(Counter),
            {N, Next} = case Counter of
                [] -> {1,[1]};
                [H|T] -> {H,[H+1|T]}
            end,
            State#state{result={N,Next,A}};
        eager({two, A}, State) -> {two,A,State};
        eager({three, A, B}, State) -> {three,A,B,State};
        eager(Event, State) -> {fallback,Event,State}.

        effect(Value) -> erlang:put(observed, Value), Value.
        observed() -> erlang:get(observed).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, ModuleName + ".erl");
        object code = Code(image);
        RequireCaseProjection(code, "build", true);
        RequireCaseProjection(code, "eager", false);
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("observed_value");
            Term malformed = Term.A("malformed");
            Term stateTag = Term.A("state");
            Term eventValue = Term.Tuple(
                Term.A("open"),
                value,
                Nil.Value,
                Nil.Value,
                Nil.Value
            );
            Term count = Term.I(7);
            Term counter = new Cons(count, malformed);
            Term state = Term.Tuple(stateTag, counter, Nil.Value);
            Term result = Term.Tuple(stateTag, counter, Term.Tuple(count, new Cons(Term.I(8), malformed), value));
            if (!result.Equals(await module.Invoke(
                context,
                "build",
                eventValue,
                state
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term empty = Term.Tuple(stateTag, Nil.Value, Nil.Value);
            Term one = Term.I(1);
            if (!Term.Tuple(stateTag, Nil.Value, Term.Tuple(one, Term.List(one), Nil.Value)).Equals(await module.Invoke(
                context,
                "eager",
                eventValue,
                empty
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "build",
                    eventValue,
                    Term.Tuple(stateTag, malformed, Nil.Value)
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), malformed)))
            {
                if (!value.Equals(await module.Invoke(context, "observed")))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            Term wrongRecord = Term.Tuple(malformed, Nil.Value, Nil.Value);
            if (!Term.Tuple(Term.A("fallback"), eventValue, wrongRecord).Equals(await module.Invoke(
                context,
                "build",
                eventValue,
                wrongRecord
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }

    private static void RequireCaseProjection(object code, string name, bool deferred)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
        int call = Array.FindIndex(body, instruction => Opcode(instruction) == LocalCall);
        int split = Array.FindIndex(body, instruction => Opcode(instruction) == GetList);
        if (call < 0 || split <= call || body.Any(instruction => Opcode(instruction) == BadMatch))
        {
            throw new InvalidOperationException(WrongShape);
        }

        object input = Items(body[split], "Operands").First();
        bool directField = body.Skip(call + 1).Take(split - call - 1).Any(instruction =>
        {
            if (Opcode(instruction) != GetTupleElement)
            {
                return false;
            }

            object[] fields = Items(instruction, "Operands").ToArray();

            return (string)Property(fields[0], "Kind") == "y"
                && (BigInteger)Property(fields[1], "Value") == 1
                && (string)Property(fields[2], "Kind") == "x";
        });
        if (deferred != directField || (string)Property(input, "Kind") != (deferred ? "x" : "y"))
        {
            throw new InvalidOperationException(WrongShape);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
