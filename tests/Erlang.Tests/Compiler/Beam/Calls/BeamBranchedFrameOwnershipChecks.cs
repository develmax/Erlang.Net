using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamBranchedFrameOwnershipChecks
{
    internal const string TestName = "compiler/beam/entry-values-after-frameless-branch-keep-owned-frame";
    private const string FrameOwnershipMismatch = "Branch frame compaction retained an unused save or changed a protected frame.";

    internal static Task Run()
    {
        const string source = """
            -module(frame_branch_owner).
            -export([render/2, cast/1, retained/2, protected/2]).
            render([], _) ->
                "~p";
            render(Tokens, Modifiers) ->
                case io_lib:printable_list(Tokens) of
                    true -> [$~|Modifiers ++ "s"];
                    false -> [$~|Modifiers ++ "p"]
                end.
            cast(Text) when is_list(Text) ->
                Text;
            cast(Symbol) when is_atom(Symbol) ->
                atom_to_list(Symbol);
            cast(Term) ->
                lists:flatten(io_lib:format("~tp",[Term])).
            retained([], Keep) ->
                Keep;
            retained(Value, Keep) ->
                case probe:classify(Value) of
                    true -> {Value, Keep};
                    false -> Keep
                end.
            protected([], Keep) ->
                Keep;
            protected(Value, Keep) ->
                try probe:classify(Value) of
                    true -> Keep;
                    false -> rejected
                catch
                    error:_ -> Keep
                end.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "frame_branch_owner.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> oneLiveSave =
        [
            BeamEmissionFormat.Label, 0x30, BeamEmissionFormat.Allocate, 0x10, 0x20,
            BeamEmissionFormat.Move, 0x13, 0x04, BeamEmissionFormat.Line, 0x20,
            BeamEmissionFormat.ExternalCall, 0x10, 0x00
        ];
        ReadOnlySpan<byte> noLiveSaves =
        [
            BeamEmissionFormat.Label, 0xA0, BeamEmissionFormat.AllocateHeap, 0x00, 0x20, 0x10,
            BeamEmissionFormat.PutList, 0x03, 0x02, 0x13
        ];
        ReadOnlySpan<byte> bothLiveSaves =
        [
            BeamEmissionFormat.Label, 0xD0, BeamEmissionFormat.Allocate, 0x20, 0x20,
            BeamEmissionFormat.Move, 0x13, 0x04, BeamEmissionFormat.Move, 0x03, 0x14
        ];
        // Try initializes its own tag before the first collection point.
        ReadOnlySpan<byte> protectedSaves =
        [
            BeamEmissionFormat.Label, 0x08, 0x13, BeamEmissionFormat.Allocate, 0x40, 0x20,
            BeamEmissionFormat.InitYRegisters, BeamEmissionFormat.ExtendedListOperand, 0x10, 0x34,
            BeamEmissionFormat.Move, 0x03, 0x04, BeamEmissionFormat.Move, 0x13, 0x14,
            BeamEmissionFormat.TryEnter, 0x24
        ];
        if (code.AsSpan().IndexOf(oneLiveSave) < 0 || code.AsSpan().IndexOf(noLiveSaves) < 0
            || code.AsSpan().IndexOf(bothLiveSaves) < 0 || code.AsSpan().IndexOf(protectedSaves) < 0)
        {
            throw new InvalidOperationException(FrameOwnershipMismatch);
        }

        _ = BeamModule.Prepare(beam);

        return Task.CompletedTask;
    }
}
