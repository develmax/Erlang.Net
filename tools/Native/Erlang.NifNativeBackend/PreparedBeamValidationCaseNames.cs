namespace Erlang.NifNativeBackend;

public static class PreparedBeamValidationCaseNames
{
    public const string Isolation = "beam/prepared-validation-exact-owner-entry-clone-mutable-ir-and-input-buffer-isolation";
    internal const int Replays = 2;
    internal const string Failure = "Prepared BEAM validation identity/immutability regression";
}
