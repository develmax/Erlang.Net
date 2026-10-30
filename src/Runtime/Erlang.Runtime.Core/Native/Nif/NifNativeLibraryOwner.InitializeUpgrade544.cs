namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    private IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> InitializeUpgrade544(
        NifNativeCallEnvironment environment,
        NifNativeFailureState failure,
        Term loadInfo
    )
    {
        if (entry.Upgrade == 0 || UpgradeOperation544 is not { } operation)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.UpgradeUnsupported);
        nuint information = environment.Add(loadInfo);
        var bindings = new List<KeyValuePair<(string Name, int Arity), ErlangFunction>>(functions.Count);
        foreach (var pair in functions)
            bindings.Add(new(pair.Key, BindFunction521(pair.Key, pair.Value)));
        SealGuestBindings535(bindings);
        var frozen = bindings.AsReadOnly();
        operation.Prepare544(this, frozen, resourceLoad527!);
        using NifResourceImagePin524 callback = operation.Previous544.RetainCallback544(this, out var previous);
        nint original = previous.privateData;
        int result;
        try
        {
            result = InvokeUpgradeCallback544(previous, environment, information);
            if (result == 0)
            {
                initialized = true;
                operation.NativeSucceeded544 = true;
            }
        }
        finally
        {
            // OTP releases normal thread progress before resource publication.
            operation.ReleaseNormalBarrier544();
        }
        if (result != 0)
        {
            // Pinned OTP resumes normal progress before restoring this cell.
            // Keep real callback custody until after the restoration.
            previous.privateData = original;
            operation.EndVeto544();
            failure.ThrowPending();
            throw new NifNativeUpgradeVetoException544(result);
        }
        try
        {
            failure.ThrowPending();
        }
        catch (Exception error)
        {
            // Native success may already have freed old storage. Publish first;
            // report a managed callback failure through the committed carrier.
            operation.Result544.NativePostCommitFailure544 = error;
        }

        return frozen;
    }
}
