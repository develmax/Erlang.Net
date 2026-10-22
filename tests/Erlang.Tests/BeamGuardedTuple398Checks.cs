using static Erlang.Tests.BeamGuardedTuple398Names;
using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamInfallibleBifControlNames;

namespace Erlang.Tests;

internal static class BeamGuardedTuple398Checks
{

    internal static byte[] Image(
        bool guard = true,
        bool overwrite = false,
        bool entryLabel = false,
        bool missingRoot = false
    )
        => BeamOpcodeEdgeBytes.Image(
            [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. (guard ? new byte[] { TupleGuard, F(4), X(0) } : []),
            .. (entryLabel ? new byte[] { Label, U(3) } : []),
            Allocate, U(missingRoot ? 3 : 2), U(1),
            .. (overwrite ? new byte[] { Move, A(7), X(0) } : []),
            Unary, F(0), U(0), X(0), Y(0),
            Catch, Y(1), F(5),
            .. (missingRoot ? new byte[] { TestHeap, U(0), U(1) } : []),
            Move, Y(0), X(0), CatchEnd, Y(1), Deallocate, U(missingRoot ? 3 : 2), Return,
            Label, U(4), Move, A(7), X(0), Return,
            Label, U(5), CatchEnd, Y(1), Deallocate, U(missingRoot ? 3 : 2), Return
        ],
            labels: 6,
            importFunction: TupleSize
        );

    internal static async Task GuardedKernelAndRemoteOverride()
    {
        await using var runtime = new ProcessRuntime();
        runtime.Modules.DeleteModule(ErlangModuleNames.Module);
        int calls = 0;
        runtime.Modules.Register(
            ErlangModuleNames.Module,
            TupleSize,
            1,
            (_, _) =>
        {
            calls++;
            throw new ErlangException(Term.A(OverrideFailure));
        }
        );
        BeamModule opcode = BeamModule.Prepare(Image());
        BeamModule remote = BeamModule.Prepare(BeamInfallibleBifControlBytes.Remote(TupleSize, 1));
        Require(opcode.Supports(BeamOpcodeEdgeNames.Run, 1));
        Require(remote.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tuple = Term.Tuple(Term.I(1), Term.I(2));
            Require((await opcode.Invoke(context, BeamOpcodeEdgeNames.Run, tuple)).Equals(Term.I(2)) && calls == 0);
            bool failed = false;
            try
            {
                await remote.Invoke(context, BeamOpcodeEdgeNames.Run, tuple);
            }
            catch (ErlangException error)
            {
                failed = error.Reason.Equals(Term.A(OverrideFailure));
            }
            Require(failed && calls == 1);

            return true;
        });
    }

    internal static Task UnprovedMutationEntryAndRootsRefuse()
    {
        foreach (byte[] image in new[] { Image(guard: false), Image(overwrite: true), Image(entryLabel: true), Image(missingRoot: true) })
        {
            bool rejected = false;
            try
            {
                rejected = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                rejected = true;
            }
            Require(rejected);
        }

        return Task.CompletedTask;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
