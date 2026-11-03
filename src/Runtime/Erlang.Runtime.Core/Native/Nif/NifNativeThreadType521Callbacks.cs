using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeThreadType521Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Read()
    {
        if (DirtyNativeThread419.Owner is not null)
            return DirtyNativeThread419.Kind switch
            {
                DirtyNativeKind419.Cpu => NifNativeScheduling521Names.CpuThread,
                DirtyNativeKind419.Io => NifNativeScheduling521Names.IoThread,
                _ => NifNativeScheduling521Names.UndefinedThread
            };

        return RuntimeActorExecutionTurn.LaneIdentity != 0 || RuntimeNormalAuxiliaryTurn536.Lane536 != 0
            ? NifNativeScheduling521Names.NormalThread
            : NifNativeScheduling521Names.UndefinedThread;
    }
}
