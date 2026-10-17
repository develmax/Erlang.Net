namespace Erlang.NifNativeBackend;

public static class NifNativeTypeHostEntry
{
    public static Task Classification() => NifNativeTypeHostChecks.Run(NifNativeTypeHostChecks.Classification);

    public static Task EnvironmentIndependence() => NifNativeTypeHostChecks.Run(NifNativeTypeHostChecks.EnvironmentIndependence);

    public static Task ListCells() => NifNativeTypeHostChecks.Run(NifNativeTypeHostChecks.ListCells);

    public static Task Containment() => NifNativeTypeHostChecks.Run(NifNativeTypeHostChecks.Containment);
}
