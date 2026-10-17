-module(entry_observer).
-export([metadata/0,run/0]).
-record(native_record, {value}).

metadata() ->
    Expected = filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),
    Expected = filename:absname(code:root_dir()),
    {term_callback_metadata,Expected,erlang:system_info(otp_release),
        erlang:system_info(version),erlang:system_info(system_architecture),
        erlang:system_info(wordsize)}.

run() ->
    ok = term_callback_contract:load(filename:join(os:getenv("ENTRY_NATIVE_ROOT"), "term_callbacks")),
    LocalPid = self(),
    LocalRef = make_ref(),
    [LocalPort|_] = erlang:ports(),
    true = node(LocalPort) =:= node(),
    <<131,Node/binary>> = term_to_binary('abi_peer@fixture'),
    RemotePid = binary_to_term(<<131,88,Node/binary,17:32,0:32,7:32>>),
    RemotePort = binary_to_term(<<131,89,Node/binary,23:32,7:32>>),
    RemoteRef = binary_to_term(<<131,90,1:16,Node/binary,7:32,31:32>>),
    true = node(RemotePid) =/= node(),
    true = node(RemotePort) =/= node(),
    true = node(RemoteRef) =/= node(),
    Cases = [{nil,[]},{proper,[1,2]},{improper,[1|tail_atom]},
        {atom,hello},{integer,42},{big_integer,1 bsl 100},{float,1.5},
        {tuple,{a,1}},{empty_tuple,{}},{binary,<<1,2>>},
        {sub_binary,binary:part(<<0,1,2,3>>,1,2)},{bitstring,<<1:1>>},
        {closure,fun() -> LocalRef end},{exported_fun,fun erlang:self/0},
        {local_pid,LocalPid},{external_pid,RemotePid},
        {local_port,LocalPort},{external_port,RemotePort},
        {local_ref,LocalRef},{external_ref,RemoteRef},{map,#{a=>1}},
        {conventional_record,#native_record{value=payload}}],
    Rows = [observe(Name,Value) || {Name,Value} <- Cases],
    Arrays = [observe_arrays(N) || N <- [0,3,8193]],
    true = code:delete(term_callback_contract),
    false = code:purge(term_callback_contract),
    false = erlang:check_old_code(term_callback_contract),
    false = code:is_loaded(term_callback_contract),
    {term_callback_contract,Rows,Arrays,{cleanup,deleted_and_purged}}.

observe(Name, Value) ->
    ExpectedFlags = list_to_tuple([flag(X) || X <- [is_atom(Value),is_binary(Value),
        is_reference(Value),is_function(Value),is_pid(Value),is_port(Value),
        Value =:= [],is_cons_or_nil(Value),is_tuple(Value),is_number(Value),is_map(Value)]]),
    ExpectedCell = case Value of [Head|Tail] -> {1,Head,Tail}; _ -> {0,101,202} end,
    Actual = term_callback_contract:inspect(Value),
    {ExpectedFlags,ExpectedCell} = Actual,
    NullActual = term_callback_contract:inspect_null(Value),
    Actual = NullActual,
    {Name,Value,Actual,NullActual}.

flag(true) -> 1;
flag(false) -> 0.

is_cons_or_nil([]) -> true;
is_cons_or_nil([_|_]) -> true;
is_cons_or_nil(_) -> false.

observe_arrays(Count) ->
    Values = case Count of 0 -> []; _ -> lists:seq(0,Count-1) end,
    Expected = {list_to_tuple(Values),Values},
    Expected = term_callback_contract:arrays(Count),
    {Count,Expected}.
