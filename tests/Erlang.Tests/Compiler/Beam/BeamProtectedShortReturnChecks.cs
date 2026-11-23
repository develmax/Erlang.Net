using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedShortReturnChecks
{
    internal const string TestName = "compiler/beam/copies-short-return-continuations-after-protected-regions";
    private const string IncorrectCapture = "A protected path that returns must close the handler and release the original frame.";
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
        if (!ReturnsAfterTry(lookup) || !ReturnsAfterTry(retain)
            || !lookup.Any(instruction => Opcode(instruction) == TryCase)
            || !retain.Any(instruction => Opcode(instruction) == TryCase))
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

    private static bool ReturnsAfterTry(object[] body)
    {
        int last = Array.FindIndex(body, instruction => Opcode(instruction) == TryEnd);

        return last >= 0 && body.Skip(last + 1).Take(2).Select(Opcode).SequenceEqual(new byte[] { Deallocate, Return });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
