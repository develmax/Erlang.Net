using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLengthNumericEntryChecks
{
    internal const string TestName = "compiler/beam/length-numeric-entry";
    private const string Fixture = "beam_length_numeric_entry_probe.erl";
    private const int NumberTypeUnion = 0x0028;
    private const string IncorrectType = "The entry length comparison did not preserve the proven numeric argument type.";
    private const string IncorrectValue = "The entry length comparison altered its value or failure.";
    private const string Source = """
        -module(beam_length_numeric_entry_probe).
        -export([from_number/1, from_unknown/1, escaped_call/1, escape/0]).
        from_number(Value) -> bounded(Value + 0).
        from_unknown(Value) -> unknown(Value).
        escaped_call(Value) -> escaped(Value + 0).
        escape() -> fun escaped/1.
        bounded(Value) when Value =< 127 -> {[Value],1};
        bounded(Value) ->
            Octets = octets(Value),
            Count = length(Octets),
            if Count =< 126 -> {[128 bor Count|Octets],Count+1};
               true -> exit({error,{probe,too_long,Count}})
            end.
        unknown(Value) when Value =< 127 -> {[Value],1};
        unknown(Value) ->
            Octets = octets(Value),
            Count = length(Octets),
            if Count =< 126 -> {[128 bor Count|Octets],Count+1};
               true -> exit({error,{probe,too_long,Count}})
            end.
        escaped(Value) when Value =< 127 -> {[Value],1};
        escaped(Value) ->
            Octets = octets(Value),
            Count = length(Octets),
            if Count =< 126 -> {[128 bor Count|Octets],Count+1};
               true -> exit({error,{probe,too_long,Count}})
            end.
        octets(Value) ->
            case Value of
                10000 -> long_octets(127);
                _ -> [Value band 255]
            end.
        long_octets(0) -> [];
        long_octets(N) -> [0|long_octets(N - 1)].
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckTypes(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term small = new FloatTerm(1.5);
            if (!Term.Tuple(Term.List(small), Term.I(1)).Equals(await module.Invoke(context, "from_number", small))
                || !Term.Tuple(Term.List(Term.I(129), Term.I(128)), Term.I(2)).Equals(
                    await module.Invoke(context, "from_number", Term.I(128))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            await CheckFailure(
                "from_unknown",
                Term.A("alien"),
                ErlangExceptionClasses.Error,
                Term.A(ErlangErrorReasons.BadArithmetic)
            );
            await CheckFailure(
                "from_number",
                Term.I(10_000),
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("error"), Term.Tuple(Term.A("probe"), Term.A("too_long"), Term.I(127)))
            );

            return Nil.Value;

            async Task CheckFailure(
                string function,
                Term value,
                string exceptionClass,
                Term reason
            )
            {
                try
                {
                    _ = await module.Invoke(context, function, value);
                }
                catch (ErlangException error) when (error.ExceptionClass == exceptionClass && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectValue);
            }
        });
    }

    private static void CheckTypes(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        object[] types = Children(Property(code, "Types"), "Entries");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (ReadType("bounded") is not int index
            || (int)Property(types[index], "TypeUnion") != NumberTypeUnion
            || ReadType("unknown") is not null
            || ReadType("escaped") is not null)
        {
            throw new InvalidOperationException(IncorrectType);
        }

        int? ReadType(string function)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == function
            );
            object compare = instructions.Skip(start + 1).First(item => Opcode(item) == IsGreaterOrEqual);

            return (int?)Property(Children(compare, "Operands")[2], "TypeIndex");
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
