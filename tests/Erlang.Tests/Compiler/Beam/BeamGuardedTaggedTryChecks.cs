using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTaggedTryChecks
{
    internal const string TestName = "compiler/beam/guarded-tagged-try";
    private const string WrongFrame = "Keep only the live selector and the try token in the projected arm's frame.";
    private const string WrongLifetime = "The checked projection and selector kill must precede the protected dispatch.";
    private const string WrongCallee = "A call to a shadowing local element function must remain a local call.";

    private const string Source = """
        -module(beam_guarded_tagged_try).
        -export([run/2]).

        run(Kind, Input) when is_binary(Input) ->
            try {ok, dispatch(Kind, element(1, produce(Input)))}
            catch
                Class:Exception:Stack when Class =:= error; Class =:= exit ->
                    case Exception of
                        {failure,{parse,Reason}} -> {failure,{parse,{Reason,Stack}}};
                        Reason -> {failure,{parse,{Reason,Stack}}}
                    end
            end;
        run(Kind, Input) ->
            try {ok, dispatch(Kind, Input)}
            catch
                Class:Exception:Stack when Class =:= error; Class =:= exit ->
                    case Exception of
                        {failure,{parse,Reason}} -> {failure,{parse,{Reason,Stack}}};
                        Reason -> {failure,{parse,{Reason,Stack}}}
                    end
            end.

        produce(<<0>>) -> invalid;
        produce(<<1>>) -> {payload,ignored};
        produce(<<2>>) -> throw(producer_throw);
        produce(Other) -> {Other,ignored}.

        dispatch(wrapped, Input) -> erlang:error({failure,{parse,Input}});
        dispatch(exiting, Input) -> exit(Input);
        dispatch(thrown, Input) -> throw(Input);
        dispatch(plain, Input) -> Input.
        """;

    internal static void Run()
    {
        object[] body = RunBody(Source);
        int[] sizes = body.Where(instruction => Opcode(instruction) == Allocate)
            .Select(instruction => Operand(instruction, 0)).ToArray();
        if (sizes is not [2, 1])
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] operations = body.Select(Opcode).ToArray();
        int projection = Array.IndexOf(operations, BifTwo);
        if (projection < 0 || Array.IndexOf(operations, TryEnter) >= projection
            || Array.IndexOf(operations, TryEnd) <= projection
            || operations[(projection + 1)..(projection + 5)] is not [Move, InitYRegisters, LocalCall, TryEnd])
        {
            throw new InvalidOperationException(WrongLifetime);
        }

        string shadowed = Source.Replace(
            "-export([run/2]).",
            "-export([run/2]).\n-compile({no_auto_import,[element/2]}).",
            StringComparison.Ordinal
        ) + "\nelement(1, {Value, _}) -> {shadowed, Value}; element(_, Value) -> Value.\n";
        if (RunBody(shadowed).Any(instruction => Opcode(instruction) == BifTwo))
        {
            throw new InvalidOperationException(WrongCallee);
        }
    }

    private static object[] RunBody(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_guarded_tagged_try.erl"));
        object[] instructions = Items(code, "Instructions");
        object function = Items(code, "Functions").Single(candidate => (string)Property(candidate, "Name") == "run");
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return instructions[start..end];
    }

    private static int Operand(object instruction, int index) => (int)(BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
