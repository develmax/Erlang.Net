namespace Erlang;

internal static class GenServerSourceLines
{
    // gen:start(?MODULE, nolink, ServerName, Module, Args, Options);
    internal const int StartLine1009 = 1009;
    // error(badarg, [ServerName, Module, Args, Options]).
    internal const int StartLine1011 = 1011;
    // error(badarg, [Module, Args, Options]).
    internal const int StartLinkLine1030 = 1030;
    // error(badarg, [ServerName, Module, Args, Options]).
    internal const int StartLinkLine1100 = 1100;
    // error(badarg, [Module, Args, Options]).
    internal const int StartMonitorLine1120 = 1120;
    // error(badarg, [ServerName, Module, Args, Options]).
    internal const int StartMonitorLine1151 = 1151;
    // case catch gen:call(ServerRef, '$gen_call', Request) of
    internal const int CallLine1217 = 1217;
    // exit({Reason, {?MODULE, call, [ServerRef, Request]}})
    internal const int CallLine1221 = 1221;
    // case catch gen:call(ServerRef, '$gen_call', Request, Timeout) of
    internal const int CallLine1297 = 1297;
    // exit({Reason, {?MODULE, call, [ServerRef, Request, Timeout]}})
    internal const int CallLine1301 = 1301;
    // gen:send_request(ServerRef, '$gen_call', Request)
    internal const int SendRequestLine1347 = 1347;
    // error(badarg, [ServerRef, Request])
    internal const int SendRequestLine1350 = 1350;
    // gen:send_request(ServerRef, '$gen_call', Request, Label, ReqIdCol)
    internal const int SendRequestLine1378 = 1378;
    // error(badarg, [ServerRef, Request, Label, ReqIdCol])
    internal const int SendRequestLine1381 = 1381;
    // gen:wait_response(ReqId, WaitTime)
    internal const int WaitResponseLine1417 = 1417;
    // error(badarg, [ReqId, WaitTime])
    internal const int WaitResponseLine1420 = 1420;
    // gen:wait_response(ReqIdCol, WaitTime, Delete)
    internal const int WaitResponseLine1491 = 1491;
    // error(badarg, [ReqIdCol, WaitTime, Delete])
    internal const int WaitResponseLine1494 = 1494;
    // gen:receive_response(ReqId, Timeout)
    internal const int ReceiveResponseLine1533 = 1533;
    // error(badarg, [ReqId, Timeout])
    internal const int ReceiveResponseLine1536 = 1536;
    // gen:receive_response(ReqIdCol, Timeout, Delete)
    internal const int ReceiveResponseLine1609 = 1609;
    // error(badarg, [ReqIdCol, Timeout, Delete])
    internal const int ReceiveResponseLine1612 = 1612;
    // gen:check_response(Msg, ReqId)
    internal const int CheckResponseLine1644 = 1644;
    // error(badarg, [Msg, ReqId])
    internal const int CheckResponseLine1647 = 1647;
    // gen:check_response(Msg, ReqIdCol, Delete)
    internal const int CheckResponseLine1709 = 1709;
    // error(badarg, [Msg, ReqIdCol, Delete])
    internal const int CheckResponseLine1712 = 1712;
    // gen:reqids_size(ReqIdCollection)
    internal const int ReqidsSizeLine1746 = 1746;
    // error:badarg -> error(badarg, [ReqIdCollection])
    internal const int ReqidsSizeLine1748 = 1748;
    // gen:reqids_add(ReqId, Label, ReqIdCollection)
    internal const int ReqidsAddLine1765 = 1765;
    // error:badarg -> error(badarg, [ReqId, Label, ReqIdCollection])
    internal const int ReqidsAddLine1767 = 1767;
    // gen:reqids_to_list(ReqIdCollection)
    internal const int ReqidsToListLine1783 = 1783;
    // error:badarg -> error(badarg, [ReqIdCollection])
    internal const int ReqidsToListLine1785 = 1785;
    // cast({global,Name}, Request) ->
    internal const int CastLine1809 = 1809;
    // abcast(Name, Request) when is_atom(Name) ->
    internal const int AbcastLine1870 = 1870;
    // abcast(Nodes, Name, Request) when is_list(Nodes), is_atom(Name) ->
    internal const int AbcastLine1893 = 1893;
    // do_abcast([Node|Nodes], Name, Msg) when is_atom(Node) ->
    internal const int DoAbcastLine1896 = 1896;
    // multi_call(Name, Request)
    internal const int MultiCallLine1928 = 1928;
    // multi_call(Nodes, Name, Request)
    internal const int MultiCallLine1943 = 1943;
    // multi_call(Nodes, Name, Request, Timeout)
    internal const int MultiCallLine2005 = 2005;
    // error(badarg).
    internal const int McSendLine2050 = 2050;
    // enter_loop(Mod, Options, State)
    internal const int EnterLoopLine2115 = 2115;
    // enter_loop(Mod, Options, State, ServerName = {Scope, _})
    internal const int EnterLoopLine2144 = 2144;
    // enter_loop(Mod, Options, State, ServerName, Action)
    internal const int EnterLoopLine2206 = 2206;
    // Name = gen:get_proc_name(ServerName),
    internal const int EnterLoopLine2208 = 2208;
    // ServerData = server_data(gen:get_parent(), Name, Mod, gen:hibernate_after(Options)),
    internal const int EnterLoopLine2209 = 2209;
    // exit({bad_action, Action});
    internal const int EnterLoopLine2213 = 2213;
    // LoopAction ->
    internal const int InitItLine2245 = 2245;
    // exit(Reason);
    internal const int InitItLine2257 = 2257;
    // exit({bad_return_value, Else});
    internal const int InitItLine2269 = 2269;
    // {ok, Mod:init(Args)}
    internal const int InitItLine2276 = 2276;
    // receive
    internal const int LoopLine2321 = 2321;
    // receive
    internal const int LoopLine2330 = 2330;
    // {ok, HandleContinue(Msg, State)}
    internal const int TryHandleContinueLine2424 = 2424;
    // {ok, HandleInfo(Msg, State)}
    internal const int TryHandleInfoLine2434 = 2434;
    // ?LOG_WARNING(
    internal const int TryHandleInfoLine2441 = 2441;
    // {ok, HandleCast(Msg, State)}
    internal const int TryHandleCastLine2460 = 2460;
    // {ok, HandleCall(Msg, From, State)}
    internal const int TryHandleCallLine2470 = 2470;
    // {ok, Mod:terminate(Reason, State)}
    internal const int TryTerminateLine2482 = 2482;
    // erlang:start_timer(T, self(), Tag, [{abs,Abs}]);
    internal const int HandleTimerLine2640 = 2640;
    // erlang:start_timer(T, self(), Tag)
    internal const int HandleTimerLine2642 = 2642;
    // system_continue(
    internal const int SystemContinueLine2656 = 2656;
    // system_code_change([#server_data{module = Mod} = ServerData, State, Timer, Hib], _Module, OldVsn, Extra) ->
    internal const int SystemCodeChangeLine2667 = 2667;
    // case catch Mod:code_change(OldVsn, State, Extra) of
    internal const int SystemCodeChangeLine2668 = 2668;
    // system_get_state([_ServerData, State, _Timer, _Hib]) ->
    internal const int SystemGetStateLine2674 = 2674;
    // system_replace_state(StateFun, [ServerData, State, Timer, Hib]) ->
    internal const int SystemReplaceStateLine2678 = 2678;
    // NState = StateFun(State),
    internal const int SystemReplaceStateLine2679 = 2679;
    // name=>Name,
    internal const int ErrorInfoLine2786 = 2786;
    // client_stacktrace(undefined) ->
    internal const int ClientStacktraceLine2799 = 2799;
    // limit_report(Report,unlimited) ->
    internal const int LimitReportLine2830 = 2830;
    // FormatOpts = maps:merge(Default,FormatOpts0),
    internal const int FormatLogLine2863 = 2863;
    // io_lib:format(Format, Args, IoOpts).
    internal const int FormatLogLine2872 = 2872;
    // format_log_single(#{label:={gen_server,terminate},
    internal const int FormatLogSingleLine2874 = 2874;
    // format_log_multi(#{label:={gen_server,terminate},
    internal const int FormatLogMultiLine2931 = 2931;
    // format_client_log_single(undefined,_) ->
    internal const int FormatClientLogSingleLine3025 = 3025;
    // format_client_log(undefined,_) ->
    internal const int FormatClientLogLine3045 = 3045;
    // [PDict, SysState, Parent, Debug, [#server_data{parent=Parent, name=Name, module=Mod}, State, _Timer, _Hib]] = StatusData,
    internal const int FormatStatusLine3082 = 3082;
    internal const int ContinueDispatch = 2291;
    internal const int CallDispatch = 2499;
    internal const int DebugCallDispatch = 2525;
    internal const int CastDispatch = 2418;
    internal const int InfoDispatch = 2420;
    internal const int FormatReport = 2871;
    internal const int MultiSend = 2012;
    internal const int WarningDirective = 2985;
    internal const int TerminateDirective = 2942;
    internal const int PrintDirective = 3065;
    internal const int SingleLine = 3071;
    internal const int InitializeCallback = 2236;
    internal const int TerminateCallback = 2733;
    internal const int SystemTerminateLine2663 = 2663;
    internal const int TryTerminate = 2478;
    internal const int StartDelegation = 986;
    internal const int StartArgumentError = 988;
}
