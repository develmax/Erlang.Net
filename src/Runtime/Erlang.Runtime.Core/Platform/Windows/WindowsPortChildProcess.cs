using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Erlang;

internal sealed class WindowsPortChildProcess : PortChildProcess
{
    private readonly SafeProcessHandle process;
    private readonly Task exit;
    private int exitCode;
    public override int Id { get; }
    public override int ExitCode => exitCode;
    public override Stream Input { get; }
    public override Stream Output { get; }

    private WindowsPortChildProcess(
        int pid,
        SafeProcessHandle process,
        Stream input,
        Stream output
    )
    {
        Id = pid;
        this.process = process;
        Input = input;
        Output = output;
        exit = ObserveExit();
    }

    public static PortChildProcess StartWindows(PortOpenOptions options)
    {
        SafeFileHandle? childInput = null, childOutput = null, childError = null, parentInput = null, parentOutput = null;
        nint attributes = 0, handles = 0, environment = 0;
        bool initialized = false, transferred = false;
        SafeProcessHandle? process = null;
        try
        {
            if (options.Write)
                (childInput, parentInput) = Pipe(true, options.OverlappedIo);
            else
                childInput = Null(WindowsPortParameters.GenericRead);
            if (options.Read)
                (parentOutput, childOutput) = Pipe(false, options.OverlappedIo);
            else
                childOutput = Null(WindowsPortParameters.GenericWrite);
            if (options.MergeStandardError)
                childError = childOutput;
            else
            {
                nint stderr = WindowsPortNative.GetStdHandle(WindowsPortParameters.StandardError);
                if (stderr == 0 || stderr == -1)
                    childError = Null(WindowsPortParameters.GenericWrite);
                else if (!WindowsPortNative.DuplicateHandle(
                    WindowsPortNative.GetCurrentProcess(),
                    stderr,
                    WindowsPortNative.GetCurrentProcess(),
                    out childError,
                    0,
                    true,
                    WindowsPortParameters.DuplicateSameAccess
                ))
                    throw Error();
            }
            nint[] inherited = new[] { childInput.DangerousGetHandle(), childOutput.DangerousGetHandle(), childError.DangerousGetHandle() }.Distinct().ToArray();
            nuint bytes = 0;
            WindowsPortNative.InitializeProcThreadAttributeList(
                0,
                1,
                0,
                ref bytes
            );
            if (bytes == 0)
                throw Error();
            attributes = Marshal.AllocHGlobal(checked((nint)bytes));
            if (!WindowsPortNative.InitializeProcThreadAttributeList(
                attributes,
                1,
                0,
                ref bytes
            ))
                throw Error();
            initialized = true;
            handles = Marshal.AllocHGlobal(inherited.Length * IntPtr.Size);
            for (int index = 0; index < inherited.Length; index++)
                Marshal.WriteIntPtr(handles, index * IntPtr.Size, inherited[index]);
            if (!WindowsPortNative.UpdateProcThreadAttribute(
                attributes,
                0,
                WindowsPortParameters.HandleListAttribute,
                handles,
                (nuint)(inherited.Length * IntPtr.Size),
                0,
                0
            ))
                throw Error();
            var startup = new WindowsPortStartup
            {
                Size = Marshal.SizeOf<WindowsPortStartup>(),
                Flags = WindowsPortParameters.StartUseStandardHandles,
                Input = childInput.DangerousGetHandle(),
                Output = childOutput.DangerousGetHandle(),
                Error = childError.DangerousGetHandle(),
                Attributes = attributes
            };
            string entries = string.Join(
                NativePortNames.EnvironmentTerminator,
                options.NativeEnvironment is { } nativeEnvironment
                    ? nativeEnvironment.Select(pair => pair.Key + NativePortNames.EnvironmentSeparator + pair.Value)
                    : options.Start.Environment.Where(pair => pair.Value is not null).OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => pair.Key + NativePortNames.EnvironmentSeparator + pair.Value)
            ) + NativePortNames.EnvironmentTerminator;
            environment = Marshal.StringToHGlobalUni(entries);
            string executable = Resolve(options.Start.FileName, options.SearchExecutableInPath);
            options.Start.FileName = executable;
            uint flags = WindowsPortParameters.ExtendedStartup | WindowsPortParameters.UnicodeEnvironment;
            if (options.Start.CreateNoWindow)
                flags |= WindowsPortParameters.NoWindow;
            if (!WindowsPortNative.CreateProcessW(
                executable,
                WindowsPortCommandLine.Create(options),
                0,
                0,
                true,
                flags,
                environment,
                string.IsNullOrEmpty(options.Start.WorkingDirectory) ? null : options.Start.WorkingDirectory,
                ref startup,
                out WindowsPortProcessInformation information
            ))
                throw Error();
            using var thread = new SafeFileHandle(information.Thread, true);
            process = new SafeProcessHandle(information.Process, true);
            Stream input = parentInput is null ? Stream.Null : new FileStream(
                parentInput,
                FileAccess.Write,
                WindowsPortParameters.StreamBufferBytes,
                true
            );
            Stream output = parentOutput is null ? Stream.Null : new FileStream(
                parentOutput,
                FileAccess.Read,
                WindowsPortParameters.StreamBufferBytes,
                true
            );
            var child = new WindowsPortChildProcess(
                information.ProcessId,
                process,
                input,
                output
            );
            transferred = true;

            return child;
        }
        finally
        {
            childInput?.Dispose();
            childOutput?.Dispose();
            if (!ReferenceEquals(childError, childOutput))
                childError?.Dispose();
            if (initialized)
                WindowsPortNative.DeleteProcThreadAttributeList(attributes);
            if (attributes != 0)
                Marshal.FreeHGlobal(attributes);
            if (handles != 0)
                Marshal.FreeHGlobal(handles);
            if (environment != 0)
                Marshal.FreeHGlobal(environment);
            if (!transferred)
            {
                parentInput?.Dispose();
                parentOutput?.Dispose();
                process?.Dispose();
            }
        }
    }

    private static (SafeFileHandle Read, SafeFileHandle Write) Pipe(bool inheritRead, bool overlapped)
    {
        string name = WindowsPortParameters.PipePrefix + Guid.NewGuid().ToString();
        var security = new WindowsPortSecurity { Length = Marshal.SizeOf<WindowsPortSecurity>(), Inherit = inheritRead };
        var read = WindowsPortNative.CreateNamedPipeW(
            name,
            WindowsPortParameters.PipeInbound | (!inheritRead || overlapped ? WindowsPortParameters.Overlapped : 0),
            0,
            1,
            0,
            0,
            WindowsPortParameters.PipeTimeoutMilliseconds,
            ref security
        );
        if (read.IsInvalid)
        {
            Win32Exception error = Error();
            read.Dispose();
            throw error;
        }
        security.Inherit = !inheritRead;
        var write = WindowsPortNative.CreateFileW(
            name,
            WindowsPortParameters.GenericWrite,
            0,
            ref security,
            WindowsPortParameters.OpenExisting,
            WindowsPortParameters.NormalFile | (inheritRead || overlapped ? WindowsPortParameters.Overlapped : 0),
            0
        );
        if (write.IsInvalid)
        {
            Win32Exception error = Error();
            read.Dispose();
            write.Dispose();
            throw error;
        }

        return (read, write);
    }

    private static SafeFileHandle Null(uint access)
    {
        var security = new WindowsPortSecurity { Length = Marshal.SizeOf<WindowsPortSecurity>(), Inherit = true };
        var handle = WindowsPortNative.CreateFileW(
            WindowsPortParameters.NullDevice,
            access,
            0,
            ref security,
            WindowsPortParameters.OpenExisting,
            WindowsPortParameters.NormalFile,
            0
        );
        if (handle.IsInvalid)
        {
            Win32Exception error = Error();
            handle.Dispose();
            throw error;
        }

        return handle;
    }

    private static Win32Exception Error() => new(Marshal.GetLastPInvokeError());

    private static string Resolve(string executable, bool searchInPath)
    {
        foreach (string extension in WindowsPortParameters.SearchExtensions)
        {
            var path = new StringBuilder(WindowsPortParameters.PathBufferCharacters);
            uint count = WindowsPortNative.SearchPathW(
                searchInPath ? null : WindowsPortParameters.CurrentDirectorySearchPath,
                executable + extension,
                null,
                path.Capacity,
                path,
                0
            );
            if (count > 0 && count < path.Capacity && File.Exists(path.ToString()))
                return path.ToString();
        }

        throw new Win32Exception(PortParameters.FileNotFoundError);
    }

    private async Task ObserveExit()
    {
        while (!process.IsClosed)
        {
            uint result;
            try
            {
                result = WindowsPortNative.WaitForSingleObject(process, 0);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            if (result == WindowsPortParameters.WaitObject)
            {
                if (!WindowsPortNative.GetExitCodeProcess(process, out uint code))
                    throw Error();
                exitCode = unchecked((int)code);

                return;
            }
            if (result == WindowsPortParameters.WaitFailed)
                throw Error();
            await Task.Delay(WindowsPortParameters.ExitPollMilliseconds);
        }
    }

    public override Task WaitForExitAsync(CancellationToken cancellation) => exit.WaitAsync(cancellation);

    public override void Dispose()
    {
        Input.Dispose();
        Output.Dispose();
        process.Dispose();
    }
}
