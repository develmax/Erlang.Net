using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPrimitiveBinaryProjectionChecks
{
    internal const string TestName = "compiler/beam/retains-proven-primitive-result-boundary";
    private const string Fixture = "beam_primitive_binary_projection.erl";
    private const string WrongSchedule = "The proven primitive projection did not use its expected retained frame.";
    private const string WrongResult = "The proven primitive projection changed its result or failure reason.";

    private const string Source = """
        -module(beam_primitive_binary_projection).
        -export([run/1]).
        run(Bits) -> primitive(Bits).
        header(<<0:1,N:7,Bytes:N/binary,Tail/binary>>) ->
            {0,leaf,Bytes,Tail};
        header(<<2:2,N:6,Bytes:N/binary,Tail/binary>>) ->
            {1,sequence,Bytes,Tail};
        header(<<3:2,_:6,Bytes/binary>>) ->
            {2,indefinite,Bytes,<<>>}.
        primitive(Bin) ->
            {Form,Tag,Payload,Rest} = header(Bin),
            case Form of
                1 -> {{Tag,definite(Payload)},Rest};
                0 -> {{Tag,Payload},Rest};
                2 -> {Taken,Later} = indefinite(Payload, []),
                     {{Tag,Taken},Later}
            end.
        definite(Bin) when byte_size(Bin) =:= 0 -> [];
        definite(Bin) ->
            {Data,Rest} = primitive(Bin),
            [Data | definite(Rest)].
        indefinite(<<0,0,Rest/binary>>, Acc) ->
            {lists:reverse(Acc),Rest};
        indefinite(Bin, Acc) ->
            {Data,Rest} = primitive(Bin),
            indefinite(Rest, [Data | Acc]).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unknown = CodeGeneration.CompileBeamModule(
            Source.Replace("{2,indefinite,Bytes,<<>>}", "{3,indefinite,Bytes,<<>>}", StringComparison.Ordinal),
            Fixture
        );
        byte[] open = CodeGeneration.CompileBeamModule(
            Source.Replace("[run/1]", "[run/1, definite/1]", StringComparison.Ordinal),
            Fixture
        );
        var dependencies = new BeamDependencySelection(["lists"]);
        var module = BeamModule.Prepare(image, dependencies);
        var unknownModule = BeamModule.Prepare(unknown, dependencies);
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists")]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term leaf = Term.Tuple(Term.A("leaf"), new BitString([0x2a]));
            Term empty = new BitString([]);
            foreach (var (data, tag, value) in new (byte[] Data, string Tag, Term Value)[]
            {
                ([1, 0x2a], "leaf", new BitString([0x2a])),
                ([0x82, 1, 0x2a], "sequence", Term.List(leaf)),
                ([0x80], "sequence", Nil.Value),
                ([0xc0, 1, 0x2a, 0, 0], "indefinite", Term.List(leaf))
            })
            {
                Term expected = Term.Tuple(Term.Tuple(Term.A(tag), value), empty);
                if (!expected.Equals(await module.Invoke(context, "run", new BitString(data))))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            try
            {
                await module.Invoke(context, "run", new BitString([0x83, 1, 0x2a]));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("function_clause")))
            {
            }

            try
            {
                await unknownModule.Invoke(context, "run", new BitString([0xc0, 1, 0x2a, 0, 0]));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), Term.I(3))))
            {
            }

            return Nil.Value;
        });

        byte[] primitive = Opcodes(image, "primitive");
        if (!primitive.Contains(SelectValue) || !primitive.Contains(TrimFrame) || primitive.Contains(CaseEnd)
            || !Opcodes(unknown, "primitive").Contains(CaseEnd)
            || !AllocatesAfterSize(image) || AllocatesAfterSize(open))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static bool AllocatesAfterSize(byte[] image)
    {
        byte[] instructions = Opcodes(image, "definite");
        int measure = Array.IndexOf(instructions, GcBifOne);
        int allocation = Array.IndexOf(instructions, Allocate);

        return measure >= 0 && allocation > measure;
    }

    private static byte[] Opcodes(byte[] image, string name)
    {
        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        object code = typeof(BeamModule).GetField(BeamBinaryHostNames.CodeField, System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic)!.GetValue(module)!;
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var nameAtom, _]
                && atoms[(int)(BigInteger)Property(nameAtom, "Value")] == name
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        return instructions.Skip(start + 1).Select(Opcode).TakeWhile(opcode => opcode != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
