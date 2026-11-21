using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFlatBindingConstantChecks
{
    internal const string TestName = "compiler/beam/flat-binding-constant";
    private const string MissingFold = "The constant should reach the final record through the fresh tuple binding.";
    private const string LostMatch = "A repeated tuple field should still compare the existing binding.";

    private const string Source = """
        -module(beam_flat_binding_constant).
        -export([publish/1, guarded/1]).
        -record(summary,{first,second,limit}).

        publish(Source) ->
            Limit = 137,
            writer:stage(Source, Limit),
            {First, _, Second} = writer:read(Source),
            writer:publish(First, Limit),
            #summary{first=First,second=Second,limit=Limit}.

        guarded(Source) ->
            Limit = 137,
            writer:stage(Source, Limit),
            {Limit, _, Second} = writer:read(Source),
            writer:publish(Second, Limit),
            #summary{first=Limit,second=Second,limit=Limit}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_flat_binding_constant.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        var tuple = Body("publish").Single(instruction => Opcode(instruction) == PutTuple);
        object last = Items(tuple, "Operands")[^1];
        if ((string)Property(last, "Kind") != "integer" || (BigInteger)Property(last, "Value") != 137)
        {
            throw new InvalidOperationException(MissingFold);
        }

        if (!Body("guarded").Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(LostMatch);
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
