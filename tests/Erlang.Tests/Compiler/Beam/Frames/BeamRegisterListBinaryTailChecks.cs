using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRegisterListBinaryTailChecks
{
    internal const string TestName = "compiler/beam/keeps-fixed-list-binary-tail-arguments-in-registers";
    private const string WrongSchedule = "The fixed list branch lost its live binary roots or frameless tail.";
    private const string WrongResult = "The fixed list branch changed its concatenation, forwarding, or failure.";
    private const string Source = """
        -module(beam_register_list_binary_tail).
        -export([assemble/1, assemble_more/1]).

        assemble([New, Previous, Head, Tail]) ->
            finish(<<Previous/binary, New/binary>>, Head, Tail).

        assemble_more([New, Previous, First, Second, Third]) ->
            finish_more(<<Previous/binary, New/binary>>, First, Second, Third).

        finish(Binary, Head, Tail) -> {Binary, Head, Tail}.
        finish_more(Binary, First, Second, Third) -> {Binary, First, Second, Third}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_register_list_binary_tail.erl");
        CheckLayout(image);
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term prefix = new BitString([1, 2]);
            Term suffix = new BitString([3, 4]);
            Term expected = new BitString([1, 2, 3, 4]);
            Term one = Term.A("one");
            Term two = Term.A("two");
            Term three = Term.A("three");
            if (!Term.Tuple(expected, one, two).Equals(await module.Invoke(
                    context, "assemble", Term.List(
                        suffix,
                        prefix,
                        one,
                        two
                    )))
                || !Term.Tuple(
                    expected,
                    one,
                    two,
                    three
                ).Equals(await module.Invoke(
                    context, "assemble_more", Term.List(
                        suffix,
                        prefix,
                        one,
                        two,
                        three
                    ))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await RequireFailure(Term.List(suffix, prefix, one), ErlangErrorReasons.FunctionClause);
            await RequireFailure(new Cons(suffix, prefix), ErlangErrorReasons.FunctionClause);
            await RequireFailure(Term.List(
                suffix,
                prefix,
                one,
                two,
                three
            ), ErlangErrorReasons.FunctionClause);
            await RequireFailure(Term.List(
                one,
                prefix,
                one,
                two
            ), ErlangErrorReasons.BadArgument);

            return Nil.Value;

            async Task RequireFailure(Term source, string reason)
            {
                try
                {
                    _ = await module.Invoke(context, "assemble", source);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(reason))
                    && (reason != ErlangErrorReasons.FunctionClause
                        || error.StackTraceTerm is Cons
                        {
                            Head: TupleTerm { Items: [_, Atom { Name: "assemble" }, var arguments, _] }
                        } && Cons.Items(arguments).SequenceEqual(new[] { source })))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("assemble", 6, [2, 1]);
        Check("assemble_more", 7, [2, 3, 1]);

        void Check(string function, int live, int[] targets)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Operand(instruction, 1)] == function);
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            object[] body = instructions.Skip(entry + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) is not (Label or Line))
                .ToArray();
            int construction = Array.FindIndex(body, instruction => Opcode(instruction) == CreateBinary);
            if (construction < 0 || Operand(body[construction], 2) != live
                || body.Take(construction).Count(instruction => Opcode(instruction) == GetList) != live - 2
                || Opcode(body[construction - 1]) != IsNil
                || body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap or Deallocate)
                || Opcode(body[^1]) != TailLocalCall
                || !body.Skip(construction + 1).Take(targets.Length)
                    .Select(instruction => Opcode(instruction) == Move ? Operand(instruction, 1) : -1).SequenceEqual(targets))
            {
                throw new InvalidOperationException(WrongSchedule);
            }
        }
    }

    private static int Operand(object instruction, int index) =>
        (int)(BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
