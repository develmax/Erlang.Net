using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSizeResultRangeChecks
{
    internal const string TestName = "compiler/beam/size-result-propagates-range-through-argument-copies";
    private const string ScheduleMismatch = "The size range must describe the actual comparison or arithmetic argument.";
    private const string ValueMismatch = "The size expression returned an unexpected value or error.";
    private const long KnownSizeUpperBound = (1L << 58) - 1;
    private const int IntegerType = 1 << 5;
    private const string Source = """
        -module(size_result_probe).
        -export([bytes/1, bits/1, items/1, above/2, same/2, unknown/1, guard_path/2, other/2]).

        bytes(Bin) -> byte_size(Bin) + 1.
        bits(Bits) -> bit_size(Bits) rem 8.
        items(Map) -> map_size(Map) - 1.
        above(Bin, Limit) -> byte_size(Bin) >= Limit.
        same(Bin, Limit) -> byte_size(Bin) =:= Limit.
        unknown(Value) -> size_result_probe_peer:byte_size(Value) + 1.
        guard_path(Bin, Minimum) when byte_size(Bin) > Minimum -> big;
        guard_path(_, _) -> none.
        other(Bin, Value) ->
            Size = byte_size(Bin),
            case Value of
                {size, K} -> K + 1;
                _ -> {Size, Value}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "size_result_probe.erl");
        CheckTypes(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term binary = new BitString(new byte[] { 255 });
            Term field = new BitString(new byte[] { 224 }, 3);
            Term none = Term.A("none");
            Term emptyMap = new MapTerm([]);
            if (!(await module.Invoke(context, "bytes", binary)).Equals(Term.I(2))
                || !(await module.Invoke(context, "bits", field)).Equals(Term.I(3))
                || !(await module.Invoke(context, "items", emptyMap)).Equals(Term.I(-1))
                || !(await module.Invoke(
                    context,
                    "above",
                    binary,
                    Term.I(1)
                )).Equals(Term.A("true"))
                || !(await module.Invoke(
                    context,
                    "guard_path",
                    none,
                    Term.I(1)
                )).Equals(none))
            {
                throw new InvalidOperationException(ValueMismatch);
            }

            try
            {
                await module.Invoke(context, "items", binary);
                throw new InvalidOperationException(ValueMismatch);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmap"), binary)))
            {
            }

            return Nil.Value;
        });
    }

    private static void CheckTypes(byte[] image)
    {
        object code = Code(image);
        object table = Call(Type("BeamTypeTableReader"), "Read", Property(code, "Structure"));
        object[] types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();
        for (int index = 0; index < 8; index++)
        {
            object[] body = instructions[entries[index]..entries[index + 1]];
            object consumer = body.Single(instruction => Opcode(instruction) is GcBifTwo or BifTwo or IsLess);
            int position = Opcode(consumer) switch
            {
                BifTwo => 2,
                IsLess => 2,
                _ => 3
            };
            object operand = ((IEnumerable)Property(consumer, "Operands")).Cast<object>().ElementAt(position);
            object? typeIndex = Property(operand, "TypeIndex");
            bool matched = typeIndex is int type
                && (int)Property(types[type], "TypeUnion") == IntegerType
                && (long)Property(types[type], "Minimum") == 0
                && (long)Property(types[type], "Maximum") == KnownSizeUpperBound;
            if (matched != (index < 5))
            {
                throw new InvalidOperationException(ScheduleMismatch);
            }
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
