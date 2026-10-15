using Erlang;
using Erlang.NifNativeBackend;

if (args.Length != 0 && args[0] == NifNativeReallocationHarnessNames.Mode)
{
    await NifNativeReallocationChecks.Run(args[1..]);

    return;
}

await NifNativeManagedChecks.Run();
Console.WriteLine(NifNativeHarnessNames.ManagedPass);
if (args.Length != 0)
{
    if (args.Length is not (2 or 4 or 6))
        throw new ArgumentException(NifNativeHarnessNames.Usage);
    var profile = new NifNativeCapabilityProfile(
        args[0],
        args[1],
        NifNativeNames.SourceSha256,
        args.Length >= 4 ? args[2] : null,
        args.Length >= 4 ? args[3] : null
    );
    await NifNativeOwnershipChecks.Run(profile);
    Console.WriteLine(NifNativeHarnessNames.NativePass);
    await NifNativeCompiledRouteChecks.Run(profile);
    if (args.Length == 6)
    {
        await NifNativeAbiChecks.Run(profile, args[4], args[5]);
        Console.WriteLine(NifNativeHarnessNames.AbiPass);
    }
}
