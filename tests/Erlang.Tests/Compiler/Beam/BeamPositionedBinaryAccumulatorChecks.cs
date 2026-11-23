using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPositionedBinaryAccumulatorChecks
{
    internal const string TestName = "compiler/beam/threads-a-closed-binary-match-context-through-recursion";
    private const string Fixture = "beam_positioned_binary_accumulator_probe.erl";
    private const string IncorrectInstructions = "A closed binary entry or its guarded matcher lost the positioned context.";
    private const string IncorrectResults = "Reusing the binary position changed the results or a rejected input.";

    private const string Source = """
        -module(beam_positioned_binary_accumulator_probe).
        -export([entry/1, exposed/3]).

        entry(B) ->
            wrapped(B).
        wrapped(<<Bytes/binary>>) ->
            unpack(Bytes, 0, 0).
        unpack(<<1:1, H:7, T/binary>>, N, A) when N < 4 ->
            V = (A bsl 7) bor H,
            unpack(T, N + 1, V);
        unpack(<<H, T/binary>>, N, A) when N < 4 ->
            V = (A bsl 7) bor H,
            [V | unpack(T, 0, 0)];
        unpack(<<>>, _N, _A) ->
            [].

        exposed(<<1:1, H:7, T/binary>>, N, A) when N < 4 ->
            V = (A bsl 7) bor H,
            exposed(T, N + 1, V);
        exposed(<<H, T/binary>>, N, A) when N < 4 ->
            V = (A bsl 7) bor H,
            [V | exposed(T, 0, 0)];
        exposed(<<>>, _N, _A) ->
            [].
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        byte[] wrapper = ReadOperations("wrapped", 1);
        byte[] loop = ReadOperations("unpack", 3);
        byte[] exposed = ReadOperations("exposed", 3);
        if (!wrapper.Contains(StartBinaryMatch) || !wrapper.Contains(GetBinaryTail)
            || !wrapper.Contains(TailLocalCall) || wrapper.Contains(Allocate)
            || !loop.Contains(StartBinaryMatchKnown) || !loop.Contains(GetBinaryPosition)
            || loop.Count(operation => operation == SetBinaryPosition) != 2
            || !loop.Contains(TailLocalCall) || !loop.Contains(LocalCall)
            || exposed.Contains(StartBinaryMatchKnown))
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            if (!Nil.Value.Equals(await module.Invoke(context, "entry", new BitString([])))
                || !Term.List(Term.I(129), Term.I(255)).Equals(
                    await module.Invoke(context, "entry", new BitString([129, 1, 129, 127])))
                || !Term.List(Term.I(128), Term.I(2)).Equals(
                    await module.Invoke(context, "entry", new BitString([129, 0, 2]))))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            await ExpectRejected(new BitString([128], 1));
            await ExpectRejected(Term.A("not_a_binary"));
            await ExpectRejected(new BitString([128, 128, 128, 128, 0]));

            return Nil.Value;

            async Task ExpectRejected(Term input)
            {
                try
                {
                    _ = await module.Invoke(context, "entry", input);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResults);
            }
        });

        byte[] ReadOperations(string name, int arity)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var count]
                    && atoms[(int)(BigInteger)Property(atom, "Value")] == name
                    && (int)(BigInteger)Property(count, "Value") == arity
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            return instructions.Skip(start + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
