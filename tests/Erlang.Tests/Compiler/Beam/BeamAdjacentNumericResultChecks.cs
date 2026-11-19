using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamAdjacentNumericResultChecks
{
    internal const string TestName = "compiler/beam/adjacent-arithmetic-number-result";
    internal const string LocalIntegerTestName = "compiler/beam/local-success-integer-result";
    private const int UnboundedIntegerType = 0x0020;
    private const string WrongLocalType = "Expected the unbounded integer annotation only after a proven local return.";
    private const string LocalSource = """
        -module(beam_adjacent_numeric).
        %% options
        -export([value/2,power/2]).
        value(Tag, Input) -> power(Tag, Input) + 1.
        power(left, Input) -> 1 bsl Input;
        power(right, Input) -> 1 bsr Input;
        power(error, Input) -> erlang:error({declined, Input}).
        """;
    private const int NumberType = 0x0028;
    private const string Module = "beam_adjacent_numeric";
    private const string WrongType = "Expected a number annotation only for the successful adjacent mixed arithmetic result.";
    private const string Source = """
        -module(beam_adjacent_numeric).
        %% options
        -export([value/1]).
        value(Input) ->
            (erlang:hd(Input) - 1) band 255.
        """;

    internal static Task Run()
    {
        byte[] mixed = Compile(Source);
        byte[] disabled = Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] noProducer = Compile(Source.Replace(" - 1", "", StringComparison.Ordinal));
        byte[] spilled = Compile(Source.Replace(
            "(erlang:hd(Input) - 1) band 255.",
            "Head = erlang:hd(Input),\n    Delta = Head - 1,\n    Delta band 255.",
            StringComparison.Ordinal
        ));
        if (BinaryBifInputType(mixed) != NumberType
            || BinaryBifInputType(disabled) is not null
            || BinaryBifInputType(noProducer) is not null
            || BinaryBifInputType(spilled) is not null)
        {
            throw new InvalidOperationException(WrongType);
        }

        return Task.CompletedTask;
    }

    internal static Task RunLocalIntegerResult()
    {
        byte[] local = Compile(LocalSource);
        byte[] disabled = Compile(
            LocalSource.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] mixed = Compile(LocalSource.Replace("1 bsr Input", "1.5", StringComparison.Ordinal));
        byte[] bounded = Compile(LocalSource
            .Replace("1 bsr Input", "Input band 255", StringComparison.Ordinal)
            .Replace("1 bsl Input", "Input band 255", StringComparison.Ordinal));
        byte[] remote = Compile(LocalSource.Replace(
            "-> power(Tag, Input)",
            "-> beam_adjacent_numeric:power(Tag, Input)",
            StringComparison.Ordinal
        ));
        byte[] dynamicCall = Compile(LocalSource.Replace(
            "value(Tag, Input) -> power(Tag, Input)",
            "value(Producer, Input) -> Producer(left, Input)",
            StringComparison.Ordinal
        ));
        if (BinaryBifInputType(local, first: true) != UnboundedIntegerType
            || BinaryBifInputType(disabled, first: true) is not null
            || BinaryBifInputType(mixed, first: true) == UnboundedIntegerType
            || BinaryBifInputType(bounded, first: true) == UnboundedIntegerType
            || BinaryBifInputType(remote, first: true) == UnboundedIntegerType
            || BinaryBifInputType(dynamicCall, first: true) == UnboundedIntegerType)
        {
            throw new InvalidOperationException(WrongLocalType);
        }

        return Task.CompletedTask;
    }

    internal static byte[] Compile(string source, bool disabled = false)
    {
        byte[] beam;
        if (disabled)
        {
            var module = new Parser(source).ParseModule();
            var locations = Enumerable.Range(4, module.DeclaredFunctions.Count)
                .Select(line => new BeamSourceLocation(null, line)).ToArray();
            beam = new BeamModuleCompiler(module.Name, sourceModule: module).Emit(
                module.DeclaredFunctions,
                locations,
                module.DeclaredExports.ToHashSet(),
                new BeamModuleMetadata(module.Attributes, null)
            );
        }
        else
        {
            beam = CodeGeneration.CompileBeamModule(source, Module + ".erl");
        }

        _ = BeamModule.Prepare(beam);

        return beam;
    }

    private static int? BinaryBifInputType(byte[] beam, bool first = false) =>
        InstructionInputType(
            beam,
            GcBifTwo,
            3,
            first
        );

    internal static int? InstructionInputType(
        byte[] beam,
        byte opcode,
        int operandIndex,
        bool first = false,
        bool required = true
    )
    {
        Assembly assembly = typeof(BeamModule).Assembly;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        Type reader = assembly.GetType(BeamOnLoadMutationParameters.StructureType)!;
        object structure = reader.GetMethod(BeamOnLoadMutationParameters.Read, flags)!
            .Invoke(null, [new ReadOnlyMemory<byte>(beam), null])!;
        Type decoder = assembly.GetType(BeamOnLoadMutationParameters.InstructionType)!;
        var instructions = (IEnumerable)decoder.GetMethod(BeamOnLoadMutationParameters.Read, flags)!
            .Invoke(null, [structure])!;
        var bifs = instructions.Cast<object>().Where(instruction =>
        {
            object definition = instruction.GetType().GetProperty(BeamOnLoadMutationParameters.Definition)!.GetValue(instruction)!;

            return (byte)definition.GetType().GetProperty("Opcode")!.GetValue(definition)! == opcode;
        });
        object? bif = first ? bifs.FirstOrDefault() : bifs.LastOrDefault();
        if (bif is null)
        {
            return required ? throw new InvalidOperationException("Expected instruction is absent.") : null;
        }

        var operands = (IEnumerable)bif.GetType().GetProperty("Operands")!.GetValue(bif)!;
        object input = operands.Cast<object>().ElementAt(operandIndex);
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
