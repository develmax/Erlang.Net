using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamAdjacentTupleProjectionOrderChecks
{
    internal const string TestName = "compiler/beam/orders-independent-tuple-projections";
    private const string Fixture = "beam_tuple_projection_order_probe.erl";
    private const string Marker = "tuple_projection_marker";
    private const string IncorrectOrder = "Independent projections of the same tuple were not ordered by field.";
    private const string IncorrectAlias = "A projection that overwrites the source was not kept last.";
    private const string IncorrectValue = "The tuple projections changed the value, failure, or process dictionary effect.";

    internal const string Source = """
        -module(beam_tuple_projection_order_probe).
        -export([cache/2, reverse/1]).
        -record(cache,{fd,unused,c}).

        cache(#cache{fd=Fd,c=C}, Name) ->
            erase(tuple_projection_marker),
            cache(Fd, Name, C).
        cache(Fd, Name, C) -> {Fd,Name,C}.
        reverse({A,B,C}) -> {C,B,A}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[][] cache = Projections("cache", 2);
        if (cache.Length != 2 || !cache.Select(operands => Value(operands[1])).SequenceEqual([1, 3]))
        {
            throw new InvalidOperationException(IncorrectOrder);
        }

        object[][] reverse = Projections("reverse", 1);
        if (reverse.Length != 3 || Value(reverse[0][0]) == Value(reverse[0][2])
            || Value(reverse[1][0]) == Value(reverse[1][2]) || Value(reverse[2][0]) != Value(reverse[2][2]))
        {
            throw new InvalidOperationException(IncorrectAlias);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term marker = Term.A(Marker);
            context.Dictionary[marker] = Term.A("set");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term three = Term.I(3);
            Term data = Term.Tuple(
                Term.A("cache"),
                one,
                two,
                three
            );
            Term name = Term.A("name");
            Term original = Term.Tuple(one, two, three);
            if (!Term.Tuple(one, name, three).Equals(await module.Invoke(
                context,
                "cache",
                data,
                name
            ))
                || context.Dictionary.ContainsKey(marker)
                || !Term.Tuple(three, two, one).Equals(await module.Invoke(context, "reverse", original)))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            context.Dictionary[marker] = Term.A("set");
            try
            {
                _ = await module.Invoke(
                    context,
                    "cache",
                    original,
                    name
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)) && context.Dictionary.ContainsKey(marker))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });

        object[][] Projections(string name, int arity)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, var count] && atoms[Value(atom)] == name && Value(count) == arity
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) == GetTupleElement).Select(Operands).ToArray();
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
