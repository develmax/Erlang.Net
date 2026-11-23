using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedCaseFailureOrderChecks
{
    internal const string TestName = "compiler/beam/places-nested-case-failures-after-normal-branches";
    private const string Fixture = "beam_nested_case_failure_probe.erl";
    private const string MisplacedFailure = "Isolated nested case failures must follow all ordinary branches.";

    private const string Source = """
        -module(beam_nested_case_failure_probe).
        -export([route/1]).

        route(Input) ->
            case envelope(Input) of
                {value,Value} -> {value,Value};
                {error,known} -> {error,known};
                {error,Reason} ->
                    Key = case options(Reason) of
                        {ready,Row} ->
                            case Row of
                                {key, K} -> K;
                                {file, F} -> resolve(F)
                            end;
                        _ -> missing
                    end,
                    case lookup(Key) of
                        {found,Good} -> {verified,Good};
                        {missing,Bad} -> {failed,Bad}
                    end
            end.

        envelope({config, Kind}) -> {error,Kind};
        envelope({value, X}) -> {value,X};
        envelope(X) -> X.
        options({r,Data}) -> {ready,Data};
        options(_) -> none.
        resolve({literal,Input}) -> Input;
        resolve(Value) -> Value.
        lookup(missing) -> {missing,missing};
        lookup({raw,Value}) -> Value;
        lookup(Value) -> {found,Value}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == "route"
        );
        byte[] route = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).ToArray();
        if (route.Count(opcode => opcode == CaseEnd) != 3
            || route.SkipWhile(opcode => opcode != CaseEnd).Any(opcode => opcode is not (Label or Line or Move or CaseEnd)))
        {
            throw new InvalidOperationException(MisplacedFailure);
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
