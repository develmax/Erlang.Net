using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEnrollmentObjectEncoderChecks
{
    internal const string TestName = "compiler/beam/retains-record-fields-across-object-selected-encoding";
    private const string Fixture = "beam_selected_record.erl";
    private const string Module = "beam_selected_record";
    private const string WrongLayout = "An object-selected record did not retain the expected tuple and callback.";
    private const string WrongValue = "An object-selected record encoded an incorrect value.";

    private const string Source = """
        -module(beam_selected_record).
        -export([direct/2, indirect/2, select/1]).

        direct(Input, Tags) ->
            {_,A,B,C} = Input,
            Callback = beam_selected_record:select(B),
            {AB,AL} = pack(A,[1]),
            {BB,BL} = pack(B,[2]),
            {CB,CL} = pack(C,[3],Callback),
            Parts = [AB,BB,CB],
            Length = AL + BL + CL,
            finish(Tags,Parts,Length).

        indirect(Input, Tags) ->
            {_,A,B,C} = Input,
            Callback = beam_selected_record:select(B),
            {AB,AL} = pack(A,[1]),
            {BB,BL} = pack(B,[2]),
            {Body,_} = Callback(type,C,[]),
            {CB,CL} = pack(Body,[3]),
            Parts = [AB,BB,CB],
            Length = AL + BL + CL,
            finish(Tags,Parts,Length).

        select(Key) -> fun(type, Value, []) -> {{Key,Value},2} end.
        pack(Value, [Tag]) -> {{Tag,Value},1}.
        pack(Value, [Tag], Callback) ->
            {Body,_} = Callback(type,Value,[]),
            {{Tag,Body},1}.
        finish(Tags, Parts, Length) -> {Tags,Parts,Length}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] wrongSelector = CodeGeneration.CompileBeamModule(
            Source.Replace("beam_selected_record:select(B)", "beam_selected_record:select(A)", StringComparison.Ordinal),
            Fixture
        );
        byte[] dynamicTags = CodeGeneration.CompileBeamModule(
            Source.Replace("pack(Body,[3])", "pack(Body,Tags)", StringComparison.Ordinal),
            Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Module]));
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Module,
            "select",
            1,
            (context, args) => module.Invoke(context, "select", args[0])
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("record");
            Term first = Term.A("first");
            Term second = Term.A("key");
            Term third = Term.A("selected");
            Term input = Term.Tuple(
                tag,
                first,
                second,
                third
            );
            Term expected = Term.Tuple(
                tag,
                Term.List(
                    Term.Tuple(Term.I(1), first),
                    Term.Tuple(Term.I(2), second),
                    Term.Tuple(Term.I(3), Term.Tuple(second, third))
                ),
                Term.I(3)
            );
            foreach (string encoder in new[] { "direct", "indirect" })
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    encoder,
                    input,
                    tag
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });

        if (!RetainsCallback(image, "direct", indirect: false) || !RetainsCallback(image, "indirect", indirect: true)
            || RetainsCallback(wrongSelector, "direct", indirect: false) || RetainsCallback(dynamicTags, "indirect", indirect: true))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool RetainsCallback(byte[] image, string function, bool indirect)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == function && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int frame = Value(Items(body.Single(instruction => Opcode(instruction) == Allocate), "Operands")[0]);

        return frame == 4 && body.Count(instruction => Opcode(instruction) == TrimFrame) == 1
            && body.Count(instruction => Opcode(instruction) == Swap) == 1
            && body.Any(instruction => Opcode(instruction) == CallFun) == indirect;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
