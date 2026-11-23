using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamOutgoingArgumentChecks
{
    internal const string TestName = "compiler/beam/outgoing-call-removes-dead-roundtrip";
    private const string ArgumentMoveMismatch = "Call argument preparation retained a dead scratch copy or removed a live argument.";

    internal static Task Run()
    {
        const string source = """
            -module(roundtrip_args).
            -export([stop/1, stop_tail/1, repeat_tail/1]).
            stop(Name) ->
                case whereis(Name) of
                    Pid when is_pid(Pid) ->
                        erlang:exit(Pid,shutdown),
                        ok;
                    _ ->
                        not_running
                end.
            stop_tail(Name) ->
                case whereis(Name) of
                    Pid when is_pid(Pid) ->
                        erlang:exit(Pid,normal);
                    _ ->
                        not_running
                end.
            repeat_tail(Name) ->
                case whereis(Name) of
                    Pid when is_pid(Pid) ->
                        probe:accept(Pid,tag,Pid);
                    _ ->
                        not_running
                end.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "roundtrip_args.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> ordinaryCall =
        [
            BeamEmissionFormat.IsPid, 0x35, 0x03,
            BeamEmissionFormat.Move, 0x52, 0x13,
            BeamEmissionFormat.Line, 0x30,
            BeamEmissionFormat.ExternalCall, 0x20, 0x10
        ];
        ReadOnlySpan<byte> tailCall =
        [
            BeamEmissionFormat.IsPid, 0x65, 0x03,
            BeamEmissionFormat.Move, 0xA2, 0x13,
            BeamEmissionFormat.Line, 0x60,
            BeamEmissionFormat.TailExternalCallWithFrame, 0x20, 0x10, 0x00
        ];
        ReadOnlySpan<byte> repeatedArgument =
        [
            BeamEmissionFormat.IsPid, 0x95, 0x03,
            BeamEmissionFormat.Move, 0x03, 0x23,
            BeamEmissionFormat.Move, 0xC2, 0x13,
            BeamEmissionFormat.Line, 0x90,
            BeamEmissionFormat.TailExternalCallWithFrame, 0x30, 0x20, 0x00
        ];
        if (code.AsSpan().IndexOf(ordinaryCall) < 0
            || code.AsSpan().IndexOf(tailCall) < 0
            || code.AsSpan().IndexOf(repeatedArgument) < 0)
        {
            throw new InvalidOperationException(ArgumentMoveMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
