-module(string_audit_jaro).
-export([write/1,write_regression/1]).

resource_case() ->
    {jaro_similarity,[binary:copy(<<"a">>,16384),binary:copy(<<"b">>,16384)]}.

write_regression(Directory) ->
    {F,A} = resource_case(),
    Row = {F,A,string_port_cases:run(F,A)},
    file:write_file(filename:join(Directory,"jaro-regression.etf"),term_to_binary([Row])).

write(Directory) ->
    ok = filelib:ensure_dir(filename:join(Directory,"placeholder")),
    Words = lists:append([words(N) || N <- lists:seq(0,4)]),
    Small = [{jaro_similarity,[A,B]} || X <- Words,Y <- Words,
        A <- [X,list_to_binary(X)], B <- [Y,list_to_binary(Y)]],
    Clusters = [[],[97,778],[229],[13,10],[16#1F469,16#200D,16#1F469]],
    Unicode = [{jaro_similarity,[A,B]} || A <- Clusters,B <- Clusters] ++
        [{jaro_similarity,[unicode:characters_to_binary(A),B]} || A <- Clusters,B <- Clusters],
    Seeds = lists:sublist(string_port_cases:cases(),71),
    Cases = lists:usort(Seeds ++ Small ++ Unicode ++ [resource_case()]),
    Rows = [{F,A,string_port_cases:run(F,A)} || {F,A} <- Cases],
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Directory,"cases.etf"),term_to_binary(Cases)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort(string:module_info(exports) -- [{module_info,0},{module_info,1}]))),
    ok = file:write_file(filename:join(Directory,"backing.etf"),term_to_binary(string_port_cases:backing())),
    io:format("~p Jaro cases~n",[length(Rows)]),
    ok.

words(0) -> [[]];
words(N) -> [[C|R] || C <- "ab", R <- words(N-1)].
