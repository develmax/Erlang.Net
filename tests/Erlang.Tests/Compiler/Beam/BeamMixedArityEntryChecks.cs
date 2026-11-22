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
        -export([dispatch/2, facts/2, protected/2, event/0, convert/1, closed/2]).

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

        closed(keep, State) -> State;
        closed(stop, _) -> erlang:error(stopped);
        closed({result, Value}, _) -> Value;
        closed({run, Value, Context}, _) ->
            Callback = fun(Input) -> {Context, Input} end,
            invoke_callback(Callback, Value).

        invoke_callback(Callback, Value) -> Callback(Value).

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

        convert([]) -> [];
        convert([Head | Tail]) ->
            case Head of
                {dot, Location} -> [{dot, Location} | convert(Tail)];
                {arrow, Location} -> [{arrow, Location} | convert(Tail)];
                {Kind, Location, Symbol} -> [{Kind, Location, Symbol} | convert(Tail)];
                {Other, Location} ->
                    Mode = case classify(Other) of
                               true -> known;
                               false -> unknown
                           end,
                    [{Mode, Location, Other} | convert(Tail)]
            end.

        classify(word) -> true;
        classify(_) -> false.

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

        int closed = Array.FindIndex(all, instruction =>
            Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == "closed");
        if (closed < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] closedPrefix = all.Skip(closed + 1).Where(instruction => Opcode(instruction) is not (Label or Line))
            .Take(2).ToArray();
        if (closedPrefix is not [var closedType, var closedArities]
            || Opcode(closedType) != IsTuple || Opcode(closedArities) != SelectTupleArity)
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

        int convert = Array.FindIndex(all, instruction =>
            Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == "convert");
        if (convert < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] convertBody = all.Skip(convert + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .ToArray();
        object[] convertPrefix = convertBody.Where(instruction => Opcode(instruction) is not (Label or Line))
            .Take(4).ToArray();
        if (!convertPrefix.Select(Opcode).SequenceEqual(new byte[]
            {
                IsNonemptyList, GetList, IsTuple, SelectTupleArity
            }))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int triple = Value(Operands(convertPrefix[^1])[6]);
        int tripleLabel = Array.FindIndex(convertBody, instruction =>
            Opcode(instruction) == Label && Value(Operands(instruction)[0]) == triple);
        if (tripleLabel < 0 || Opcode(convertBody[tripleLabel + 1]) != Allocate
            || !Operands(convertBody[tripleLabel + 1]).Select(Value).SequenceEqual([1, 2]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int pair = Value(Operands(convertPrefix[^1])[4]);
        int pairLabel = Array.FindIndex(convertBody, instruction =>
            Opcode(instruction) == Label && Value(Operands(instruction)[0]) == pair);
        if (pairLabel < 0 || Opcode(convertBody[pairLabel + 1]) != GetTupleElement
            || Opcode(convertBody[pairLabel + 2]) != SelectValue)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] tagTable = Operands(convertBody[pairLabel + 2]);
        if (!tagTable.Skip(3).Where((_, index) => index % 2 == 0).Select(atom => atoms[Value(atom)])
            .SequenceEqual(["arrow", "dot"]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        foreach (object target in tagTable.Skip(4).Where((_, index) => index % 2 == 0))
        {
            int arm = Array.FindIndex(convertBody, instruction =>
                Opcode(instruction) == Label && Value(Operands(instruction)[0]) == Value(target));
            if (arm < 0 || Opcode(convertBody[arm + 1]) != Allocate
                || !Operands(convertBody[arm + 1]).Select(Value).SequenceEqual([1, 2]))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        object[] conversion = convertBody.Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
        int construction = Array.FindIndex(conversion, instruction => Opcode(instruction) == TestHeap
            && Operands(instruction).Select(Value).SequenceEqual([6, 1]));
        if (construction < 2 || construction + 4 >= conversion.Length
            || Opcode(conversion[construction - 2]) != TrimFrame
            || !Operands(conversion[construction - 2]).Select(Value).SequenceEqual([1, 3])
            || Opcode(conversion[construction - 1]) != LocalCall
            || !conversion.Skip(construction + 1).Take(4).Select(Opcode).SequenceEqual(new byte[]
            {
                GetTupleElement, PutTuple, PutList, Deallocate
            })
            || !Operands(conversion[construction + 4]).Select(Value).SequenceEqual([3]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term bad = Term.A("bad");
            Term rawContext = Term.A("unmatched_state");
            if (!alpha.Equals(await module.Invoke(
                context,
                "closed",
                Term.A("keep"),
                alpha
            ))
                || !bad.Equals(await module.Invoke(
                    context,
                    "closed",
                    Term.Tuple(Term.A("result"), bad),
                    alpha
                ))
                || !Term.Tuple(alpha, bad).Equals(await module.Invoke(
                    context,
                    "closed",
                    Term.Tuple(Term.A("run"), bad, alpha),
                    alpha
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "closed",
                    Term.A("unmatched"),
                    alpha
                );
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("function_clause")))
            {
            }

            Term protectedReject = Term.Tuple(Term.A("rejected"), bad);
            Term dot = Term.Tuple(Term.A("dot"), alpha);
            Term arrow = Term.Tuple(Term.A("arrow"), alpha);
            Term raw = Term.Tuple(Term.A("word"), alpha);
            Term otherTag = Term.A("other");
            Term other = Term.Tuple(otherTag, alpha);
            Term tripleToken = Term.Tuple(Term.A("symbol"), alpha, bad);
            if (!Term.List(
                dot,
                arrow,
                tripleToken,
                Term.Tuple(Term.A("known"), alpha, Term.A("word")),
                Term.Tuple(Term.A("unknown"), alpha, otherTag)
            ).Equals(await module.Invoke(context, "convert", Term.List(
                dot,
                arrow,
                tripleToken,
                raw,
                other
            )))
                || !Nil.Value.Equals(await module.Invoke(context, "convert", Nil.Value)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(context, "convert", Term.List(bad));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), bad)))
            {
                // The case must raise with the unmatched original head.
            }

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
