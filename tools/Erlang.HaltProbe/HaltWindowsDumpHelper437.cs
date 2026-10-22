using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace Erlang.HaltProbe;

internal static class HaltWindowsDumpHelper437
{
    internal static int Run(string[] arguments)
    {
        try
        {
            if (!OperatingSystem.IsWindows() || arguments.Length != HaltWindowsDumpNames437.ArgumentCount
                || !uint.TryParse(
                    arguments[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out uint parentId
                )
                || parentId == 0 || parentId > int.MaxValue
                || !long.TryParse(
                    arguments[2],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long creationTicks
                )
                || !Path.IsPathFullyQualified(arguments[3]))
                throw new ArgumentException(HaltWindowsDumpNames437.ParentRefused);
            uint creator = CreatingParent();
            if (parentId != creator)
                throw new InvalidOperationException(HaltWindowsDumpNames437.ParentRefused);
            // The parent is blocked waiting for this child. Its real OS identity
            // must remain live; the command never opens an arbitrary supplied PID.
            using var target = Process.GetProcessById(checked((int)parentId));
            using SafeProcessHandle handle = OpenProcess(
                HaltWindowsDumpNames437.QueryInformation | HaltWindowsDumpNames437.VmRead, false, parentId);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastPInvokeError());
            if (target.HasExited || target.StartTime.ToUniversalTime().Ticks != creationTicks)
                throw new InvalidOperationException(HaltWindowsDumpNames437.ParentRefused);
            using (var file = new FileStream(
                arguments[3],
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            ))
            {
                if (!MiniDumpWriteDump(
                    handle,
                    parentId,
                    file.SafeFileHandle,
                    HaltProbeNames.NormalDump,
                    0,
                    0,
                    0
                ))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                file.Flush(true);
            }
            HaltWindowsDumpIdentity437.Verify(arguments[3], parentId);
            File.WriteAllText(
                arguments[3] + HaltWindowsDumpNames437.IdentitySuffix,
                JsonSerializer.Serialize(new
                {
                    ParentPid = parentId,
                    OsCreatingParentPid = creator,
                    ParentCreationUtcTicks = creationTicks,
                    HelperPid = Environment.ProcessId,
                    ActualMiscInfoProcessIdVerified = true
                })
            );

            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);

            return HaltProbeNames.InfrastructureExit;
        }
    }

    private static uint CreatingParent()
    {
        using SafeFileHandle snapshot = CreateToolhelp32Snapshot(HaltWindowsDumpNames437.ProcessSnapshot, 0);
        if (snapshot.IsInvalid) throw new Win32Exception(Marshal.GetLastPInvokeError());
        var entry = new HaltWindowsDumpProcessEntry437
        {
            Size = checked((uint)Marshal.SizeOf<HaltWindowsDumpProcessEntry437>()),
            Executable = string.Empty
        };
        if (!Process32First(snapshot, ref entry)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        do
        {
            if (entry.ProcessId == checked((uint)Environment.ProcessId)) return entry.ParentProcessId;
        } while (Process32Next(snapshot, ref entry));
        throw new InvalidOperationException(HaltWindowsDumpNames437.ParentRefused);
    }

    [DllImport(HaltWindowsDumpNames437.Kernel, SetLastError = true)]
    private static extern SafeFileHandle CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport(HaltWindowsDumpNames437.Kernel, EntryPoint = "Process32FirstW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(SafeFileHandle snapshot, ref HaltWindowsDumpProcessEntry437 entry);

    [DllImport(HaltWindowsDumpNames437.Kernel, EntryPoint = "Process32NextW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(SafeFileHandle snapshot, ref HaltWindowsDumpProcessEntry437 entry);

    [DllImport(HaltWindowsDumpNames437.Kernel, SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint processId);

    [DllImport(HaltProbeNames.WindowsDumpLibrary, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MiniDumpWriteDump(
        SafeProcessHandle process,
        uint processId,
        SafeFileHandle file,
        int type,
        nint exceptions,
        nint userStreams,
        nint callbacks
    );
}
