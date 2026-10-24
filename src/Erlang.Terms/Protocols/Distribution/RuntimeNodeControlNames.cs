namespace Erlang;

public static class RuntimeNodeControlNames
{
    public const string Monitor = "monitor_node";
    public const string MonitorTrap = "dmonitor_node";
    public const string AutoConnect = "auto_connect";
    public const string Disconnect = "disconnect_node";
    public const string Kernel = "net_kernel";
    public const string DisconnectRequest = "disconnect";
    public const string PassiveRequest = "passive_cnct";
    public const string AllowPassive = "allow_passive_connect";
    public const string NodeDown = "nodedown";
    public const string NotAlive = "notalive";
    public const string Ignored = "ignored";
    public const string BadOption = "badopt";
    public const string Cause = "cause";
    public const string ErrorModule = "module";
    public const string ErrorFormatter = "erl_erts_errors";
    public const string Server = "gen_server";
    public const string Call = "call";
    public const string Infinity = "infinity";
}
