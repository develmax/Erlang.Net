namespace Erlang.Tests;

internal static class ConnectedDependencyHostNames
{
    internal const string ParseAll = "compiler/connected-dependency-parse-all";
    internal const string Host1 = "runtime/connected-codec-frames";
    internal const string Host2 = "runtime/connected-real-request-reply-death";
    internal const string Host3 = "runtime/connected-stale-reply-generation";
    internal const string Host4 = "runtime/connected-owner-death-before-reply";
    internal const string Host5 = "runtime/connected-abandon-before-reply";
    internal const string Host6 = "runtime/connected-owner-death-after-reply-before-selection";
    internal const string Host7 = "runtime/connected-producer-death-during-capacity-wait";
    internal const string Host8 = "runtime/connected-traced-success";
    internal const string Host9 = "runtime/connected-traced-creator-error";
    internal const string Host10 = "runtime/connected-priority-customtag-explicit-alias";
    internal const string Host11 = "runtime/connected-priority-customtag-demonitor-alias";
    internal const string Host12 = "runtime/connected-priority-customtag-reply-demonitor-alias";
    internal const string Host13 = "runtime/connected-real-demonitor-after-connected-success";
    internal const string Host14 = "runtime/connected-priority-linked-exit";
    internal const string Host15 = "runtime/connected-wire-order-last-tuple-precedence";
    internal const string Host16 = "runtime/connected-remote-alias-send2";
    internal const string Host17 = "runtime/connected-remote-alias-trace-send2";
    internal const string Host18 = "runtime/connected-remote-alias-send3-noconnect";
    internal const string Host19 = "runtime/connected-remote-alias-alternative-trace-send3";
    internal const string Host20 = "runtime/connected-rpc-local-caller-and-exceptions";
    internal const string Host21 = "runtime/connected-rpc-connected-label-lifecycle";
    internal const string Host22 = "runtime/connected-rpc-queued-down-before-zero-wait";
    internal const string Host23 = "runtime/connected-rpc-timeout-loses-to-admitted-down";
    internal const string Host24 = "runtime/connected-proc-lib-node-spawnfun";
    internal const string Host25 = "runtime/connected-proc-lib-node-spawnmfa";
    internal const string Host26 = "runtime/connected-proc-lib-node-spawnlinkfun";
    internal const string Host27 = "runtime/connected-proc-lib-node-spawnlinkmfa";
    internal const string Host28 = "runtime/connected-proc-lib-node-spawnoptfun";
    internal const string Host29 = "runtime/connected-proc-lib-node-spawnoptmfa";
    internal const string Host30 = "runtime/connected-nodes-real-connected-query";
    internal const string NativeAliases = "runtime/connected-native-three-monitor-alias-sequences";
    internal const string LegacyTracedSend = "runtime/connected-native-legacy-traced-send3-noconnect";
}
