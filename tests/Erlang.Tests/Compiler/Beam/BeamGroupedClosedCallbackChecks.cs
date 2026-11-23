using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGroupedClosedCallbackChecks
{
    internal const string TestName = "compiler/beam/groups-disjoint-entry-with-one-closed-literal-callback";
    private const string ModuleName = "beam_grouped_closed_callback";
    private const string WrongShape = "Mixed entry did not preserve the closed callback's identity and ownership.";
    private const string WrongValue = "Mixed entry changed callback results or original fallback arguments.";
    private const string Source = """
        -module(beam_grouped_closed_callback).
        -export([closed/2, captured/2]).

        closed(start, State) -> {start,State};
        closed({short,A}, State) -> {short,A,State};
        closed({medium,A,B}, State) -> {medium,A,B,State};
        closed({long,_,_,_}, Pair) ->
            invoke(fun({[],_}) -> empty; ({_,_}) -> present end, Pair);
        closed(Event, State) -> {fallback,Event,State}.

        captured(start, State) -> {start,State};
        captured({short,A}, State) -> {short,A,State};
        captured({medium,A,B}, State) -> {medium,A,B,State};
        captured({long,A,_,_}, Pair) ->
            invoke(fun({[],_}) -> empty; ({_,_}) -> A end, Pair);
        captured(Event, State) -> {fallback,Event,State}.

        invoke(Fun, Arg) -> Fun(Arg).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, ModuleName + ".erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] closed = ReadInstructions(code, instructions, "closed");
        object[] captured = ReadInstructions(code, instructions, "captured");
        if (!closed.Any(instruction => Opcode(instruction) == SelectTupleArity)
            || closed.Count(instruction => Opcode(instruction) == MakeFun) != 1
            || captured.Any(instruction => Opcode(instruction) == SelectTupleArity)
            || Items(code, "Functions").Count(item => (string)Property(item, "Name") == "-closed/2-fun-0-") != 1)
        {
            throw new InvalidOperationException(WrongShape);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term other = Term.A("other");
            Term payload = Term.A("captured_value");
            Term evt = Term.Tuple(
                Term.A("long"),
                payload,
                other,
                other
            );
            Term empty = Term.Tuple(Nil.Value, other);
            Term present = Term.Tuple(other, other);
            if (!Term.A("empty").Equals(await module.Invoke(
                context,
                "closed",
                evt,
                empty
            ))
                || !Term.A("present").Equals(await module.Invoke(
                    context,
                    "closed",
                    evt,
                    present
                ))
                || !payload.Equals(await module.Invoke(
                    context,
                    "captured",
                    evt,
                    present
                ))
                || !Term.Tuple(Term.A("fallback"), other, present).Equals(await module.Invoke(
                    context,
                    "closed",
                    other,
                    present
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "closed",
                    evt,
                    other
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static object[] ReadInstructions(object code, object[] instructions, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return instructions.Skip(start).Take(end - start).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
