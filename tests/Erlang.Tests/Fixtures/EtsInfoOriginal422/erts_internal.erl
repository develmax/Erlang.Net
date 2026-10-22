-module(erts_internal).
-export([cmp_term/2,map_next/3,ets_super_user/1,ets_raw_first/1,ets_raw_next/2,ets_lookup_binary_info/2,ets_info_binary/1]).

-file("erts_internal.erl",413).
cmp_term(_A,_B) ->
    erlang:nif_error(undefined).

-file("erts_internal.erl",456).
map_next(_I, _M, _A) ->
    erlang:nif_error(undefined).

-file("erts_internal.erl",930).
ets_super_user(_Bool) ->
    erlang:nif_error(undef).

-file("erts_internal.erl",936).
ets_raw_first(_Tab) ->
    erlang:nif_error(undef).

-file("erts_internal.erl",943).
ets_raw_next(_Tab, _Key) ->
    erlang:nif_error(undef).

-file("erts_internal.erl",924).
ets_lookup_binary_info(_Tab, _Key) ->
    erlang:nif_error(undef).

-file("erts_internal.erl",950).
ets_info_binary(Tab) ->
    try
        erts_internal:ets_super_user(true),
        ets:safe_fixtable(Tab, true),
        ets_info_binary_iter(Tab, erts_internal:ets_raw_first(Tab), [])
    catch
        C:R:S ->
            ets_info_binary_error(Tab, C, R, S)
    after
        ets:safe_fixtable(Tab, false),
        erts_internal:ets_super_user(false)
    end.

-file("erts_internal.erl",963).
ets_info_binary_error(Tab, C, R, []) ->
    erlang:raise(C, R, [{ets, info, [Tab, binary], []}]);
ets_info_binary_error(Tab, C, R, [SF|SFs]) when
      element(1, SF) == erts_internal,
      element(2, SF) == ets_info_binary ->
    erlang:raise(C, R, [{ets, info, [Tab, binary], []}|SFs]);
ets_info_binary_error(Tab, C, R, [_SF|SFs]) ->
    ets_info_binary_error(Tab, C, R, SFs).

-file("erts_internal.erl",972).
ets_info_binary_iter(_Tab, '$end_of_table', Acc) ->
    Acc;
ets_info_binary_iter(Tab, Key, Acc) ->
    NewAcc = case erts_internal:ets_lookup_binary_info(Tab, Key) of
                 [] -> Acc;
                 [BI] -> [BI|Acc];
                 [_|_] = BIL -> BIL ++ Acc
             end,
    ets_info_binary_iter(Tab, erts_internal:ets_raw_next(Tab, Key), NewAcc).

