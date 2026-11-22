using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamLocalConstantArgumentChecks
{
    internal const string RelayTestName = "compiler/beam/private-atom-tail-relay-keeps-register-order";
    internal const string TestName = "compiler/beam/private-atom-selector-moves-only-when-needed";
    private const string ConstantArgumentMismatch = "A proven local atom argument occupied a stack slot across a call.";
    private const string OpenArgumentMismatch = "Conflicting or escaped call arguments lost their stack slots.";
    private const string EarlyOnlyArgumentMismatch = "A key read before the only call was materialized again in its original register.";
    private const string NilContinuationMismatch = "Only a proven private empty-list argument may disappear from the selector continuation frame.";
    private const string SelectorArgumentMismatch = "A private atom argument changed its selector read or its post-call stack lifetime.";

    internal static Task RunRelay()
    {
        const string source = "private_atom_relays.erl";
        const string reference = "private_atom_relays.beam";
        byte[] candidate = CodeGeneration.CompileBeamModule(
            Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(source)),
            source
        );
        if (!candidate.AsSpan().SequenceEqual(BeamModuleEmissionChecks.Read(reference)))
        {
            throw new InvalidOperationException(BeamPrivateAtomRelayDiagnostics.ExpectedOtpEncoding);
        }

        _ = BeamModule.Prepare(candidate);

        return Task.CompletedTask;
    }

    internal static Task Run()
    {
        const string source = """
            -module(beam_local_atom_arguments).
            -export([run/1, left/1, right/1, direct/1, captured/0, lookup/1]).
            run(Value) -> fixed(marker, Value).
            left(Value) -> varying(marker, Value).
            right(Value) -> varying(other, Value).
            direct(Value) -> escaped(marker, Value).
            captured() -> fun escaped/2.
            fixed(Key, Value) -> finish(Key, Value, erlang:make_ref()).
            varying(Key, Value) -> finish(Key, Value, erlang:make_ref()).
            escaped(Key, Value) -> finish(Key, Value, erlang:make_ref()).
            finish(Key, Value, Ref) -> {Key, Value, is_reference(Ref)}.
            lookup(Candidates) -> first_only(marker, Candidates).
            first_only(Key, Candidates) ->
                {value, Result} = lists:keysearch(Key, 1, Candidates),
                {ok, Result}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_local_atom_arguments.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> constantFrame = [BeamEmissionFormat.Allocate, 0x10, 0x20, BeamEmissionFormat.Move, 0x13, 0x04];
        if (code.AsSpan().IndexOf(constantFrame) < 0)
        {
            throw new InvalidOperationException(ConstantArgumentMismatch);
        }

        ReadOnlySpan<byte> openFrame = [BeamEmissionFormat.Allocate, 0x20, 0x20, BeamEmissionFormat.Move, 0x13, 0x04];
        int firstOpenFrame = code.AsSpan().IndexOf(openFrame);
        if (firstOpenFrame < 0 || code.AsSpan(firstOpenFrame + openFrame.Length).IndexOf(openFrame) < 0)
        {
            throw new InvalidOperationException(OpenArgumentMismatch);
        }

        ReadOnlySpan<byte> firstCallArguments =
        [
            BeamEmissionFormat.Allocate, 0x00, 0x20,
            BeamEmissionFormat.Move, 0x13, 0x23,
            BeamEmissionFormat.Move, 0x11, 0x13,
            BeamEmissionFormat.Line
        ];
        if (code.AsSpan().IndexOf(firstCallArguments) < 0)
        {
            throw new InvalidOperationException(EarlyOnlyArgumentMismatch);
        }

        _ = BeamModule.Prepare(beam);
        CheckCaseSelector();
        CheckMovedSelector();
        CheckNilContinuation();

        return Task.CompletedTask;
    }

    private static void CheckNilContinuation()
    {
        const string fixture = "beam_local_nil_selector.erl";
        const string source = """
            -module(beam_local_nil_selector).
            -export([run/2]).
            run(A, B) -> check(A, B, []).
            check(A, B, Tag) ->
                case selector_dep:classify(A, B, Tag) of
                    {error, Reason} -> exit({Tag, Reason});
                    _ -> ok
                end.
            """;
        string explicitNil = source.Replace("exit({Tag, Reason})", "exit({[], Reason})", StringComparison.Ordinal);
        string open = source.Replace("-export([run/2]).", "-export([run/2, check/3]).", StringComparison.Ordinal);
        string openNil = open.Replace("exit({Tag, Reason})", "exit({[], Reason})", StringComparison.Ordinal);
        byte[] closed = CodeGeneration.CompileBeamModule(source, fixture);
        byte[] first = CodeGeneration.CompileBeamModule(open, fixture);
        byte[] second = CodeGeneration.CompileBeamModule(openNil, fixture);
        if (!closed.SequenceEqual(CodeGeneration.CompileBeamModule(explicitNil, fixture)) || first.SequenceEqual(second))
        {
            throw new InvalidOperationException(NilContinuationMismatch);
        }

        _ = BeamModule.Prepare(closed);
        _ = BeamModule.Prepare(first);
    }

    private static void CheckMovedSelector()
    {
        const string file = "beam_local_atom_moved.erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        byte[] beam = CodeGeneration.CompileBeamModule(source, file);
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> selector =
        [
            BeamEmissionFormat.Move, 0x13, 0x03,
            BeamEmissionFormat.Move, 0x32, 0x13,
            BeamEmissionFormat.Line, 0x30,
            BeamEmissionFormat.ExternalCall, 0x20, 0x00
        ];
        if (code.AsSpan().IndexOf(selector) < 0)
        {
            throw new InvalidOperationException(SelectorArgumentMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }

    private static void CheckCaseSelector()
    {
        const string source = """
            -module(beam_local_atom_selector).
            -export([run/2, open/3]).
            run(Target, Value) -> pick(Target, marker, Value).
            pick(Target, Key, Value) ->
                case erlang:function_exported(Target, Key, 1) of
                    false -> selector_dep:keep({missing, Target, Key, Value});
                    true -> Target:Key(Value)
                end.
            open(Target, Key, Value) ->
                case erlang:function_exported(Target, Key, 1) of
                    false -> selector_dep:keep({missing, Target, Key, Value});
                    true -> Target:Key(Value)
                end.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "beam_local_atom_selector.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> closedSelector =
        [
            BeamEmissionFormat.Allocate, 0x20, 0x30,
            BeamEmissionFormat.Move, 0x23, 0x04,
            BeamEmissionFormat.Move, 0x03, 0x14,
            BeamEmissionFormat.Move, 0x11, 0x23,
            BeamEmissionFormat.Line
        ];
        ReadOnlySpan<byte> openSelector =
        [
            BeamEmissionFormat.Allocate, 0x30, 0x30,
            BeamEmissionFormat.Move, 0x23, 0x04,
            BeamEmissionFormat.Move, 0x13, 0x14,
            BeamEmissionFormat.Move, 0x03, 0x24,
            BeamEmissionFormat.Move, 0x11, 0x23,
            BeamEmissionFormat.Line
        ];
        if (code.AsSpan().IndexOf(closedSelector) < 0 || code.AsSpan().IndexOf(openSelector) < 0)
        {
            throw new InvalidOperationException(SelectorArgumentMismatch);
        }

        _ = BeamModule.Prepare(beam);
    }
}
