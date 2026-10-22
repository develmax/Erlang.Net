-module(beam_debug_dispatch432).
-export([run/0,run_stacks/0,capture/1,target/0]).

cases() -> [enable_target,disable_target,missing_module,missing_function,negative_arity,large_small_arity,
    invalid_flag,invalid_mfa,wrong_size,nonatom_module,nonatom_function,big_arity,arity_atom,
    bad_wildcard_function,bad_wildcard_module,wildcard_arity].

run() -> [{Case,observe(Case)} || Case <- cases()].
run_stacks() ->
    [{Depth,Depth,frames(Depth,fun() -> [{Case,observe(Case)} || Case <- cases()] end)} || Depth <- [0,1,5,17,73]].
frames(0,Fun) -> Fun();
frames(Depth,Fun) -> try frames(Depth-1,Fun) of Value -> erlang:put(debug432_last,Depth),Value catch Class:Reason:Stack -> erlang:raise(Class,Reason,Stack) end.

observe(Case) ->
    try operation(Case) of Value -> {value,Value}
    catch Class:Reason:Stack -> {exception,Class,Reason,Stack} end.

operation(enable_target) -> erts_debug:breakpoint({?MODULE,target,0},true);
operation(disable_target) -> erts_debug:breakpoint({?MODULE,target,0},false);
operation(missing_module) -> erts_debug:breakpoint({debug432_missing,target,0},true);
operation(missing_function) -> erts_debug:breakpoint({?MODULE,missing,0},true);
operation(negative_arity) -> erts_debug:breakpoint({?MODULE,target,-1},true);
operation(large_small_arity) -> erts_debug:breakpoint({?MODULE,target,1000000},true);
operation(invalid_flag) -> erts_debug:breakpoint({?MODULE,target,0},invalid);
operation(invalid_mfa) -> erts_debug:breakpoint([],false);
operation(wrong_size) -> erts_debug:breakpoint({?MODULE,target},false);
operation(nonatom_module) -> erts_debug:breakpoint({1,target,0},false);
operation(nonatom_function) -> erts_debug:breakpoint({?MODULE,1,0},false);
operation(big_arity) -> erts_debug:breakpoint({?MODULE,target,1 bsl 80},false);
operation(arity_atom) -> erts_debug:breakpoint({?MODULE,target,invalid},false);
operation(bad_wildcard_function) -> erts_debug:breakpoint({?MODULE,'_',0},false);
operation(bad_wildcard_module) -> erts_debug:breakpoint({'_',target,'_'},false);
operation(wildcard_arity) -> erts_debug:breakpoint({?MODULE,target,'_'},false).

%% Original capture never invokes an armed function: native OS debug traps are
%% platform state. The own-runtime HOST must prove actual pause/resume/kill.
target() -> ok.
capture([Directory]) ->
    ok=prim_file:write_file(Directory++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/instructions.etf",term_to_binary(erts_debug:instructions(),[{minor_version,2}])),
    ok=prim_file:write_file(Directory++"/profile.etf",term_to_binary({erlang:system_info(wordsize),erlang:system_info(otp_release)},[{minor_version,2}])),
    erlang:halt(0).
