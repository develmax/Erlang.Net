namespace Erlang.NifNativeBackend;

internal static class NativeOwned516Profile
{
    internal static bool Requested(string[] arguments)
    {
        if (arguments.Length == 4)
            return false;
        if (arguments.Length != 5 || arguments[4] != NativeOwned516ProfileNames.Mode)
            throw new InvalidOperationException(NativeOwned516ProfileNames.InvalidArguments);

        return true;
    }

    internal static ProcessRuntime Create(bool enabled)
        => new(new ProcessRuntimeOptions { OwnedTermsPilot506 = enabled });
}
