using System.Diagnostics;
using System.Text;

namespace Erlang.HaltProbe;

internal static class NativeHaltVersionProbe
{
    public static async Task<NativeHaltVersionResult> Run(string executable)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in new[] { "-noshell", "-noinput", "-eval", NativeHaltTrackNames.VersionCommand })
            start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException(executable);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return new(
                null,
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                null,
                false,
                exception.ToString()
            );
        }
        Task<byte[]> output = Read(process.StandardOutput.BaseStream);
        Task<byte[]> error = Read(process.StandardError.BaseStream);
        bool timedOut = false;
        string? failure = null;
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(HaltProcessParameters.ProcessTimeoutSeconds));
        }
        catch (TimeoutException exception)
        {
            timedOut = true;
            failure = exception.ToString();
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(true);
                }
                catch (InvalidOperationException) when (process.HasExited) { }
            }
            await process.WaitForExitAsync();
        }
        byte[] stdout = await output, stderr = await error;

        return new(
            process.ExitCode,
            Array.AsReadOnly(stdout),
            Array.AsReadOnly(stderr),
            Encoding.UTF8.GetString(stdout).Trim(),
            timedOut,
            failure
        );
    }

    private static async Task<byte[]> Read(Stream stream)
    {
        using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes);

        return bytes.ToArray();
    }
}
