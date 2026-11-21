using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedArityEntryChecks
{
    internal const string TestName = "compiler/beam/partitions-mixed-entry-domains-before-framing";
    private const string Fixture = "beam_mixed_arity_entry_probe.erl";
    private const string WrongLayout = "Tuple arities must be selected before allocating a clause frame.";
    private const string WrongResult = "A mixed entry changed a checked clause, fallback or effect.";
    private const string WrongRecordCheck = "Only the unchecked tuple arm can fail with badrecord.";

    private const string Source = """
        -module(beam_mixed_arity_entry_probe).
        -export([dispatch/2, facts/2, protected/2, event/0]).

        -record(context, {value}).
        -record(payload, {value}).

        dispatch(leave, #context{value=Value}) -> {leave, Value};
        dispatch(finish, #context{value=[{payload, Value} | Tail]}) -> {finish, Value, Tail};
        dispatch(finish_record, #context{value=[#payload{value=Value} = Head | Tail]}) ->
            {finish_record, Head#payload{value={checked, Value}}, Tail};
        dispatch({pair, X}, Context) -> pair(X, Context);
        dispatch(enter, Context) -> entered(Context);
        dispatch({triple, leading, X}, Context) -> {leading, X, Context};
        dispatch({triple, Key, X}, Context) -> {triple, Key, X, Context};
        dispatch({quad, X, Y, Z}, Context) -> {quad, X, Y, Z, Context};
        dispatch(Kind, Context) -> {fallback, Kind, Context}.

        facts(skip, State) -> {ignored, State};
        facts({pair, checked}, State = #context{value=[Head = #payload{} | Tail]}) ->
            {State#context{value=Tail}, Head#payload{value=checked}};
        facts({pair, unchecked}, State) ->
            {unchecked, State#context{value=unchecked}};
        facts({triple, X, Y}, State) -> {triple, X, Y, State};
        facts({quad, X, Y, Z}, State) -> {quad, X, Y, Z, State};
        facts(Event, State) -> {fallback, Event, State}.

        pair(X, Context) -> {pair, X, Context}.

        entered(Context) ->
            erlang:put(last_entry_event, Context),
            {entered, Context}.

        event() -> erlang:get(last_entry_event).

        protected(read, Context) -> Context;
        protected({pair, X}, _) ->
            try check(X) of
                Result -> {ok, Result}
            catch
                error:bad_value -> {rejected, X}
            end;
        protected({triple, X, Y}, _) -> {X, Y};
        protected({quad, X, Y, Z}, _) -> {X, Y, Z};
        protected(_, Context) -> {fallback, Context}.

        check(bad) -> erlang:error(bad_value);
        check(Value) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] all = Items(code, "Instructions").ToArray();
        int entry = Array.FindIndex(all, instruction =>
            Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == "dispatch");
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = all.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
        if (body is not [var type, var arities, ..]
            || Opcode(type) != IsTuple || Opcode(arities) != SelectTupleArity
            || !Operands(arities).Skip(3).Where((_, index) => index % 2 == 0)
                .Select(Value).SequenceEqual([2, 3, 4])
            || Value(Operands(type)[0]) == Value(Operands(arities)[1]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int facts = Array.FindIndex(all, instruction =>
            Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == "facts");
        if (facts < 0)
        {
            throw new InvalidOperationException(WrongRecordCheck);
        }

        object[] factBody = all.Skip(facts + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
        if (factBody is not [var factType, var factArities, ..]
            || Opcode(factType) != IsTuple || Opcode(factArities) != SelectTupleArity
            || factBody.Count(instruction => Opcode(instruction) == BadRecord) != 1)
        {
            throw new InvalidOperationException(WrongRecordCheck);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term bad = Term.A("bad");
            Term rawContext = Term.A("unmatched_state");
            Term protectedReject = Term.Tuple(Term.A("rejected"), bad);
            Term fallback = Term.A("fallback");
            Term leave = Term.A("leave");
            Term record = Term.Tuple(Term.A("context"), alpha);
            Term two = Term.Tuple(Term.A("pair"), alpha);
            Term leading = Term.Tuple(Term.A("triple"), Term.A("leading"), alpha);
            Term trailing = Term.Tuple(Term.A("triple"), bad, alpha);
            Term malformed = Term.Tuple(Term.A("triple"), alpha);
            if (!Term.Tuple(fallback, leave, rawContext).Equals(await module.Invoke(
                context,
                "dispatch",
                leave,
                rawContext
            ))
                || !Term.Tuple(fallback, malformed, rawContext).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    malformed,
                    rawContext
                ))
                || !Term.Tuple(fallback, Term.I(7), rawContext).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    Term.I(7),
                    rawContext
                ))
                || !Term.Tuple(leave, alpha).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    leave,
                    record
                ))
                || !Term.Tuple(Term.A("pair"), alpha, rawContext).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    two,
                    rawContext
                ))
                || !Term.Tuple(Term.A("leading"), alpha, rawContext).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    leading,
                    rawContext
                ))
                || !Term.Tuple(
                    Term.A("triple"),
                    bad,
                    alpha,
                    rawContext
                ).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    trailing,
                    rawContext
                ))
                || !Term.A("undefined").Equals(await module.Invoke(context, "event"))
                || !Term.Tuple(Term.A("entered"), alpha).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    Term.A("enter"),
                    alpha
                ))
                || !alpha.Equals(await module.Invoke(context, "event"))
                || !protectedReject.Equals(await module.Invoke(
                    context,
                    "protected",
                    Term.Tuple(Term.A("pair"), bad),
                    alpha
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term finish = Term.A("finish");
            Term finishRecord = Term.A("finish_record");
            Term payload = Term.A("payload");
            Term contextTag = Term.A("context");
            Term head = Term.Tuple(payload, alpha);
            Term improperTail = Term.A("improper_tail");
            Term proper = Term.Tuple(contextTag, Term.List(head));
            Term improper = Term.Tuple(contextTag, new Cons(head, improperTail));
            Term empty = Term.Tuple(contextTag, Nil.Value);
            Term wrongHead = Term.Tuple(contextTag, Term.List(Term.Tuple(Term.A("unmatched"), alpha)));
            Term checkedHead = Term.Tuple(payload, Term.Tuple(Term.A("checked"), alpha));
            if (!Term.Tuple(finish, alpha, Nil.Value).Equals(await module.Invoke(
                context,
                "dispatch",
                finish,
                proper
            ))
                || !Term.Tuple(finish, alpha, improperTail).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    finish,
                    improper
                ))
                || !Term.Tuple(finishRecord, checkedHead, improperTail).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    finishRecord,
                    improper
                ))
                || !Term.Tuple(fallback, finish, empty).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    finish,
                    empty
                ))
                || !Term.Tuple(fallback, finish, wrongHead).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    finish,
                    wrongHead
                ))
                || !Term.Tuple(fallback, finishRecord, rawContext).Equals(await module.Invoke(
                    context,
                    "dispatch",
                    finishRecord,
                    rawContext
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term checkedMark = Term.A("checked");
            Term uncheckedMark = Term.A("unchecked");
            Term checkedPair = Term.Tuple(Term.A("pair"), checkedMark);
            Term uncheckedPair = Term.Tuple(Term.A("pair"), uncheckedMark);
            Term checkedResult = Term.Tuple(Term.Tuple(contextTag, improperTail), Term.Tuple(payload, checkedMark));
            Term uncheckedResult = Term.Tuple(uncheckedMark, Term.Tuple(contextTag, uncheckedMark));
            if (!checkedResult.Equals(await module.Invoke(
                context,
                "facts",
                checkedPair,
                improper
            ))
                || !Term.Tuple(fallback, checkedPair, wrongHead).Equals(await module.Invoke(
                    context,
                    "facts",
                    checkedPair,
                    wrongHead
                ))
                || !uncheckedResult.Equals(await module.Invoke(
                    context,
                    "facts",
                    uncheckedPair,
                    record
                )))
            {
                throw new InvalidOperationException(WrongRecordCheck);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "facts",
                    uncheckedPair,
                    rawContext
                );
                throw new InvalidOperationException(WrongRecordCheck);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badrecord"), rawContext)))
            {
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
