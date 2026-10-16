namespace Erlang.Tests;

internal static class RemoteSourceFunHostNames
{
    internal const string Module = "proc_lib_remote_payload";
    internal const string Maker = "maker";
    internal const string Parent = "parent";
    internal const string Marker = "marker";
    internal const string MarkerName = "remote_source_fun_constructor";
    internal const string Label = "label";
    internal const string Payload = "payload";
    internal const string SnapshotNode = "node";
    internal const string GroupLeader = "group_leader";
    internal const string Links = "links";
    internal const string LexicalModule = "source_fun_lexical_contract";
    internal const string MakeShadow = "make_shadow";
    internal const string MakeMatch = "make_match";
    internal const string Inner = "inner";
    internal const string Outer = "outer";
    internal const string Captured = "captured";
    internal const string RemoteReady = "remote_ready";
    internal const string ConstructorMarker = "constructor_marker";
    internal const string Ancestors = "$ancestors";
    internal const string InitialCall = "initial_call";
    internal const string TranslateInitialCall = "translate_initial_call";
    internal const string RawInitialCall = "raw_initial_call";
    internal const string SnapshotAncestors = "ancestors";
    internal const string Finish = "finish";
    internal const string Undefined = "undefined";
    internal const string Root = "remote_source_fun_root";
    internal const string SourceModule = "source_fun_resolution";
    internal const string Make = "make";
    internal const string Old = "old";
    internal const string New = "new";
    internal const string PayloadSource = "-module(proc_lib_remote_payload).\n-export([maker/1, child/1]).\n\nmaker(Capture) ->\n    true = register(maps:get(marker, Capture), self()),\n    fun() -> child(Capture) end.\n\nchild(Capture) ->\n    Parent = maps:get(parent, Capture),\n    Marker = maps:get(marker, Capture),\n    Label = maps:get(label, Capture),\n    ok = proc_lib:set_label(Label),\n    Snapshot = #{captured => Capture, node => node(),\n                 initial_call => proc_lib:initial_call(self()),\n                 raw_initial_call => get('$initial_call'),\n                 ancestors => get('$ancestors'), group_leader => group_leader(),\n                 links => process_info(self(), links),\n                 label => proc_lib:get_label(self()), constructor_marker => whereis(Marker)},\n    Parent ! {remote_ready, self(), Snapshot},\n    receive\n        {finish, Parent} -> ok\n    after 15000 -> exit(payload_finish_timeout)\n    end.\n";
    internal const string OldSource = "-module(source_fun_resolution). -export([make/1]). make(X)->fun()->{old,X} end.";
    internal const string NewSource = "-module(source_fun_resolution). -export([make/1]). make(X)->fun()->{new,X} end.";
    internal const string LexicalSource = "-module(source_fun_lexical_contract).\r\n-export([run/0, make/1, make_shadow/1, make_match/1]).\r\nmake(X) -> fun(Y) -> Z = Y + 1, {X, Z} end.\r\nmake_shadow(X) -> fun(X) -> X end.\r\nmake_match(X) -> fun() -> X = outer, X end.\r\nrun() ->\r\n    A = make(10), B = make(20),\r\n    A2 = binary_to_term(term_to_binary(A)),\r\n    B2 = binary_to_term(term_to_binary(B)),\r\n    {10, 4} = A2(3), {20, 5} = B2(4),\r\n    X = outer,\r\n    Shadow = fun(X) -> X end,\r\n    inner = Shadow(inner),\r\n    Match = fun() -> X = outer, X end,\r\n    outer = Match(), ok.\r\n";
}
