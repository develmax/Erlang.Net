using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedAtomClosureTuplesChecks
{
    internal const string TestName = "compiler/beam/dispatches-closed-atom-closure-tuples-without-a-frame";
    private const string WrongLayout = "Expected one selector and two combined tuple and closure allocations.";
    private const string WrongResult = "The atom branch changed its callback, value, or failure arguments.";
    private const string Source = """
        -module(beam_closed_atom_closure_tuples).
        -export([select/2, invoke/2]).

        select(missing, _) -> {[], default_value};
        select(on, _) -> {[selected], fun(Value, _) -> Value end};
        select(off, _) -> {[unselected], fun(_, Value) -> Value end}.

        invoke({Tag, Callback}, Argument) -> {Tag, Callback(Argument, argument_two)}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_closed_atom_closure_tuples.erl");
        CheckLayout(image);
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term state = Term.A("state");
            Term argument = Term.A("argument");
            Term on = await module.Invoke(
                context,
                "select",
                Term.A("on"),
                state
            );
            Term off = await module.Invoke(
                context,
                "select",
                Term.A("off"),
                state
            );
            if (!Term.Tuple(Term.List(Term.A("selected")), argument).Equals(await module.Invoke(
                context,
                "invoke",
                on,
                argument
            ))
                || !Term.Tuple(Term.List(Term.A("unselected")), Term.A("argument_two")).Equals(await module.Invoke(
                    context,
                    "invoke",
                    off,
                    argument
                ))
                || !Term.Tuple(Nil.Value, Term.A("default_value")).Equals(await module.Invoke(
                    context,
                    "select",
                    Term.A("missing"),
                    state
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term unknown = Term.A("unknown");
            try
            {
                _ = await module.Invoke(
                    context,
                    "select",
                    unknown,
                    state
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause))
                && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom { Name: "select" }, var arguments, _] } }
                && Cons.Items(arguments).SequenceEqual(new[] { unknown, state }))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == "select"
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).Where(opcode => opcode is not (Label or Line)).ToArray();
        if (!body.SequenceEqual([
            SelectValue, TestHeap, MakeFun, PutTuple, Return, TestHeap, MakeFun, PutTuple, Return, Move, Return
        ]))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
