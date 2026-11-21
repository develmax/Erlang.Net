using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamHeadTupleReturnFrameChecks
{
    internal const string TestName = "compiler/beam/places-heap-before-frameless-list-head-return";
    private const string Fixture = "beam_head_tuple_return_frame_probe.erl";
    private const string UnexpectedSchedule = "The closed head return still keeps a frame or projects before its heap check.";
    private const string InvalidValue = "The head return or the exact fallback changed its result.";

    internal const string Source = """
        -module(beam_head_tuple_return_frame_probe).
        -export([run/4, open/3]).

        run(Category, Value, Tail, Token) ->
            accept(seed, Category, [], [Value | Tail], Token, [], undefined).
        accept(_State, done, _States, Stack, _Token, _Rest, _Scanner) -> {ok, hd(Stack)};
        accept(_State, _Category, _States, _Stack, Token, _Rest, _Scanner) -> reject(Token).
        open(done, Stack, _Token) -> {ok, hd(Stack)};
        open(_Category, _Stack, Token) -> reject(Token).
        reject(Token) -> {error, Token}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == "accept"
        );
        object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
        if (!body.Select(Opcode).SequenceEqual(new byte[] { ExactEqualTest, TestHeap, GetHead, PutTuple, Return, Move, TailLocalCall })
            || Operands(body[1]) is not [var words, var live] || Value(words) != 3 || Value(live) != 4)
        {
            throw new InvalidOperationException(UnexpectedSchedule);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term done = Term.A("done");
            Term other = Term.A("other");
            Term item = Term.A("item");
            Term token = Term.A("token");
            Term improper = Term.A("improper");
            if (!Term.Tuple(Term.A("ok"), item).Equals(await module.Invoke(context, "run", [done, item, improper, token]))
                || !Term.Tuple(Term.A("error"), token).Equals(await module.Invoke(context, "run", [other, item, Nil.Value, token])))
            {
                throw new InvalidOperationException(InvalidValue);
            }

            return Nil.Value;
        });
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
