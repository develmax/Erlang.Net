-module(beam_append_tuple_branches).
-export([run/5]).
-record(info, {uri}).
run(Code, Op, none, Path, Reason) ->
    {Code, none, lists:flatten("longer" ++ ": " ++ Op ++ " " ++ Path ++ Reason)};
run(Code, Op, Info, Path, Reason) ->
    {Code, Info#info.uri,
     lists:flatten("longer" ++ ": " ++ Op ++ " " ++ Path ++ Reason)}.
