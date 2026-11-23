using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalCaseConstantChecks
{
    internal const string TestName = "compiler/beam/folds-fresh-literal-bindings-in-terminal-case-arms";
    private const string Fixture = "beam_terminal_case_constants.erl";
    private const string WrongStructure = "The terminal case did not preserve constant bindings and match checks.";

    private const string Source = """
        -module(beam_terminal_case_constants).
        -export([fold/1, nested/2, bound/2, exported/1]).
        -record(issue, {code, text}).
        fold(Selector) ->
            case Selector of
                left ->
                    Description = #issue{code=1,text="left"},
                    {reply, Description};
                _ ->
                    Description = #issue{code=2,text="right"},
                    {reply, Description}
            end.
        nested(Selector, Kind) ->
            case Selector of
                left ->
                    case Kind of
                        inner ->
                            Code = 7, Desc = {issue,Code}, {reply,Desc};
                        _ ->
                            Code = 11, Desc = {issue,Code}, {reply,Desc}
                    end;
                _ -> none
            end.
        bound(Selector, Data) ->
            case Selector of
                enforce ->
                    Data = {ok,3},
                    {reply,Data};
                _ -> {reply,Data}
            end.
        exported(Selector) ->
            Result =
                case Selector of
                    left -> Data = {ok,1}, 4;
                    _ -> Data = {ok,2}, 5
                end,
            {Result,Data}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        if (Operations("fold").Contains(PutTuple) || Operations("nested").Contains(PutTuple)
            || !Operations("bound").Contains(BadMatch) || !Operations("exported").Contains(PutTuple))
        {
            throw new InvalidOperationException(WrongStructure);
        }

        return Task.CompletedTask;

        byte[] Operations(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction =>
                Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongStructure);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
