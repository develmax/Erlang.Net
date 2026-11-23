using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaughtPositionalListLoopChecks
{
    internal const string TestName = "compiler/beam/keeps-caught-list-head-in-its-outgoing-register";
    private const string Fixture = "beam_caught_positional_list_loop.erl";
    private const string BadLayout = "The checked list entry retained an unnecessary frame slot or reloaded the call arguments.";
    private const string BadValue = "The caught list loop changed the tagged result, continuation, or malformed-list behavior.";

    private const string Source = """
        -module(beam_caught_positional_list_loop).
        -export([phase/3, exchanged/3]).

        phase(_, _, []) -> finished;
        phase(Phase, Type, [Item | Rest]) ->
            case catch step(Phase, Type, Item) of
                {error, _} = Error -> Error;
                _ -> phase(Phase, Type, Rest)
            end.

        exchanged(_, _, []) -> finished;
        exchanged(Phase, Type, [Item | Rest]) ->
            case catch step(Phase, Type, Item) of
                {error, _} = Error -> Error;
                _ -> exchanged(Type, Phase, Rest)
            end.

        step(Phase, Type, stop) -> {error, {Phase, Type}};
        step(Phase, Type, throws) -> throw({error, {Phase, Type}});
        step(_, _, fault) -> erlang:error(ignored);
        step(_, _, _) -> next.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasDirectProtectedArguments(ReadBody("phase")) || HasDirectProtectedArguments(ReadBody("exchanged")))
        {
            throw new InvalidOperationException(BadLayout);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term phase = Term.A("phase");
            Term type = Term.A("type");
            Term stop = Term.A("stop");
            Term fail = Term.A("fault");
            Term pass = Term.A("pass");
            Term improper = Term.A("improper");
            Term expected = Term.Tuple(Term.A("error"), Term.Tuple(phase, type));
            await Check("phase", Nil.Value, Term.A("finished"));
            await Check("phase", Term.List(fail, pass), Term.A("finished"));
            await Check("phase", Term.List(fail, stop), expected);
            await Check("phase", Term.List(Term.A("throws"), fail), expected);
            await Check("phase", new Cons(stop, improper), expected);
            await Check("exchanged", Term.List(pass, stop), Term.Tuple(Term.A("error"), Term.Tuple(type, phase)));
            await Reject(improper);
            await Reject(new Cons(pass, improper));

            return Nil.Value;

            async Task Check(string function, Term input, Term result)
            {
                if (!result.Equals(await module.Invoke(
                    context,
                    function,
                    phase,
                    type,
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(Term input)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "phase",
                        phase,
                        type,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
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
                    && atoms[Value(name)] == function && Value(arity) == 3
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasDirectProtectedArguments(object[] body)
    {
        int cons = Array.FindIndex(body, instruction => Opcode(instruction) == IsNonemptyList);
        int allocate = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int extract = Array.FindIndex(body, instruction => Opcode(instruction) == GetList);
        int enter = Array.FindIndex(body, instruction => Opcode(instruction) == CatchEnter);
        int call = Array.FindIndex(body, instruction => Opcode(instruction) == LocalCall);

        return cons >= 0 && cons < allocate && allocate < extract && extract < enter && enter < call
            && Items(body[allocate], "Operands") is [var frame, var live]
            && Value(frame) == 4 && Value(live) == 3
            && Items(body[extract], "Operands") is [_, var head, var tail]
            && Equals(Property(head, "Kind"), "x") && Value(head) == 2
            && Equals(Property(tail, "Kind"), "y") && Value(tail) == 0
            && body.Skip(enter + 1).Take(call - enter - 1).All(instruction => Opcode(instruction) == Line);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
