using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedConstantCallChecks
{
    internal const string TestName = "compiler/beam/keeps-exact-catch-registers-with-literal-call-arguments";
    private const string Fixture = "beam_protected_constant_probe.erl";
    private const string Inspect = "inspect";
    private const string IncorrectValue = "The protected call changed an argument, exact catch, or reraised error.";
    private const string IncorrectLayout = "Only the literal protected call should use the one-slot catch frame.";

    private const string Source = """
        -module(beam_protected_constant_probe).
        -export([inspect/1]).

        inspect(shortcut) -> true;
        inspect(Value) ->
            try check(Value, [expected], <<1:2>>, 2) of
                _ -> true
            catch
                throw:false -> false
            end.

        check(known, [expected], <<1:2>>, 2) -> ignored;
        check(caught, [expected], <<1:2>>, 2) -> throw(false);
        check(different, [expected], <<1:2>>, 2) -> throw(other_reason);
        check(raised, [expected], <<1:2>>, 2) -> erlang:error(probe_error).

        options(side_error) -> throw(false);
        options(_) -> [expected].
        """;

    internal static async Task Run()
    {
        byte[] literal = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] dynamicArguments = CodeGeneration.CompileBeamModule(
            Source.Replace("Value, [expected]", "Value, options(Value)", StringComparison.Ordinal), Fixture
        );
        var first = BeamModule.Prepare(literal);
        var second = BeamModule.Prepare(dynamicArguments);
        var yes = Term.A("true");
        var no = Term.A("false");
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!yes.Equals(await first.Invoke(context, Inspect, Term.A("shortcut")))
                || !yes.Equals(await first.Invoke(context, Inspect, Term.A("known")))
                || !no.Equals(await first.Invoke(context, Inspect, Term.A("caught")))
                || !yes.Equals(await second.Invoke(context, Inspect, Term.A("known")))
                || !no.Equals(await second.Invoke(context, Inspect, Term.A("side_error"))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await first.Invoke(context, Inspect, Term.A("different"));
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Throw
                && error.Reason.Equals(Term.A("other_reason")))
            {
            }

            try
            {
                await first.Invoke(context, Inspect, Term.A("raised"));
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("probe_error")))
            {
            }

            return Nil.Value;
        });

        if (Frame(literal) != 1 || Frame(dynamicArguments) <= 1)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static int Frame(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Inspect && Value(arity) == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object frame = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Single(instruction => Opcode(instruction) == Allocate);

        return Value(Items(frame, "Operands")[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
