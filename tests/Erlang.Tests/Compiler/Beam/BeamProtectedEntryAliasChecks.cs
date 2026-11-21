using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedEntryAliasChecks
{
    internal const string TestName = "compiler/beam/keeps-original-arguments-live-when-installing-a-handler";
    private const string IncorrectCapture = "Installing a handler must keep the original argument live in X.";
    private const string Fixture = "protected_projection_probe.erl";
    private const string Source = """
        -module(protected_projection_probe).
        -export([lookup/2, retain/2]).

        lookup(Ref, Bag) ->
            try
                case Ref of
                    {map, Key} -> maps:get(Key, Bag);
                    {list, Key} -> proplists:get_value(Key, Bag);
                    _ -> error(badarg)
                end
            catch
                error:Reason -> {Reason,Ref,Bag}
            end.

        retain(Ref, Bag) ->
            try
                case Ref of
                    {map, Key} -> {maps:get(Key, Bag), Key};
                    _ -> error(badarg)
                end
            catch
                error:Reason -> {Reason,Ref,Bag}
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object[] lookup = Body("lookup");
        object[] retain = Body("retain");
        int handler = Array.FindIndex(lookup, instruction => Opcode(instruction) == TryEnter);
        if (handler < 0 || Opcode(lookup.Skip(handler + 1).First(instruction => Opcode(instruction) != Line)) != IsTuple
            || !lookup.Any(instruction => Opcode(instruction) == TryCase)
            || !retain.Any(instruction => IsStackProjection(instruction, "y")))
        {
            throw new InvalidOperationException(IncorrectCapture);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool IsStackProjection(object instruction, string destination) =>
        Opcode(instruction) == GetTupleElement
        && (string)Property(Items(instruction, "Operands").First(), "Kind") == "y"
        && (string)Property(Items(instruction, "Operands").Last(), "Kind") == destination;

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
