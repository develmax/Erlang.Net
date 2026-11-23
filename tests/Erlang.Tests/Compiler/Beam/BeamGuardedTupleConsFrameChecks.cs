using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTupleConsFrameChecks
{
    internal const string TestName = "compiler/beam/splits-guarded-entry-cons-frames-and-defers-checked-projection";
    private const string WrongLayout = "Expected the guarded call sequence and the fallback to own separate frames.";
    private const string WrongValue = "Unexpected guarded entry result or failure.";
    private const string Source = """
        -module(guarded_tuple_cons_frames).
        -export([convert/1, live_tail/1]).

        convert({Label, [Head|Tail]}) when is_tuple(Head) ->
            {list_to_atom(Label), decorate(lists:reverse([Head|Tail]))};
        convert({Label, Input}) ->
            case decode(Input) of
                {ok, Value} -> {list_to_atom(Label), Value};
                Error -> throw(Error)
            end.

        live_tail({Label, [Head|Tail]}) when is_tuple(Head) ->
            {list_to_atom(Label), decorate(lists:reverse([Head|Tail])), Tail};
        live_tail({Label, Input}) ->
            {Label, Input}.

        decode(decoded) -> {ok, 17};
        decode(Input) -> {rejected, Input}.
        decorate(Value) -> {decorated, Value}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "guarded_tuple_cons_frames.erl");
        object[] instructions = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();
        if (entries.Length < 3)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions[entries[0]..entries[1]];
        int[] frames = Enumerable.Range(0, body.Length).Where(index => Opcode(body[index]) == Allocate).ToArray();
        int head = Array.FindIndex(body, instruction => Opcode(instruction) == GetHead);
        int swap = Array.FindIndex(body, instruction => Opcode(instruction) == Swap);
        int tagged = Array.FindIndex(body, instruction => Opcode(instruction) == IsTaggedTuple);
        if (frames.Length != 2 || head < 0 || head >= frames[0] || swap <= frames[0] || swap >= frames[1]
            || tagged <= frames[1] || Operand(body[frames[0]], 0) != 1 || Operand(body[frames[0]], 1) != 2
            || Operand(body[frames[1]], 0) != 2 || Operand(body[frames[1]], 1) != 2
            || body.Any(instruction => Opcode(instruction) is PutList or TrimFrame)
            || !instructions[entries[1]..entries[2]].Any(instruction => Opcode(instruction) == PutList))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int effect = Array.FindIndex(body, tagged + 1, instruction => Opcode(instruction) == ExternalCall);
        int projection = Array.FindIndex(body, tagged + 1, instruction => Opcode(instruction) == GetTupleElement);
        if (effect <= tagged || projection <= effect || Opcode(body[projection - 1]) != TestHeap)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var image = BeamModule.Prepare(bytes, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term label = Term.List(Term.I(107), Term.I(101), Term.I(121));
            Term key = Term.A("key");
            Term headValue = Term.Tuple(Term.A("head"), Term.I(11));
            Term item = Term.A("rest");
            Term hotInput = Term.Tuple(label, Term.List(headValue, item));
            Term coldInput = Term.Tuple(label, Term.A("decoded"));
            Term decorated = Term.Tuple(Term.A("decorated"), Term.List(item, headValue));
            if (!Term.Tuple(key, decorated).Equals(await image.Invoke(context, "convert", hotInput))
                || !Term.Tuple(key, Term.I(17)).Equals(await image.Invoke(context, "convert", coldInput))
                || !Term.Tuple(key, decorated, Term.List(item)).Equals(await image.Invoke(context, "live_tail", hotInput)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term rejected = Term.List(item, headValue);
            await ExpectFailure(
                Term.Tuple(label, rejected),
                ErlangExceptionClasses.Throw,
                Term.Tuple(Term.A("rejected"), rejected)
            );
            Term improper = new Cons(headValue, new Cons(item, item));
            await ExpectFailure(Term.Tuple(label, improper), ErlangExceptionClasses.Error, Term.A(ErlangErrorReasons.BadArgument));
            await ExpectFailure(item, ErlangExceptionClasses.Error, Term.A(ErlangErrorReasons.FunctionClause));
            await ExpectFailure(Term.Tuple(item), ErlangExceptionClasses.Error, Term.A(ErlangErrorReasons.FunctionClause));

            return Nil.Value;

            async Task ExpectFailure(Term input, string exceptionClass, Term reason)
            {
                try
                {
                    await image.Invoke(context, "convert", input);
                    throw new InvalidOperationException(WrongValue);
                }
                catch (ErlangException error) when (error.ExceptionClass == exceptionClass && error.Reason.Equals(reason))
                {
                }
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
