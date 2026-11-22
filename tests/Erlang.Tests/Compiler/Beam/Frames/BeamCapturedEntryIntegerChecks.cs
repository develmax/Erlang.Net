using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCapturedEntryIntegerChecks
{
    internal const string TestName = "compiler/beam/types-a-private-product-of-a-captured-list-length";
    private const string ModuleFile = "beam_captured_entry_integer.erl";
    private const int IntegerUnion = 0x0020;
    private const int OnlyLowerBound = 0x2000;
    private const long CapturedLengthUpper = (1L << 58) - 1;
    private const string IncorrectHelper = "A captured list length must retain its finite range and incoming GC root.";
    private const string Incorrect = "Only a proven private length capture may annotate the first multiplication operand.";

    private const string Source = """
        -module(beam_captured_entry_integer).
        -export([render/1]).
        render(Items) ->
            Depth = length(Items),
            [scaled(Depth + 1) ++ Entry || Entry <- Items] ++ scaled(Depth).
        scaled(Depth) ->
            [$\n | lists:duplicate(2 * Depth, $\s)].
        """;

    internal static Task Run()
    {
        string shadowed = Source.Replace("Entry || Entry", "Depth || Depth", StringComparison.Ordinal);
        string escaped = Source.Replace(
            "-export([render/1]).",
            "-export([render/1,escaped/0]).\nescaped() -> fun scaled/1.",
            StringComparison.Ordinal
        );
        if (ReadProductLowerBound(Source) != 0 || ReadProductLowerBound(shadowed) is not null
            || ReadProductLowerBound(escaped) is not null)
        {
            throw new InvalidOperationException(Incorrect);
        }

        string unknown = Source.Replace("length(Items)", "read_depth(Items)", StringComparison.Ordinal)
            + "\nread_depth(Items) -> caller:untyped_depth(Items).\n";
        if (ReadHelperAdditionRange(Source) != (0L, CapturedLengthUpper)
            || ReadHelperAdditionRange(shadowed) is not null || ReadHelperAdditionRange(unknown) is not null)
        {
            throw new InvalidOperationException(IncorrectHelper);
        }

        return Task.CompletedTask;
    }

    private static (long Minimum, long Maximum)? ReadHelperAdditionRange(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, ModuleFile));
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Children(code, "Instructions").ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))]
                .StartsWith("-render/1-lc$", StringComparison.Ordinal)
        );
        if (info < 0)
        {
            return null;
        }

        object? calculation = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .FirstOrDefault(instruction => Opcode(instruction) == GcBifTwo);
        if (calculation is null)
        {
            return null;
        }

        object[] operands = Children(calculation, "Operands").ToArray();
        if (Value(operands[1]) != 3 || Value(operands[3]) != 2 || Value(operands[4]) != 1
            || (string)Property(operands[3], "Kind") != "x"
            || Property(operands[3], "TypeIndex") is not int typeIndex)
        {
            return null;
        }

        object type = Children(Property(code, "Types"), "Entries").ElementAt(typeIndex);

        return (int)Property(type, "TypeUnion") == IntegerUnion
            ? ((long)Property(type, "Minimum"), (long)Property(type, "Maximum")) : null;
    }

    private static long? ReadProductLowerBound(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, ModuleFile));
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Children(code, "Instructions").ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == "scaled"
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(Incorrect);
        }

        object calculation = instructions.Skip(entry + 1).First(instruction => Opcode(instruction) is not (Label or Line));
        if (Opcode(calculation) != GcBifTwo)
        {
            throw new InvalidOperationException(Incorrect);
        }

        object[] operands = Children(calculation, "Operands").ToArray();
        if (Property(operands[3], "TypeIndex") is not int typeIndex)
        {
            return null;
        }

        object type = Children(Property(code, "Types"), "Entries").ElementAt(typeIndex);

        return (int)Property(type, "TypeUnion") == IntegerUnion
            && (int)Property(type, "MetadataFlags") == OnlyLowerBound
            && Value(operands[3]).IsZero && Value(operands[4]) == 2
            ? (long)Property(type, "Minimum")
            : null;
    }
}
