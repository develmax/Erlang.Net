using System.Globalization;

namespace Erlang;

internal sealed class NifNativeUpgradeVetoException544(int returnCode)
    : InvalidOperationException(NifNativeUpgrade544Names.Veto)
{
    internal int ReturnCode544 { get; } = returnCode;
    internal string Category544 => NifNativeDefaultLoaderNames.Upgrade;
    internal string Reason544 => NifNativeUpgrade544Names.VetoPrefix
        + ReturnCode544.ToString(CultureInfo.InvariantCulture) + NifNativeUpgrade544Names.VetoSuffix;
}
