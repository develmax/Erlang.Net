using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal sealed class LinuxPortChildProcess : PortChildProcess
{
    private readonly Task exit;
    private int exitCode;
    public override int Id { get; }
    public override int ExitCode => exitCode;
    public override Stream Input { get; }
    public override Stream Output { get; }

    private LinuxPortChildProcess(int pid, Stream input, Stream output)
    {
        Id = pid;
        Input = input;
        Output = output;
        exit = Reap();
    }

    public static PortChildProcess StartLinux(PortOpenOptions options)
    {
        if (IntPtr.Size != sizeof(long))
            throw new ErlangException(PortNames.NotSupported);
        (SafeFileHandle childInput, SafeFileHandle parentInput) = Pipe();
        using (childInput)
        {
            SafeFileHandle? childOutput = null, parentOutput = null;
            nint actions = 0;
            bool initialized = false, transferred = false;
            try
            {
                (parentOutput, childOutput) = Pipe();
                actions = Marshal.AllocHGlobal(LinuxPortParameters.FileActionsBytes);
                Check(LinuxPortNative.posix_spawn_file_actions_init(actions));
                initialized = true;
                int input = options.UseStdio ? LinuxPortParameters.StandardInput : LinuxPortParameters.AuxiliaryInput;
                int output = options.UseStdio ? LinuxPortParameters.StandardOutput : LinuxPortParameters.AuxiliaryOutput;
                if (!string.IsNullOrEmpty(options.Start.WorkingDirectory))
                    Check(LinuxPortNative.posix_spawn_file_actions_addchdir_np(actions, options.Start.WorkingDirectory));
                Check(LinuxPortNative.posix_spawn_file_actions_adddup2(actions, childInput.DangerousGetHandle().ToInt32(), input));
                Check(LinuxPortNative.posix_spawn_file_actions_adddup2(actions, childOutput.DangerousGetHandle().ToInt32(), output));
                if (options.MergeStandardError)
                    Check(LinuxPortNative.posix_spawn_file_actions_adddup2(actions, output, LinuxPortParameters.StandardError));
                Check(LinuxPortNative.posix_spawn_file_actions_addclosefrom_np(actions, options.UseStdio ? LinuxPortParameters.AuxiliaryInput : LinuxPortParameters.MinimumPipeDescriptor));
                using var arguments = new NativePortStringVector(new[] { options.ArgumentZero ?? options.Start.FileName }.Concat(options.Start.ArgumentList));
                using var environment = new NativePortStringVector(options.Start.Environment.Where(pair => pair.Value is not null).Select(pair => pair.Key + NativePortNames.EnvironmentSeparator + pair.Value));
                NonBlocking(parentInput);
                NonBlocking(parentOutput);
                Check(LinuxPortNative.posix_spawnp(
                    out int pid,
                    options.Start.FileName,
                    actions,
                    0,
                    arguments.Pointer,
                    environment.Pointer
                ));
                if (!options.Write)
                    parentInput.Dispose();
                if (!options.Read)
                    parentOutput.Dispose();
                var child = new LinuxPortChildProcess(pid, new LinuxPortPipeStream(parentInput, false), new LinuxPortPipeStream(parentOutput, true));
                transferred = true;

                return child;
            }
            finally
            {
                childOutput?.Dispose();
                if (initialized)
                    LinuxPortNative.posix_spawn_file_actions_destroy(actions);
                if (actions != 0)
                    Marshal.FreeHGlobal(actions);
                if (!transferred)
                {
                    parentInput.Dispose();
                    parentOutput?.Dispose();
                }
            }
        }
    }

    private static (SafeFileHandle Read, SafeFileHandle Write) Pipe()
    {
        int[] descriptors = new int[2];
        if (LinuxPortNative.pipe2(descriptors, LinuxPortParameters.CloseOnExec) != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        using var read = new SafeFileHandle(descriptors[0], true);
        using var write = new SafeFileHandle(descriptors[1], true);
        int readCopy = LinuxPortNative.fcntl(descriptors[0], LinuxPortParameters.DuplicateCloseOnExec, LinuxPortParameters.MinimumPipeDescriptor);
        if (readCopy < 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        var ownedRead = new SafeFileHandle(readCopy, true);
        int writeCopy = LinuxPortNative.fcntl(descriptors[1], LinuxPortParameters.DuplicateCloseOnExec, LinuxPortParameters.MinimumPipeDescriptor);
        if (writeCopy < 0)
        {
            int error = Marshal.GetLastPInvokeError();
            ownedRead.Dispose();
            throw new Win32Exception(error);
        }

        return (ownedRead, new SafeFileHandle(writeCopy, true));
    }

    private static void NonBlocking(SafeFileHandle handle)
    {
        int descriptor = handle.DangerousGetHandle().ToInt32();
        int flags = LinuxPortNative.fcntl(descriptor, LinuxPortParameters.GetFlags, 0);
        if (flags < 0 || LinuxPortNative.fcntl(descriptor, LinuxPortParameters.SetFlags, flags | LinuxPortParameters.NonBlocking) < 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    private static void Check(int error)
    {
        if (error != 0)
            throw new Win32Exception(error);
    }

    private async Task Reap()
    {
        while (true)
        {
            int result = LinuxPortNative.waitpid(Id, out int status, LinuxPortParameters.WaitNoHang);
            if (result == Id)
            {
                int signal = status & LinuxPortParameters.SignalMask;
                exitCode = signal == 0 ? status >> LinuxPortParameters.ExitStatusShift : LinuxPortParameters.SignaledExitBase + signal;

                return;
            }
            if (result < 0 && Marshal.GetLastPInvokeError() != LinuxPortParameters.Interrupted)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            await Task.Delay(LinuxPortParameters.ExitPollMilliseconds);
        }
    }

    public override Task WaitForExitAsync(CancellationToken cancellation) => exit.WaitAsync(cancellation);

    public override void Dispose()
    {
        Input.Dispose();
        Output.Dispose();
    }
}
