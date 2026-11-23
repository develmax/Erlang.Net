using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPhysicalLabelOrderChecks
{
    internal const string TestName = "compiler/beam/numbers-final-labels-in-physical-order";
    private const string Fixture = "beam_label_identity_probe.erl";
    private const string NonphysicalLabels = "Labels must be numbered from one in final physical code order.";
    private const string Source = """
        -module(beam_label_identity_probe).
        -on_load(init/0).
        -export([pack/1, via/2, caught/1, initialized/0]).

        init() ->
            persistent_term:put({?MODULE, ready}, true),
            ok.

        initialized() ->
            persistent_term:get({?MODULE, ready}, false).

        pack(Input) ->
            <<Head, Tail/binary>> = produce(Input),
            case Head band 128 of
                128 -> <<0,Head,Tail/binary>>;
                _ -> <<Head,Tail/binary>>
            end.

        via(Captured, Input) ->
            Fun = wrap(Captured),
            Fun(Input).

        wrap(Captured) ->
            fun(Input) -> {Captured, pack(Input)} end.

        caught(Input) ->
            try pack(Input) of
                Value -> {ok,Value}
            catch
                error:Reason -> {error,Reason}
            end.

        produce(high) -> <<128,27,13>>;
        produce(low) -> <<4,3>>;
        produce(Other) -> Other.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        int[] labels = Items(code, "Instructions")
            .Where(instruction => Opcode(instruction) == Label)
            .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands").Single(), "Value"))
            .ToArray();
        if (labels.Length == 0 || !labels.SequenceEqual(Enumerable.Range(1, labels.Length)))
        {
            throw new InvalidOperationException(NonphysicalLabels);
        }

        CheckDeferredClosureEntry();
    }

    private static void CheckDeferredClosureEntry()
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        var compiler = new BeamModuleCompiler("beam_deferred_label_probe");
        System.Type compilerType = compiler.GetType();
        var deferred = (Dictionary<int, int>)compilerType.GetField("deferredEntries", PrivateInstance)!
            .GetValue(compiler)!;
        deferred.Add(-1, 1);
        compilerType.GetField("emittedLabels", PrivateInstance)!.SetValue(compiler, new[] { 0, 3, 2, 1 });
        FieldInfo closureField = compilerType.GetField("closures", PrivateInstance)!;
        var closures = (IList)closureField.GetValue(compiler)!;
        object closure = RuntimeHelpers.GetUninitializedObject(closureField.FieldType.GetGenericArguments()[0]);
        closure.GetType().GetProperty("Entry", PrivateInstance)!.SetValue(closure, -1);
        closures.Add(closure);

        // Compact one-byte unsigned operands carry the label value in the high nibble.
        ReadOnlyMemory<byte> instructions = new byte[] { Label, 3 << 4, Return, Label, 1 << 4, Return, EndCode };
        object result = Call(
            compiler,
            "OrderFinalPhysicalLabels",
            instructions,
            4
        );
        if (result is null || (int)Call(compiler, "MapClosureEntryLabel", -1) != 1)
        {
            throw new InvalidOperationException(NonphysicalLabels);
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
