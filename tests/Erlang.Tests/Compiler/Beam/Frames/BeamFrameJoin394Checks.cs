using static Erlang.Tests.LibrarySelectionHostData;
using static Erlang.Tests.BeamFrameJoin394Names;

namespace Erlang.Tests;

internal static class BeamFrameJoin394Checks
{
    internal static Task Admission()
    {
        Require(BeamModule.Prepare(BeamFrameJoin394Bytes.Join()).Supports(BeamOpcodeEdgeNames.Run, 1));
        Reject(BeamFrameJoin394Bytes.Join(uninitialized: true));
        Reject(BeamFrameJoin394Bytes.Join(invalidY: true));
        Reject(BeamFrameJoin394Bytes.Join(returning: true));
        Require(BeamModule.Prepare(BeamFrameJoin394Bytes.Join(exceptional: true)).Supports(BeamOpcodeEdgeNames.Run, 1));
        Reject(BeamFrameJoin394Bytes.Join(uninitialized: true, exceptional: true));
        Reject(BeamFrameJoin394Bytes.Join(invalidY: true, exceptional: true));
        Reject(BeamFrameJoin394Bytes.Join(returning: true, exceptional: true));
        Require(BeamModule.Prepare(BeamFrameJoin394Bytes.ProtectedArm()).Supports(BeamOpcodeEdgeNames.Run, 1));
        Reject(BeamFrameJoin394Bytes.ProtectedJoin(protectedFirst: true));
        Reject(BeamFrameJoin394Bytes.ProtectedJoin(protectedFirst: false));
        Reject(BeamFrameJoin394Bytes.Last(BeamOpcodeEdgeNames.Length, 3));
        Require(BeamModule.Prepare(BeamFrameJoin394Bytes.Last(Error, 3)).Supports(BeamOpcodeEdgeNames.Run, 1));

        return Task.CompletedTask;
    }

    internal static async Task ActualBodyOverride()
    {
        foreach (bool markAsNative in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            int invoked = 0;
            runtime.Modules.Apply(registry =>
            {
                registry.DeleteModule(RuntimeModule);

                void Override(ModuleRegistry target) => target.Register(
                    RuntimeModule,
                    Error,
                    1,
                    (_, _) =>
                {
                    Interlocked.Increment(ref invoked);

                    return ValueTask.FromResult<Term>(Term.I(23));
                }
                );
                if (markAsNative)
                    registry.InstallPreloaded(Override);
                else
                    Override(registry);
            });
            BeamModule valid = BeamModule.Prepare(BeamFrameJoin394Bytes.Last(Error, 1));
            Require((await actor.InvokeAsync(context => valid.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(4)).AsTask())).Equals(Term.I(23)));
            Require(invoked == 1);
            BeamModule garbage = BeamModule.Prepare(BeamFrameJoin394Bytes.Last(Error, 3));
            bool refused = false;
            try
            {
                _ = await actor.InvokeAsync(context => garbage.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(5)).AsTask());
            }
            catch (InvalidDataException)
            {
                refused = true;
            }
            Require(refused && invoked == 1);
        }
    }

    private static void Reject(byte[] image)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, 1);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }
}
