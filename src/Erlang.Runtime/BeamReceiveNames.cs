namespace Erlang;

internal static class BeamReceiveNames
{
    public const string Infinity = "infinity";
    public const uint MaximumMilliseconds = uint.MaxValue;
    public const string UnsupportedMatcher = "Unsupported decoded BEAM receive matcher";
    public const string InvalidPlan = "Invalid decoded BEAM receive control flow";
    public const string InvalidFrame = "Unbalanced BEAM zero-slot continuation frame";
}
