using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedTupleReturnTrimChecks
{
    internal const string TestName = "compiler/beam/trim-nested-tuple-return-after-call-arguments";
    private const string MissingTrim = "The live tuple field was not relocated into the late call slot.";
    private const string PureCallTrim = "A pure call received the relocation reserved for a potentially recursive call.";
    private const string Source = """
        -module(beam_nested_tuple_return_trim).
        -export([prior/1, build/1]).
        prior(Opts) ->
            Parent = prep:option(parent, Opts),
            Root = prep:new(Parent, []),
            Layout = prep:layout(vertical),
            Extra = prep:group(vertical,Root,[{label,"Extra"}]),
            Area = prep:group(horizontal,Root,[{label,"Area"}]),
            Box = prep:layout(horizontal),
            Stage = prep:new(Root,-1,[{size,{3,3}}]),
            Toggle = prep:toggle(Root,-1,"Toggle",[]),
            prep:add(Area,Stage),
            prep:add(Box,Area),
            prep:add(Extra,Toggle),
            prep:add(Layout,Box),
            prep:add(Layout,Extra),
            prep:watch(Toggle,click,[]),
            prep:revalidate(Root,5,5),
            prep:attach(Root,Layout),
            {Root,{ready,Root,Opts,undefined,Stage}}.
        build(Opts) ->
            Root = prep:new(Opts, []),
            Layout = prep:layout(vertical),
            Area = prep:group(horizontal,Root,[{label,"Area"}]),
            Box = prep:layout(horizontal),
            Stage = prep:new(Root,-1,[{size,{3,3}}]),
            Toggle = prep:toggle(Root,-1,"Toggle",[]),
            prep:add(Area,Stage),
            prep:add(Box,Area),
            prep:add(Layout,Box),
            prep:watch(Toggle,click,[]),
            prep:revalidate(Root,5,5),
            prep:attach(Root,Layout),
            {Root,{ready,Root,Opts,undefined,Stage}}.
        """;

    internal static void Run()
    {
        if (!HasRelocatedReturn(Source))
        {
            throw new InvalidOperationException(MissingTrim);
        }

        string pure = Source.Replace("prep:attach", "math:pow", StringComparison.Ordinal);
        if (HasRelocatedReturn(pure))
        {
            throw new InvalidOperationException(PureCallTrim);
        }
    }

    private static bool HasRelocatedReturn(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_nested_tuple_return_trim.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>();
        object entry = functions.Single(candidate => (string)Property(candidate, "Name") == "build");
        int start = (int)Property(entry, "FirstInstructionIndex");
        int end = (int)Property(entry, "EndInstructionIndex");
        object[] body = instructions[start..end].Where(instruction => Opcode(instruction) != Line).ToArray();

        return body is [.., var relocate, var trim, var load, var call, var heap, var inner, var outer, var deallocate, var ret]
            && Opcode(relocate) == Move && Operands(relocate) is [("y", 0), ("y", 1)]
            && Opcode(trim) == TrimFrame && Operands(trim).Select(operand => operand.Value).SequenceEqual([1, 3])
            && Opcode(load) == Move && Operands(load) is [("y", 1), ("x", 0)]
            && Opcode(call) == ExternalCall && Opcode(heap) == TestHeap
            && Opcode(inner) == PutTuple && Opcode(outer) == PutTuple
            && Opcode(deallocate) == Deallocate && Operands(deallocate)[0].Value == 3 && Opcode(ret) == Return;
    }

    private static (string Kind, int Value)[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
            .Select(operand => ((string)Property(operand, "Kind"), (int)(BigInteger)Property(operand, "Value"))).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
