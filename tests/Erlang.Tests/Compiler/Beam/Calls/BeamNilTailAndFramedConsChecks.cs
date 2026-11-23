using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNilTailAndFramedConsChecks
{
    internal const string TestName = "compiler/beam/keeps-nil-tail-outside-a-checked-cons-frame";
    private const string Fixture = "beam_nil_tail_and_framed_cons.erl";
    private const string BadLayout = "The nil and cons entries did not preserve their frame ownership.";
    private const string BadValue = "A list entry changed its retained values or original rejection.";

    private const string Source = """
        -module(beam_nil_tail_and_framed_cons).
        -export([route/4, captured/4, swap/4, keep_old/4]).

        route(A, [], _, _) -> erlang:error({empty, A});
        route(A, _Candidates = [Head | Tail], Flag, Extra) ->
            Saved = prepare(Extra),
            finish(A, Head, Tail, Flag, Saved).

        captured(A, [], _, _) -> erlang:error({empty, A});
        captured(A, Candidates = [Head | Tail], Flag, Extra) ->
            Saved = prepare(Extra),
            finish(A, Head, Tail, Flag, {Saved, Candidates}).

        swap(A, [], _, _) -> erlang:error({empty, A});
        swap(A, [Head | Tail], Flag, Extra) ->
            Old = prepare(Extra),
            Result = produce(Head),
            _ = restore(Old),
            finish(A, Head, Tail, Flag, Result).

        keep_old(A, [], _, _) -> erlang:error({empty, A});
        keep_old(A, [Head | Tail], Flag, Extra) ->
            Old = prepare(Extra),
            Result = produce(Head),
            restore(Old),
            finish(A, Head, Tail, Flag, {Result, Old}).

        produce(Head) -> {made, Head}.

        restore(prepared) -> unchanged;
        restore(Other) -> erlang:error({unexpected_restore, Other}).

        prepare(value) -> prepared;
        prepare(Other) -> Other.

        finish(A, Head, Tail, Flag, Saved) -> {A, Head, Tail, Flag, Saved}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] route = ReadBody("route");
        object[] captured = ReadBody("captured");
        if (!HasColdNilExit(route) || !HasColdNilExit(captured)
            || FrameWidth(captured) != FrameWidth(route) + 1
            || !ReadBody("swap").Any(instruction => Opcode(instruction) == Swap)
            || ReadBody("keep_old").Any(instruction => Opcode(instruction) == Swap))
        {
            throw new InvalidOperationException(BadLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term app = Term.A("app");
            Term head = Term.A("head");
            Term tail = Term.A("improper_tail");
            Term flag = Term.A("flag");
            Term extra = Term.A("value");
            Term prepared = Term.A("prepared");
            Term candidates = new Cons(head, tail);
            Term expected = Term.Tuple(
                app,
                head,
                tail,
                flag,
                prepared
            );
            if (!expected.Equals(await module.Invoke(
                context,
                "route",
                app,
                candidates,
                flag,
                extra
            ))
                || !Term.Tuple(
                    app,
                    head,
                    tail,
                    flag,
                    Term.Tuple(prepared, candidates)
                ).Equals(await module.Invoke(
                    context,
                    "captured",
                    app,
                    candidates,
                    flag,
                    extra
                )))
            {
                throw new InvalidOperationException(BadValue);
            }

            Term made = Term.Tuple(Term.A("made"), head);
            await CheckRestoredResult("swap", made);
            await CheckRestoredResult("keep_old", Term.Tuple(made, prepared));
            await Reject(Nil.Value, Term.Tuple(Term.A("empty"), app));
            await Reject(Term.A("not_list"), Term.A(ErlangErrorReasons.FunctionClause));

            return Nil.Value;

            async Task CheckRestoredResult(string function, Term result)
            {
                if (!Term.Tuple(
                    app,
                    head,
                    tail,
                    flag,
                    result
                ).Equals(await module.Invoke(
                    context,
                    function,
                    app,
                    candidates,
                    flag,
                    extra
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(Term input, Term reason)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "route",
                        app,
                        input,
                        flag,
                        extra
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(BadValue);
            }
        });

        object[] ReadBody(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var arity]
                    && atoms[Value(name)] == function && Value(arity) == 4
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int FrameWidth(object[] body) =>
        Value(Items(body.Single(instruction => Opcode(instruction) == Allocate), "Operands")[0]);

    private static bool HasColdNilExit(object[] body)
    {
        int cons = Array.FindIndex(body, instruction => Opcode(instruction) == IsNonemptyList);
        int allocation = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int nil = Array.FindIndex(body, instruction => Opcode(instruction) == IsNil);

        return cons >= 0 && cons < allocation && allocation < nil
            && body.Skip(nil + 1).Any(instruction => Opcode(instruction) == TailExternalCall)
            && body.Skip(allocation).Take(nil - allocation).Any(instruction =>
                Opcode(instruction) == GetList
                && Items(instruction, "Operands") is [_, var head, var tail]
                && Equals(Property(head, "Kind"), "y") && Equals(Property(tail, "Kind"), "y"));
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
