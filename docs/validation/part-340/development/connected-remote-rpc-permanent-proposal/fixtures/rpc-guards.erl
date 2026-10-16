-module(rpc_dependency_cases).
-export([run/0,local_caller/0, throws/0, exits/0, errors/0, guards/0,
         disconnected/0, always_spawn/0, user_system_limit/0]).

%% Source-derived contracts for the five-export dependency slice, not observed baselines.
local_caller() ->
    Self = self(),
    Self = rpc:call(node(), erlang, self, []),
    Self = erpc:call(node(), erlang, self, []),
    undefined = rpc:call(node(), erlang, put, [rpc_caller_dictionary, stored]),
    stored = get(rpc_caller_dictionary),
    stored = erase(rpc_caller_dictionary),
    ok.

throws() ->
    value = rpc:call(node(), erlang, throw, [value]),
    {badrpc, {'EXIT', thrown}} = rpc:call(node(), erlang, throw, [{'EXIT', thrown}]),
    try erpc:call(node(), erlang, throw, [value]) of
        _ -> error(unexpected_return)
    catch throw:value -> ok end.

exits() ->
    {badrpc, {'EXIT', stopped}} = rpc:call(node(), erlang, exit, [stopped]),
    try erpc:call(node(), erlang, exit, [stopped]) of
        _ -> error(unexpected_return)
    catch exit:{exception, stopped} -> ok end.

errors() ->
    {badrpc, {'EXIT', {reason, [{erlang,error,[reason],_}|_]}}} = rpc:call(node(), erlang, error, [reason]),
    try erpc:call(node(), rpc_dependency_cases, user_system_limit, []) of
        _ -> error(unexpected_return)
    catch error:{exception,system_limit,[_|_]} -> ok end.

guards() ->
    badarg(fun() -> rpc:call(1,erlang,self,[]) end),
    badarg(fun() -> rpc:call(node(),erlang,self,[],invalid_timeout) end),
    badarg(fun() -> rpc:call(node(),erlang,self,[],#{always_spawn=>invalid}) end),
    {badrpc,{'EXIT',{badarg,[_|_]}}} = rpc:call(node(),erlang,self,[a|tail]),
    ok.

disconnected() ->
    {badrpc,nodedown} = rpc:call(missing_rpc@localhost,erlang,self,[]),
    try erpc:call(missing_rpc@localhost,erlang,self,[]) of
        _ -> error(unexpected_return)
    catch error:{erpc,noconnection} -> ok end.

always_spawn() ->
    Self = self(),
    Child = erpc:call(node(),erlang,self,[],#{always_spawn=>true}),
    true = is_pid(Child), false = (Child =:= Self),
    ok.

user_system_limit() -> error(system_limit).

badarg(F) ->
    try F() of _ -> error(unexpected_return)
    catch error:badarg -> ok end.

run() -> guards().
