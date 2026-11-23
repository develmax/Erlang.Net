using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTaggedAliasChoiceChecks
{
    internal const string TestName = "compiler/beam/guarded-tagged-alias-dispatch-retains-original-tuple";
    private const string ShapeError = "The checked tag groups did not share the outer tuple dispatch.";
    private const string Source = """
        -module(beam_tagged_alias_choice).
        -export([save/2]).

        save({content, {Mime,Path}}=Row, _) when is_list(Mime), is_list(Path) -> {ok,Row};
        save({content, Bad}, _) -> {error,{invalid,{content,Bad}}};
        save({method, {Method,Path}}=Row, _) when is_list(Method), is_list(Path) ->
            case string:to_lower(Method) of
                "get" -> {ok,Row};
                "post" -> {ok,Row};
                _ -> {error,{invalid,Row}}
            end;
        save({method, Bad}, _) -> {error,{invalid,{method,Bad}}}.
        """;

    internal static void Run()
    {
        string alternate = Source.Replace("{invalid,{content,Bad}}", "{different,{content,Bad}}", StringComparison.Ordinal);
        if (!BeginsWithTagSwitch(Source) || BeginsWithTagSwitch(alternate))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool BeginsWithTagSwitch(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_tagged_alias_choice.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().Select(Opcode).ToArray();
        var first = instructions.SkipWhile(opcode => opcode != FunctionInfo).Skip(1)
            .Where(opcode => opcode is not (Label or Line)).Take(5).ToArray();

        return first is [IsTuple, TestArity, GetTupleElement, GetTupleElement, SelectValue];
    }

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
