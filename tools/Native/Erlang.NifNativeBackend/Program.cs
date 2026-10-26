using Erlang;
using Erlang.NifNativeBackend;

try
{
    if (args.Length != 0 && args[0] == NifNativeUnixRawPathNames.Mode)
    {
        await NifNativeUnixRawPathDriver.Run(args[1..]);

        return;
    }

    if (args.Length == 1 && args[0] == NifNativeLoadReasonCheckNames.Mode)
    {
        await NifNativeLoadReasonControlChecks.Run();
        Console.WriteLine(NifNativeLoadReasonCheckNames.Pass);

        return;
    }

    if (args.Length == 1 && args[0] == NifNativeFilenameControlNames.Mode)
    {
        NifNativeFilenameControls.Run();
        Console.WriteLine(NifNativeFilenameControlNames.Pass);

        return;
    }

    if (args.Length != 0 && args[0] == NifNativeElevenNames.OrdinaryHostMode)
    {
        await NifNativeElevenDriver.Run(args[1..], ordinaryHost: true);

        return;
    }

    if (args.Length != 0 && args[0] == NifNativeElevenNames.Mode)
    {
        await NifNativeElevenDriver.Run(args[1..]);

        return;
    }

    if (args.Length == 1 && args[0] == NifNativeDefaultRouteNames.Mode)
    {
        await NifNativeDefaultRouteChecks.Run();

        return;
    }

    if (args.Length != 0 && args[0] == NifNativePayloadQueryHarnessNames.Mode)
    {
        await NifNativePayloadQueryHarness.Run(args[1..]);

        return;
    }

    if (args.Length != 0 && args[0] == NifNativeReallocationHarnessNames.Mode)
    {
        await NifNativeReallocationChecks.Run(args[1..]);

        return;
    }

    if (args.Length != 0 && args[0] == MapFamily504CallbackNames.Mode)
    {
        await MapFamily504CallbackEntry.Run(args[1..]);

        return;
    }

    if (args.Length != 0 && args[0] == MapValue503CallbackNames.Mode)
    {
        await MapValue503CallbackEntry.Run(args[1..]);

        return;
    }

    if (args.Length != 0 && args[0] == MapCreation501CallbackNames.Mode)
    {
        await MapCreation501CallbackEntry.Run(args[1..]);

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
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
