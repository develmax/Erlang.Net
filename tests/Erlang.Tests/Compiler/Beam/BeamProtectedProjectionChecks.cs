using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedProjectionChecks
{
    internal const string TestName = "compiler/beam/keeps-short-protected-projections-in-call-registers";
    private const string IncorrectCapture = "Only projections read after a call need their own stack slot.";
    private const string Fixture = "protected_projection_probe.erl";
    private const string Source = """
        -module(protected_projection_probe).
        -export([lookup/2, retain/2]).

        lookup(Ref, Bag) ->
            try
                case Ref of
                    {dict, Key} -> dict:fetch(Key, Bag);
                    {list, Key} -> proplists:get_value(Key, Bag);
                    _ -> error(badarg)
                end
            catch
                error:Reason -> {Reason,Ref,Bag}
            end.

        retain(Ref, Bag) ->
            try
                case Ref of
                    {dict, Key} -> {dict:fetch(Key, Bag), Key};
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
        if (FrameWidth(lookup) != 3 || !lookup.Any(instruction => Opcode(instruction) == TryEnter)
            || !lookup.Any(instruction => IsStackProjection(instruction, "x"))
            || lookup.Any(instruction => IsStackProjection(instruction, "y"))
            || FrameWidth(retain) != 4 || !retain.Any(instruction => IsStackProjection(instruction, "y")))
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

    private static BigInteger FrameWidth(object[] body) =>
        (BigInteger)Property(Items(body.Single(instruction => Opcode(instruction) == Allocate), "Operands").First(), "Value");

    private static bool IsStackProjection(object instruction, string destination) =>
        Opcode(instruction) == GetTupleElement
        && (string)Property(Items(instruction, "Operands").First(), "Kind") == "y"
        && (string)Property(Items(instruction, "Operands").Last(), "Kind") == destination;

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}
