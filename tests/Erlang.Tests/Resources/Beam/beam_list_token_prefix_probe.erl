-module(beam_list_token_prefix_probe).
-export([scan/3, quoted/3]).

scan([$"|Rest], _, Acc) ->
    {{token,1,lists:reverse(Acc)},Rest};
scan([Head|Rest], Line, Acc) ->
    scan(Rest, Line, [Head|Acc]);
scan([], _, _) ->
    missing.

quoted([$!,$A|Rest], Line, Acc) ->
    {{alpha,Line,lists:reverse(Acc)},Rest};
quoted([$!,$B|Rest], Line, Acc) ->
    {{beta,Line,lists:reverse(Acc)},Rest};
quoted([Head|Rest], Line, Acc) ->
    quoted(Rest, Line, [Head|Acc]);
quoted([], _, _) ->
    missing.
