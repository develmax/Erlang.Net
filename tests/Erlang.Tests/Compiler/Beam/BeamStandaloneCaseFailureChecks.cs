using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStandaloneCaseFailureChecks
{
    internal const string TestName = "compiler/beam/places-isolated-case-exit-after-passthrough";
    private const string Fixture = "beam_projected_case_exit.erl";
    private const string MisplacedFailure = "The isolated case exit must follow the passthrough return and preserve nearby guarded and protected exits.";

    private const string Source = """
        -module(beam_projected_case_exit).
        -export([lookup/2, decorated/2, guarded/2, throwing/2, reply/1]).
        -record(opened, {path, token, state}).

        lookup(Path, Input) ->
            Parent = erlang:make_ref(),
            case ?MODULE:reply(Input) of
                {ok, Token} -> {ok, #opened{path=Path, token={Parent, Token}, state=start}};
                {error, _} = Error -> Error
            end.

        decorated(Path, Input) ->
            case ?MODULE:reply(Input) of
                {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                {error, Problem} -> {error, Path, Problem}
            end.

        guarded(Path, Input) ->
            case ?MODULE:reply(Input) of
                {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                {error, _} = Error when is_atom(Path) -> Error;
                {error, Reason} -> {error, Path, Reason}
            end.

        throwing(Path, Input) ->
            try ?MODULE:reply(Input) of
                {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                {error, _} = Error -> Error
            catch
                throw:Reason -> {thrown, Reason}
            end.

        reply({throw, Reason}) -> throw(Reason);
        reply(Answer) -> Answer.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasTrailingCaseExit("lookup") || !HasTrailingCaseExit("decorated") || !HasTrailingCaseExit("guarded")
            || ReadFunction("throwing").Count(opcode => opcode == TryCaseEnd) != 1)
        {
            throw new InvalidOperationException(MisplacedFailure);
        }

        bool HasTrailingCaseExit(string name)
        {
            byte[] operations = ReadFunction(name);
            int failure = Array.IndexOf(operations, CaseEnd);

            return failure > 0 && operations[..failure].Count(opcode => opcode == Return) >= 2
                && operations[(failure + 1)..].All(opcode => opcode is Label or Line);
        }

        byte[] ReadFunction(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
