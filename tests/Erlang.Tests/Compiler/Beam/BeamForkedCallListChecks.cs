using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamForkedCallListChecks
{
    internal const string TestName = "compiler/beam/reuses-a-forked-call-slot-and-direct-filter-bindings";
    private const string Module = "beam_forked_call_list";
    private const string ShapeError = "The forked sequence or identity filters retained redundant stack slots.";
    private const string ValueError = "The filtered list changed its order or result.";
    private const string FailureError = "The identity filter lost the malformed-filter error.";

    private const string Source = """
        -module(beam_forked_call_list).
        -export([run/1, incomplete/1]).

        run(Input) ->
            Source = obtain(Input),
            Left = [X || X <- Source, keep_left(X)],
            Right = [Y || Y <- Source, keep_right(Y)],
            [right(Right), left(Left)].

        incomplete(Items) -> [X || X <- Items, ambiguous(X)].

        obtain(Value) -> Value.
        keep_left(l) -> true;
        keep_left(_) -> false.
        keep_right(r) -> true;
        keep_right(_) -> false.
        ambiguous(l) -> true;
        ambiguous(_) -> malformed.
        left(Value) -> {left, Value}.
        right(Value) -> {right, Value}.
        """;

    internal static async Task Run()
    {
        byte[] compiled = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(compiled);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] run = Body("run");
        if (run.Count(instruction => Opcode(instruction) == Swap) != 2
            || Body("-run/1-lc$^0/1-0-").Count(LiveFilterFrame) != 1
            || Body("-run/1-lc$^1/1-1-").Count(LiveFilterFrame) != 1
            || !run.Any(instruction => Opcode(instruction) == Allocate && Value(Children(instruction, "Operands")[0]) == 1))
        {
            throw new InvalidOperationException(ShapeError);
        }

        var module = BeamModule.Prepare(compiled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term l = Term.A("l");
            Term r = Term.A("r");
            Term n = Term.A("n");
            Term input = Term.List(l, r, n);
            Term expected = Term.List(Term.Tuple(Term.A("right"), Term.List(r)), Term.Tuple(Term.A("left"), Term.List(l)));
            if (!expected.Equals(await module.Invoke(context, "run", input)))
            {
                throw new InvalidOperationException(ValueError);
            }

            try
            {
                await module.Invoke(context, "incomplete", input);
                throw new InvalidOperationException(FailureError);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("bad_filter"), Term.A("malformed"))))
            {
                return Nil.Value;
            }
        });

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Children(instruction, "Operands")[1])] == name);

            return start < 0 ? [] : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool LiveFilterFrame(object instruction) => Opcode(instruction) == Allocate
        && Children(instruction, "Operands").Select(Value).SequenceEqual([2, 1]);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
