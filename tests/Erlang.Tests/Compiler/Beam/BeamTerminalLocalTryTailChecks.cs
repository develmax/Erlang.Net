using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalLocalTryTailChecks
{
    internal const string TestName = "compiler/beam/local-try-fallbacks-use-tail-calls";
    private const string Module = "beam_terminal_local_try_tail";
    private const string FallbackError = "unhandled_fallback";
    private const string TailMismatch = "Expected one local tail call on each protected fallback route without a call-site line.";
    private const string ControlMismatch = "Expected the continuation, cleanup and external call sites to retain their source lines.";
    private const string ResultMismatch = "The protected fallback returned an unexpected result.";
    private const string ExceptionMismatch = "An exception from a fallback escaped with the wrong reason or was caught by its own try.";

    private const string Source = """
        -module(beam_terminal_local_try_tail).
        -export([run/1, projected/1, wrapped/1, cleanup/1, remote/1]).
        run(T) ->
            try probe(T) of
                undefined -> fallback(T);
                Value -> Value
            catch _:_ -> fallback(T)
            end.
        projected(T) ->
            try probe(element(2,T)) of
                undefined -> fallback(T);
                Value -> Value
            catch _:_ -> fallback(T)
            end.
        wrapped(T) ->
            {wrapped, try probe(T) of
                undefined -> fallback(T);
                Value -> Value
            catch _:_ -> fallback(T)
            end}.
        cleanup(T) ->
            try probe(T) of
                undefined -> fallback(T);
                Value -> Value
            catch _:_ -> fallback(T)
            after erlang:put(cleaned,true)
            end.
        remote(T) ->
            try probe(T) of
                undefined -> lists:reverse(T);
                Value -> Value
            catch _:_ -> lists:reverse(T)
            end.
        probe(throwme) -> erlang:error(bad_probe);
        probe(fallback_error) -> undefined;
        probe(catch_fallback_error) -> erlang:error(bad_probe);
        probe(T) -> T.
        fallback(fallback_error) -> erlang:error(unhandled_fallback);
        fallback(catch_fallback_error) -> erlang:error(unhandled_fallback);
        fallback(T) -> {fallback,T}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        CheckTail(ReadOpcodes(code, "run"));
        CheckTail(ReadOpcodes(code, "projected"));
        CheckControl(ReadOpcodes(code, "wrapped"), BeamEmissionFormat.LocalCall);
        CheckControl(ReadOpcodes(code, "cleanup"), BeamEmissionFormat.LocalCall);
        CheckControl(ReadOpcodes(code, "remote"), BeamEmissionFormat.TailExternalCallWithFrame);

        BeamModule image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term fail = Term.A("throwme");
            Term undefined = Term.A("undefined");
            Term success = Term.A("value");
            Term invalidTuple = Term.A("invalid_tuple");
            await CheckResult("run", undefined, Fallback(undefined));
            await CheckResult("run", fail, Fallback(fail));
            await CheckResult("run", success, success);
            await CheckResult("projected", Term.Tuple(Term.A("token"), success), success);
            await CheckResult("projected", invalidTuple, Fallback(invalidTuple));
            await CheckResult("wrapped", fail, Term.Tuple(Term.A("wrapped"), Fallback(fail)));
            await CheckResult("cleanup", fail, Fallback(fail));
            if (!Term.A("true").Equals(context.Dictionary[Term.A("cleaned")]))
            {
                throw new InvalidOperationException(ResultMismatch);
            }

            await CheckEscaped("fallback_error");
            await CheckEscaped("catch_fallback_error");

            return Nil.Value;

            async ValueTask CheckResult(string function, Term input, Term expected)
            {
                if (!expected.Equals(await image.Invoke(context, function, input)))
                {
                    throw new InvalidOperationException(ResultMismatch);
                }
            }

            async ValueTask CheckEscaped(string input)
            {
                try
                {
                    await image.Invoke(context, "run", Term.A(input));
                }
                catch (ErlangException error) when (Term.A(FallbackError).Equals(error.Reason))
                {
                    return;
                }

                throw new InvalidOperationException(ExceptionMismatch);
            }
        });
    }

    private static Term Fallback(Term value) => Term.Tuple(Term.A("fallback"), value);

    private static byte[] ReadOpcodes(object code, string name)
    {
        object function = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Single(candidate => (string)Property(candidate, "Name") == name);
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>().Skip(first).Take(end - first)
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode"))
            .ToArray();
    }

    private static void CheckTail(byte[] operations)
    {
        int normal = Array.IndexOf(operations, BeamEmissionFormat.TryEnd);
        int handler = Array.IndexOf(operations, BeamEmissionFormat.TryCase);
        ReadOnlySpan<byte> tail = [BeamEmissionFormat.Move, BeamEmissionFormat.TailLocalCallWithFrame];
        if (normal < 0 || handler <= normal
            || operations.AsSpan(normal, handler - normal).IndexOf(tail) < 0
            || operations.AsSpan(handler).IndexOf(tail) < 0
            || operations.Count(operation => operation == BeamEmissionFormat.TailLocalCallWithFrame) != 2)
        {
            throw new InvalidOperationException(TailMismatch);
        }
    }

    private static void CheckControl(byte[] operations, byte call)
    {
        int normal = Array.IndexOf(operations, BeamEmissionFormat.TryEnd);
        ReadOnlySpan<byte> located = [BeamEmissionFormat.Line, call];
        if (normal < 0 || operations.AsSpan(normal).IndexOf(located) < 0
            || operations.Contains(BeamEmissionFormat.TailLocalCallWithFrame))
        {
            throw new InvalidOperationException(ControlMismatch);
        }
    }
}
