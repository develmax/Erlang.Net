using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDirectAnonymousFunChecks
{
    internal const string TestName = "compiler/beam/keeps-direct-anonymous-application-frames";
    private const string Failure = "Direct anonymous application changed capture, scope, function identity, or failure behavior.";
    private const string Fixture = "beam_direct_anonymous_fun.erl";
    private const string Source = """
        -module(beam_direct_anonymous_fun).
        -export([value/1, ordinal/1, protected_value/1, callable/1, reference/0]).

        value(Input) ->
            Result = (fun() ->
                Local = Input,
                case Local of
                    first -> changed;
                    Other -> Other
                end
            end)(),
            Local = outside,
            {Result, Local}.

        ordinal(Input) ->
            (fun() -> erlang:put(anonymous_application_marker, Input) end)(),
            fun() -> Input end.

        protected_value(Reason) ->
            try (fun() -> erlang:error(Reason) end)()
            catch error:Actual:Trace -> {Actual, Trace}
            end.

        callable(Input) -> (fun(Inside) -> {retained, Inside} end)(Input).
        reference() -> fun reference/0.
        """;
    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        Func<string, object[]> body = ReadBodies(beam);

        bool UsesClosure(string name) => body(name).Any(instruction => Opcode(instruction) == MakeFun);
        if (!UsesClosure("value") || !UsesClosure("protected_value")
            || body("ordinal").Count(instruction => Opcode(instruction) == MakeFun) != 2
            || body("-ordinal/1-fun-1-").Length == 0 || body("-ordinal/1-fun-0-").Length == 0
            || !UsesClosure("callable") || !UsesClosure("reference"))
        {
            throw new InvalidOperationException(Failure);
        }

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term other = Term.A("other");
            Term outside = Term.A("outside");
            if (!Term.Tuple(Term.A("changed"), outside).Equals(await module.Invoke(context, "value", first))
                || !Term.Tuple(other, outside).Equals(await module.Invoke(context, "value", other))
                || !Term.Tuple(Term.A("retained"), other).Equals(await module.Invoke(context, "callable", other))
                || await module.Invoke(context, "protected_value", other) is not TupleTerm
                {
                    Items:
                    [
                        Atom { Name: "other" },
                        Cons { Head: TupleTerm { Items.Count: 4 } }
                    ]
                })
            {
                throw new InvalidOperationException(Failure);
            }

            return Nil.Value;
        });
    }

    private static Func<string, object[]> ReadBodies(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return name =>
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == name
            );

            return start < 0 ? [] : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        };
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
