using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalEffectfulCaseCarrierChecks
{
    internal const string TestName = "compiler/beam/reuses-terminal-case-tuple-across-effect";

    private const string Source = """
        -module(terminal_effectful_carrier_fixture).
        -export([keep/2,changed/2,identity/2,reads_field/2,rebuild/1]).

        rebuild([]) -> [];
        rebuild([Value | Tail]) ->
            case Value of
                {tag, Field} when is_atom(Field) -> [{tag, Field} | rebuild(Tail)];
                {A, B, C} -> [{A, B, C} | rebuild(Tail)];
                {Other, Field} -> [{changed, Field, Other} | rebuild(Tail)]
            end.

        keep(Input, Context) ->
            Accepted = Input,
            case terminal_effectful_carrier_provider:select(Accepted) of
                ok -> {ok, Context};
                {error, Reason} ->
                    terminal_effectful_carrier_provider:note(Context),
                    {error, Reason}
            end.

        changed(Input, Context) ->
            case terminal_effectful_carrier_provider:select(Input) of
                ok -> {ok, Context};
                {error, Reason} ->
                    terminal_effectful_carrier_provider:note(Context),
                    {changed, Reason}
            end.

        identity(Input, Context) ->
            case terminal_effectful_carrier_provider:select(Input) of
                {error, Reason} ->
                    terminal_effectful_carrier_provider:note(Context),
                    {error, Reason};
                Other ->
                    Other
            end.

        reads_field(Input, Context) ->
            case terminal_effectful_carrier_provider:select(Input) of
                ok -> {ok, Context};
                {error, Reason} ->
                    terminal_effectful_carrier_provider:note(Reason),
                    {error, Reason}
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "terminal_effectful_carrier_fixture.erl"));
        byte[] carrier = Instructions(code, "keep");
        int effect = Array.LastIndexOf(carrier, ExternalCall);
        int check = Array.LastIndexOf(carrier, IsTaggedTuple, effect);
        var afterEffect = carrier.Skip(effect + 1).TakeWhile(instruction => instruction != Return).ToArray();
        if (effect < 0 || check < 0 || carrier.AsSpan(check, effect - check).Contains(GetTupleElement)
            || afterEffect.Length == 0 || afterEffect[0] != Move || afterEffect.Contains(TestHeap)
            || afterEffect.Contains(PutTuple))
        {
            throw new InvalidOperationException(BeamTerminalEffectfulCaseCarrierDiagnostics.Carrier);
        }

        int producer = Array.IndexOf(carrier, ExternalCall);
        if (producer < 0 || !carrier.AsSpan(producer + 1, 3).SequenceEqual([Move, IsTuple, IsTaggedTuple]))
        {
            throw new InvalidOperationException(BeamTerminalEffectfulCaseCarrierDiagnostics.TypedEntry);
        }

        VerifyOriginalConstruction(code, "changed");
        VerifyOriginalConstruction(code, "identity");
        VerifyOriginalConstruction(code, "reads_field");

        byte[] list = Instructions(code, "rebuild");
        if (list.Count(instruction => instruction == PutTuple) != 1
            || list.Count(instruction => instruction == PutList) < 2)
        {
            throw new InvalidOperationException(BeamTerminalEffectfulCaseCarrierDiagnostics.ListHead);
        }
    }

    private static void VerifyOriginalConstruction(object code, string name)
    {
        byte[] instructions = Instructions(code, name);
        int effect = Array.LastIndexOf(instructions, ExternalCall);
        var continuation = instructions.Skip(effect + 1).TakeWhile(instruction => instruction != Return);
        if (effect < 0 || !continuation.Contains(PutTuple))
        {
            throw new InvalidOperationException(BeamTerminalEffectfulCaseCarrierDiagnostics.Control);
        }
    }

    private static byte[] Instructions(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions")[start..end]
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
    }

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
