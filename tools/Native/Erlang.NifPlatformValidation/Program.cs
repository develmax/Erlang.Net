using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

if (args is ["--image", var beam, var library, var imageOutput])
{
    try
    {
        await NifPlatformImageChecks.Run(beam, library, imageOutput);
        Console.WriteLine(JsonSerializer.Serialize(new { Passed = true, Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Runtime = RuntimeInformation.FrameworkDescription, OperatingSystem = RuntimeInformation.OSDescription, PointerBytes = IntPtr.Size, ProcessHost = NifNativePortableUnixApi.IsProcessHost }));
    }
    catch (Exception error)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { Passed = false, Error = error.ToString() }));
        Environment.ExitCode = 1;
    }

    return;
}

var results = new List<object>();
int failed = 0;
void Check(string name, Action test)
{
    try
    {
        test();
        results.Add(new { Name = name, Passed = true });
    }
    catch (Exception error)
    {
        failed++;
        results.Add(new { Name = name, Passed = false, Error = error.ToString() });
    }
}

Check(
    "platform-admission",
    () =>
{
    foreach (Architecture architecture in new[] { Architecture.X64, Architecture.Arm64 })
    {
        if (!NifNativePlatformPolicy.Supports(
            true,
            false,
            architecture,
            8
        )
            || !NifNativePlatformPolicy.Supports(
                false,
                true,
                architecture,
                8
            ))
            throw new InvalidOperationException("64-bit target rejected");
    }
    if (NifNativePlatformPolicy.Supports(
        true,
        false,
        Architecture.X86,
        4
    )
        || NifNativePlatformPolicy.Supports(
            false,
            true,
            Architecture.Arm,
            4
        )
        || NifNativePlatformPolicy.Supports(
            false,
            false,
            Architecture.X64,
            8
        ))
        throw new InvalidOperationException("Unsupported ABI admitted");
}
);

Check(
    "entry-layout",
    () =>
{
    if (Marshal.SizeOf<NifNativeAbiEntry>() != NifNativeAbi.EntryBytes
        || Marshal.SizeOf<NifNativeAbiFunction>() != NifNativeAbi.FunctionBytes
        || Marshal.SizeOf<NifNativeBinaryDescriptor>() != 40
        || Marshal.SizeOf<NifNativeBridgeTable>() != NifNativeAbi.BridgeTableBytes)
        throw new InvalidOperationException("Host managed ABI differs from declared native layout");
}
);

Check(
    "elf-capsule-machine",
    () =>
{
    foreach (Architecture architecture in new[] { Architecture.X64, Architecture.Arm64 })
    {
        byte[] capsule = NifNativeElfCapsule.Create(architecture, "/bridge.so"u8.ToArray(), "/nif.so"u8.ToArray());
        ushort expected = architecture == Architecture.Arm64
            ? NifNativeCapsuleLayout.Arm64Machine : NifNativeCapsuleLayout.X64Machine;
        if (BinaryPrimitives.ReadUInt16LittleEndian(capsule.AsSpan(18, 2)) != expected
            || BinaryPrimitives.ReadUInt64LittleEndian(capsule.AsSpan(112, 8)) != (ulong)Environment.SystemPageSize)
            throw new InvalidOperationException("ELF capsule architecture/page alignment differs from the host profile");
    }
    try
    {
        NifNativeElfCapsule.Create(Architecture.Arm, "/bridge.so"u8.ToArray(), "/nif.so"u8.ToArray());
        throw new InvalidOperationException("Unsupported ELF architecture was accepted");
    }
    catch (PlatformNotSupportedException)
    {
    }
}
);

Check(
    "host-loader-missing-file",
    () =>
{
    try
    {
        NifNativePlatformLoader.OpenNifLibrary(null, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".so"), false);
        throw new InvalidOperationException("Missing library was admitted");
    }
    catch (NifNativeLoadStageException)
    {
    }
}
);

if (OperatingSystem.IsWindows() && args.Length > 0)
{
    string adapter = Path.GetFullPath(args[0]);
    Check("windows-native-variadic-adapter", () => WindowsAdapterChecks.Run(adapter));
    Check("windows-runtime-native-slot", WindowsAdapterChecks.VerifyRuntimeBinding);
}
else if (!OperatingSystem.IsWindows() && args.Length > 0)
{
    Check("unix-native-callbacks-and-vm-ownership", () => UnixBridgeChecks.Run(args[0]).GetAwaiter().GetResult());
}

string report = JsonSerializer.Serialize(
    new
    {
        Host = RuntimeInformation.RuntimeIdentifier,
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        LinuxNamespace = NifNativePlatformPolicy.UseLinuxNamespace,
        CrossPlatformExecutionProven = false,
        Runtime = RuntimeInformation.FrameworkDescription,
        OperatingSystem = RuntimeInformation.OSDescription,
        PointerBytes = IntPtr.Size,
        Results = results
    },
    new JsonSerializerOptions { WriteIndented = true }
);
Console.WriteLine(report);
Environment.ExitCode = failed == 0 ? 0 : 1;
