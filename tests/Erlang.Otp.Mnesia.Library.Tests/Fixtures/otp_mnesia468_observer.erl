-module(otp_mnesia468_observer).
-export([worker/1, run/0]).
-file("otp_mnesia468_observer.erl", 1).

worker(Parent) -> Parent ! {finished, run()}.
run() ->
    S1 = mnesia_frag_hash:init_state(table, undefined),
    {S2,_,_} = mnesia_frag_hash:add_frag(S1),
    {S3,_,_} = mnesia_frag_hash:add_frag(S2),
    {S4,_,_} = mnesia_frag_hash:add_frag(S3),
    [observe(init, fun() -> mnesia_frag_hash:init_state(table, undefined) end),
     observe(init_bad, fun() -> mnesia_frag_hash:init_state(table, invalid) end),
     observe(add_first, fun() -> mnesia_frag_hash:add_frag(S1) end),
     observe(add_second, fun() -> mnesia_frag_hash:add_frag(S2) end),
     observe(add_third, fun() -> mnesia_frag_hash:add_frag(S3) end),
     observe(add_eighth, fun() -> grow(8, S1) end),
     observe(del_second, fun() -> mnesia_frag_hash:del_frag(S2) end),
     observe(del_third, fun() -> mnesia_frag_hash:del_frag(S3) end),
     observe(del_fourth, fun() -> mnesia_frag_hash:del_frag(S4) end),
     observe(old_add, fun() -> mnesia_frag_hash:add_frag({hash_state,2,1,1}) end),
     observe(old_del, fun() -> mnesia_frag_hash:del_frag({hash_state,4,1,2}) end),
     observe(key_integer, fun() -> mnesia_frag_hash:key_to_frag_number(S3, 42) end),
     observe(key_negative, fun() -> mnesia_frag_hash:key_to_frag_number(S3, -42) end),
     observe(key_binary, fun() -> mnesia_frag_hash:key_to_frag_number(S4, <<1,2,3>>) end),
     observe(key_legacy, fun() -> mnesia_frag_hash:key_to_frag_number({hash_state,4,1,2}, 42) end),
     observe(match_empty, fun() -> mnesia_frag_hash:match_spec_to_frag_numbers(S3, []) end),
     observe(match_any, fun() -> mnesia_frag_hash:match_spec_to_frag_numbers(S4, '_') end),
     observe(match_multiple, fun() -> mnesia_frag_hash:match_spec_to_frag_numbers(S3, [a,b]) end),
     observe(add_bad, fun() -> mnesia_frag_hash:add_frag(42) end),
     observe(del_bad, fun() -> mnesia_frag_hash:del_frag(42) end),
     observe(key_bad, fun() -> mnesia_frag_hash:key_to_frag_number(42, key) end),
     observe(match_bad, fun() -> mnesia_frag_hash:match_spec_to_frag_numbers(42, []) end),
     observe(app_start_mode, fun() -> mnesia_app:start(takeover, []) end),
     observe(app_stop, fun() -> mnesia_app:stop(any_state) end)].
grow(0, State) -> State;
grow(N, State) -> {Next,_,_} = mnesia_frag_hash:add_frag(State), grow(N-1, Next).
observe(Name, Fun) ->
    try {Name, {returned, Fun()}}
    catch Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
