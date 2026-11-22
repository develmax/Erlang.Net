using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedCaseOriginalResultChecks
{
    internal const string TestName = "compiler/beam/returns-the-original-tagged-result-after-an-identity-checked-attempt";
    private const string WrongResult = "An acknowledged tagged result must keep its checked pair and propagate either rejection.";
    private const string Source = """
        -module(beam_checked_case_original_result).
        -export([run/2, changed/2]).

        run(Input, Check) ->
            case make_pair(Input) of
                {ok, Item} ->
                    case confirm(Check) of
                        accepted -> {ok, Item};
                        Rejected -> Rejected
                    end;
                Failure -> Failure
            end.

        changed(Input, Check) ->
            case make_pair(Input) of
                {ok, Item} ->
                    case confirm(Check) of
                        accepted -> {changed, Item};
                        Rejected -> Rejected
                    end;
                Failure -> Failure
            end.

        make_pair({value, Value}) -> {ok, Value};
        make_pair(Input) -> Input.

        confirm(yes) -> accepted;
        confirm(Result) -> Result.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_checked_case_original_result.erl");
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = Items(code, "Instructions");
        if (ReadsTupleConstruction("run") || !ReadsTupleConstruction("changed"))
        {
            throw new InvalidOperationException(WrongResult);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("payload");
            Term input = Term.Tuple(Term.A("value"), value);
            Term passed = Term.A("yes");
            Term rejected = Term.A("denied");
            if (!Term.Tuple(Term.A("ok"), value).Equals(await module.Invoke(context, "run", [input, passed]))
                || !rejected.Equals(await module.Invoke(context, "run", [input, rejected]))
                || !rejected.Equals(await module.Invoke(context, "run", [rejected, passed]))
                || !Term.Tuple(Term.A("changed"), value).Equals(await module.Invoke(context, "changed", [input, passed])))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });

        bool ReadsTupleConstruction(string name)
        {
            int start = Array.FindIndex(
                body,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongResult);
            }

            return body.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Any(instruction => Opcode(instruction) == PutTuple2);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object value, string name) => ((IEnumerable)Property(value, name)).Cast<object>().ToArray();
}
