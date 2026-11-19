using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamKeyFindTupleTypeChecks
{
    internal const string TestName = "compiler/beam/keyfind-result-typed-tuple-test";
    private const int AtomOrTupleType = (1 << 0) | (1 << 11);
    private const string WrongType = "The tuple check lost the resolved keyfind result type or acquired an unproven annotation.";
    private const string Module = "beam_keyfind_tuple_type";
    private const string Source = """
        -module(beam_keyfind_tuple_type).
        %% options
        -export([lookup/2]).

        lookup(Key, Rows) ->
            case lists:keyfind(Key, 1, Rows) of
                {Key, Values} -> lists:reverse(Values);
                _ -> []
            end.
        """;

    private const string TaggedSource = """
        -module(beam_keyfind_tuple_type).
        -export([lookup/1]).

        lookup(Tree) ->
            Rows = erl_syntax:get_ann(Tree),
            case lists:keyfind(env, 1, Rows) of
                {env, Values} -> lookup(Tree, Values);
                _ -> erlang:error(badarg)
            end.

        lookup(Tree, Values) -> {Tree, Values}.
        """;

    private const string DirectSource = """
        -module(beam_keyfind_tuple_type).
        -export([lookup/1]).

        lookup(Rows) ->
            {env, Value} = lists:keyfind(env, 1, Rows),
            Value.
        """;

    internal static Task Run()
    {
        byte[] normal = Compile(Source);
        byte[] imported = Compile(Source.Replace("%% options", "-import(lists, [keyfind/3]).", StringComparison.Ordinal)
            .Replace("lists:keyfind", "keyfind", StringComparison.Ordinal));
        byte[] disabled = CompileDisabled(Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal));
        byte[] local = Compile(Source.Replace("lists:keyfind", "keyfind", StringComparison.Ordinal)
            + "\nkeyfind(_Key, _Position, Rows) -> Rows.\n");
        byte[] alternate = Compile(Source.Replace(
            "case lists:keyfind(Key, 1, Rows) of",
            "Found = case Key of skip -> Rows; _ -> lists:keyfind(Key, 1, Rows) end, case Found of",
            StringComparison.Ordinal
        ));
        byte[] provenTag = Compile(TaggedSource);
        byte[] wildcard = Compile(TaggedSource.Replace("{env, Values}", "{_, Values}", StringComparison.Ordinal));
        if (TupleInputType(normal) != AtomOrTupleType || TupleInputType(imported) != AtomOrTupleType
            || TupleInputType(disabled) is not null || TupleInputType(local) is not null || TupleInputType(alternate) is not null
            || TupleInputType(provenTag) is not null || TupleInputType(wildcard) != AtomOrTupleType)
        {
            throw new InvalidOperationException(WrongType);
        }

        byte[] direct = Compile(DirectSource);
        byte[] directTail = Compile(DirectSource.Replace("Value.", "filelib:ensure_dir(Value).", StringComparison.Ordinal));
        byte[] directWildcard = Compile(DirectSource.Replace("{env, Value}", "{_, Value}", StringComparison.Ordinal));
        byte[] otherPosition = Compile(DirectSource.Replace("env, 1, Rows", "env, 2, Rows", StringComparison.Ordinal));
        if (TupleInputType(direct) is not null || HasTagCheck(direct)
            || TupleInputType(directTail) is not null || HasTagCheck(directTail)
            || TupleInputType(directWildcard) != AtomOrTupleType || HasTagCheck(directWildcard)
            || !HasTagCheck(otherPosition))
        {
            throw new InvalidOperationException(WrongType);
        }

        return Task.CompletedTask;
    }

    private static bool HasTagCheck(byte[] beam)
    {
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> compareFirstField = [GetTupleElement, 0x03, 0x00, 0x13, ExactEqualTest];

        return code.AsSpan().IndexOf(compareFirstField) >= 0
            || ReadInstructions(beam).Any(instruction => Opcode(instruction) == IsTaggedTuple);
    }

    private static byte[] CompileDisabled(string source)
    {
        var module = new Parser(source).ParseModule();
        var locations = Enumerable.Range(5, module.DeclaredFunctions.Count)
            .Select(line => new BeamSourceLocation(null, line)).ToArray();
        byte[] beam = new BeamModuleCompiler(module.Name, sourceModule: module).Emit(
            module.DeclaredFunctions,
            locations,
            module.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(module.Attributes, null)
        );
        _ = BeamModule.Prepare(beam);

        return beam;
    }

    private static byte[] Compile(string source)
    {
        byte[] beam = CodeGeneration.CompileBeamModule(source, Module + ".erl");
        _ = BeamModule.Prepare(beam);

        return beam;
    }

    private static IEnumerable<object> ReadInstructions(byte[] beam)
    {
        Assembly assembly = typeof(BeamModule).Assembly;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        Type reader = assembly.GetType(BeamOnLoadMutationParameters.StructureType)!;
        object structure = reader.GetMethod(BeamOnLoadMutationParameters.Read, flags)!
            .Invoke(null, [new ReadOnlyMemory<byte>(beam), null])!;
        Type decoder = assembly.GetType(BeamOnLoadMutationParameters.InstructionType)!;
        var instructions = (IEnumerable)decoder.GetMethod(BeamOnLoadMutationParameters.Read, flags)!.Invoke(null, [structure])!;

        return instructions.Cast<object>();
    }

    private static byte Opcode(object instruction)
    {
        object definition = instruction.GetType().GetProperty(BeamOnLoadMutationParameters.Definition)!.GetValue(instruction)!;

        return (byte)definition.GetType().GetProperty("Opcode")!.GetValue(definition)!;
    }

    private static int? TupleInputType(byte[] beam)
    {
        object test = ReadInstructions(beam).Single(instruction => Opcode(instruction) == IsTuple);
        var operands = (IEnumerable)test.GetType().GetProperty("Operands")!.GetValue(test)!;
        object input = operands.Cast<object>().ElementAt(1);
        if (input.GetType().GetProperty("TypeIndex")!.GetValue(input) is not int type)
        {
            return null;
        }

        byte[] types = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Type").Data;
        int offset = sizeof(int) * 2;
        for (int index = 0; index < type; index++)
        {
            int flagsAtEntry = BinaryPrimitives.ReadUInt16BigEndian(types.AsSpan(offset));
            offset += sizeof(ushort)
                + ((flagsAtEntry & 0x2000) != 0 ? sizeof(long) : 0)
                + ((flagsAtEntry & 0x4000) != 0 ? sizeof(long) : 0)
                + ((flagsAtEntry & 0x8000) != 0 ? sizeof(byte) : 0);
        }

        return BinaryPrimitives.ReadUInt16BigEndian(types.AsSpan(offset));
    }
}
