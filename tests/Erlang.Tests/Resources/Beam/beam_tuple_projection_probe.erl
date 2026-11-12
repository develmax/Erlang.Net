-module(beam_tuple_projection_probe).
-export([project/1, split/4]).

project(Hosts) ->
    [{dns_id, Host} || Host <- Hosts].

split([{recv_cnt, R}|Rest], _, W, P) -> split(Rest, R, W, P);
split([{send_cnt, W}|Rest], R, _, P) -> split(Rest, R, W, P);
split([{send_pend, P}|Rest], R, W, _) -> split(Rest, R, W, P);
split([], R, W, P) -> {ok, R, W, P}.
