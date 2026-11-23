using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamProjectedFrameInitializationChecks
{
    internal const string TestName = "compiler/beam/projects-an-entry-list-across-a-repeated-frame-clear";
    private const string Incorrect = "A repeated clear of an unchanged initialized slot must not interrupt list projection.";
    private const string ChangedValue = "A new clear or one that overwrites a saved argument must stay in the original setup.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("repeated_projection_initialization");
        byte[] entry =
        [
            .. Start(
                1,
                2,
                1,
                3
            ), IsNonemptyList, F(3), X(0)
        ];
        byte[] initial = [InitYRegisters, ExtendedListOperand, U(3), Y(0), Y(1), Y(3)];
        byte[] captures = [Move, X(2), Y(3), Move, X(1), Y(4)];
        byte[] setup = [GetList, X(0), X(3), X(4), Allocate, U(5), U(5), .. initial, .. captures];
        byte[] arguments = [Move, X(4), Y(2), Move, A(0), X(1), Move, X(3), X(0)];
        byte[] finish = [ExternalCall, U(2), U(0), Deallocate, U(5), Return, Label, U(3), Return, EndCode];
        byte[] repeated = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] source = [.. entry, .. setup, .. repeated, .. arguments, .. finish];
        byte[] expected =
        [
            .. entry, Allocate, U(5), U(3), .. initial, .. captures,
            GetList, X(0), X(0), Y(2), Move, A(0), X(1), .. finish
        ];
        if (Apply(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Incorrect);
        }

        byte[] newlyCleared = [InitYRegisters, ExtendedListOperand, U(1), Y(2)];
        byte[] savedValueCleared = [InitYRegisters, ExtendedListOperand, U(1), Y(3)];
        if (Apply([.. entry, .. setup, .. newlyCleared, .. arguments, .. finish]) is not null
            || Apply([.. entry, .. setup, .. savedValueCleared, .. arguments, .. finish]) is not null)
        {
            throw new InvalidOperationException(ChangedValue);
        }

        return Task.CompletedTask;

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ProjectEntryListDirectlyIntoFrame",
            (ReadOnlyMemory<byte>)bytes,
            4,
            true
        );
    }
}
