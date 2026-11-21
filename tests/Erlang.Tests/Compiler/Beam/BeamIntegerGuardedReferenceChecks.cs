using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamIntegerGuardedReferenceChecks
{
    internal const string TestName = "compiler/beam/selects-local-function-references-by-exact-integer-guards";
    private const string Fixture = "beam_integer_guarded_references.erl";
    private const string WrongLayout = "Distinct integer guards did not share their reference dispatch.";
    private const string WrongValue = "The guard selected the wrong local function or fallback.";

    private const string Source = """
        -module(beam_integer_guarded_references).
        -export([run/2, duplicate/2]).
        run(Key, Value) -> F = choose(Key), F(Value).
        duplicate(Key, Value) -> F = overlap(Key), F(Value).
        choose(Key) when Key =:= 2 -> fun second/1;
        choose(Key) when Key =:= -1 -> fun first/1;
        choose(Key) when Key =:= 7 -> fun third/1;
        choose(Key) -> fun(Value) -> {Key,Value} end.
        overlap(Key) when Key =:= 2 -> fun second/1;
        overlap(Key) when Key =:= 2 -> fun first/1;
        overlap(_) -> fun(Value) -> {fallback,Value} end.
        first(Value) -> {first,Value}.
        second(Value) -> {second,Value}.
        third(Value) -> {third,Value}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("value");
            Term unknown = Term.I(3);
            Term floating = new FloatTerm(2);
            await Expect("run", Term.I(-1), Term.A("first"));
            await Expect("run", Term.I(2), Term.A("second"));
            await Expect("run", Term.I(7), Term.A("third"));
            await Expect("run", unknown, unknown);
            await Expect("run", floating, floating);
            await Expect("duplicate", Term.I(2), Term.A("second"));
            await Expect("duplicate", unknown, Term.A("fallback"));

            return Nil.Value;

            async Task Expect(string entry, Term key, Term tag)
            {
                if (!Term.Tuple(tag, value).Equals(await module.Invoke(
                    context,
                    entry,
                    key,
                    value
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });

        if (!UsesValueDispatch(image, "choose") || UsesValueDispatch(image, "overlap"))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool UsesValueDispatch(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == function && Value(arity) == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] operations = instructions.Skip(start + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).Select(Opcode).ToArray();

        return operations.Count(op => op == SelectValue) == 1 && !operations.Contains(ExactEqualTest);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
