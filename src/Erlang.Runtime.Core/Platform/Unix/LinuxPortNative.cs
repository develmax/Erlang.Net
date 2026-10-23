using System.Runtime.InteropServices;

namespace Erlang;

internal static class LinuxPortNative
{
    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    public static extern int pipe2([Out] int[] descriptors, int flags);

    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    public static extern int fcntl(int descriptor, int command, int value);

    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    public static extern nint read(int descriptor, nint buffer, nuint count);

    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    public static extern nint write(int descriptor, nint buffer, nuint count);

    [DllImport(LinuxPortParameters.Library, SetLastError = true)]
    public static extern int waitpid(int pid, out int status, int options);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawn_file_actions_init(nint actions);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawn_file_actions_destroy(nint actions);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawn_file_actions_adddup2(nint actions, int descriptor, int destination);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawn_file_actions_addclose(nint actions, int descriptor);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawn_file_actions_addclosefrom_np(nint actions, int from);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawn_file_actions_addchdir_np(nint actions, [MarshalAs(UnmanagedType.LPUTF8Str)] string directory);

    [DllImport(LinuxPortParameters.Library)]
    public static extern int posix_spawnp(
        out int pid,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string executable,
        nint actions,
        nint attributes,
        nint arguments,
        nint environment
    );
}
