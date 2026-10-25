using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace Erlang.Differential;

public static class DescriptorIoContracts
{
    private static Term Expected => Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion));

    public static async Task RunCancellation()
    {
        string path = Path.Combine(Path.GetTempPath(), DescriptorIoNames.TemporaryPrefix + Guid.NewGuid());
        try
        {
            using var pipe = new AnonymousPipeServerStream(PipeDirection.In);
            using var output = File.OpenHandle(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.ReadWrite
            );
            var fixture = new DescriptorIoCase(
                DescriptorIoNames.CancellationTest,
                DescriptorIoNames.CancellationBody,
                string.Empty,
                string.Empty,
                string.Empty
            );
            using var artifact = GeneratedModuleCompiler.Compile(Source(fixture, pipe.SafePipeHandle.DangerousGetHandle().ToInt64(), output.DangerousGetHandle().ToInt64()));
            Term result = await CompiledModuleExecution.Run(artifact).WaitAsync(TimeSpan.FromSeconds(DescriptorIoNames.TimeoutSeconds));
            if (!result.Equals(Expected))
                throw new InvalidOperationException(DescriptorIoNames.Failure);
            using var writeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(pipe.ClientSafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            using var feeder = new FileStream(writeHandle, FileAccess.Write);
            feeder.WriteByte(65);
            feeder.Flush();
            if (pipe.ReadByte() != 65)
                throw new InvalidOperationException(DescriptorIoNames.Failure);
            System.IO.RandomAccess.Write(output, new byte[] { 69 }, 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static async Task<Term> RunSdk(DescriptorIoCase fixture)
    {
        string directory = Path.Combine(Path.GetTempPath(), DescriptorIoNames.TemporaryPrefix + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, DescriptorIoNames.OutputFile);
        try
        {
            using var pipe = new AnonymousPipeServerStream(PipeDirection.In);
            using var writeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(pipe.ClientSafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            using (var feeder = new FileStream(writeHandle, FileAccess.Write))
                feeder.Write(Convert.FromHexString(fixture.InputHex));
            pipe.DisposeLocalCopyOfClientHandle();
            using var output = File.OpenHandle(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.ReadWrite
            );
            string source = Source(fixture, pipe.SafePipeHandle.DangerousGetHandle().ToInt64(), output.DangerousGetHandle().ToInt64());
            using var artifact = GeneratedModuleCompiler.Compile(source);
            Term result = await CompiledModuleExecution.Run(artifact).WaitAsync(TimeSpan.FromSeconds(DescriptorIoNames.TimeoutSeconds));
            if (!result.Equals(Expected) || Convert.ToHexString(ReadOwned(output)) != fixture.OutputHex)
                throw new InvalidOperationException(DescriptorIoNames.Failure + ": " + fixture.Name + " " + result.ToString());
            // Closing the port leaves both borrowed OS descriptors usable.
            if (pipe.SafePipeHandle.IsClosed || output.IsClosed)
                throw new InvalidOperationException(DescriptorIoNames.Failure);
            System.IO.RandomAccess.Write(output, new byte[] { 69 }, new FileInfo(path).Length);

            return result;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static byte[] ReadOwned(Microsoft.Win32.SafeHandles.SafeFileHandle handle)
    {
        byte[] bytes = new byte[checked((int)System.IO.RandomAccess.GetLength(handle))];
        int count = System.IO.RandomAccess.Read(handle, bytes, 0);
        if (count != bytes.Length)
            throw new InvalidOperationException(DescriptorIoNames.Failure);

        return bytes;
    }

    public static string Source(DescriptorIoCase fixture, long input, long output) =>
        DescriptorIoNames.ModulePrefix + fixture.Body.Replace(DescriptorIoNames.InputToken, input.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace(DescriptorIoNames.OutputToken, output.ToString(System.Globalization.CultureInfo.InvariantCulture)) + DescriptorIoNames.ModuleSuffix;

    public static async Task<int> RunReference(string oracle, string report)
    {
        var results = new List<object>();
        int failed = 0;
        foreach (var fixture in DescriptorIoCases.All)
        {
            string directory = Path.Combine(Path.GetTempPath(), DescriptorIoNames.TemporaryPrefix + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                string input = Path.Combine(directory, DescriptorIoNames.InputFile);
                string output = Path.Combine(directory, DescriptorIoNames.OutputFile);
                await File.WriteAllBytesAsync(input, Convert.FromHexString(fixture.InputHex));
                string source = Source(fixture, DescriptorIoNames.ReferenceInput, DescriptorIoNames.ReferenceOutput);
                var start = new ProcessStartInfo(DescriptorIoNames.Shell) { RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (string argument in new[] { DescriptorIoNames.ShellOption, DescriptorIoNames.ShellScript, DescriptorIoNames.ShellLabel, oracle, input, output, CompiledModuleProtocol.Command(source) })
                    start.ArgumentList.Add(argument);
                using var child = Process.Start(start)!;
                var stdout = child.StandardOutput.ReadToEndAsync();
                var stderr = child.StandardError.ReadToEndAsync();
                try
                {
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(DescriptorIoNames.TimeoutSeconds));
                }
                catch
                {
                    child.Kill(true);
                    await child.WaitForExitAsync();
                    throw;
                }
                string raw = await stdout;
                string errors = await stderr;
                Term native = OracleResultProtocol.Decode(raw);
                Term sdk = await RunSdk(fixture);
                string actualBytes = Convert.ToHexString(await File.ReadAllBytesAsync(output));
                bool passed = child.ExitCode == 0 && native.Equals(Expected) && sdk.Equals(Expected) && actualBytes == fixture.OutputHex;
                if (!passed)
                    failed++;
                results.Add(new { fixture.Name, fixture.Body, fixture.InputHex, fixture.OutputHex, ReferenceSource = source, Native = native.ToString(), Sdk = sdk.ToString(), ActualOutputHex = actualBytes, StandardOutput = raw, StandardError = errors, child.ExitCode, Passed = passed });
            }
            catch (Exception exception)
            {
                failed++;
                results.Add(new { fixture.Name, fixture.Body, fixture.InputHex, fixture.OutputHex, Passed = false, InfrastructureError = exception.ToString() });
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        await File.WriteAllTextAsync(
            report,
            JsonSerializer.Serialize(new { Passed = results.Count - failed, Failed = failed, Results = results }, new JsonSerializerOptions { WriteIndented = true })
        );

        return failed == 0 ? 0 : 1;
    }
}
