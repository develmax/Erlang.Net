namespace Erlang.Tests;

internal static class NifInventoryHostNames
{
    internal const string Test = "runtime/nif-installed-current-image-inventory-and-cache";
    internal const string Module = "nif_inventory_host";
    internal const string First = "zeta";
    internal const string Second = "alpha";
    internal const string Missing = "missing";
    internal const string Nifs = "nifs";
    internal const string Native = "native";
    internal const string NativeAddresses = "native_addresses";
    internal const string False = "false";
    internal const string Broken = "Installed NIF inventory contract failed";
    internal const string Resource = ".BeamOnLoad.fresh_success.new.beam_on_load_fresh_success.beam";
    internal const int Value = 313;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
