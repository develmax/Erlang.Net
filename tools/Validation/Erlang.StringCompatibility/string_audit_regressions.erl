-module(string_audit_regressions).
-export([write/1]).

write(Directory) ->
    Search = [{F,[T,P]} || F <- [prefix,find,split],
        T <- ["abc",<<"abc">>,[<<"a">>,"bc"]],
        P <- [[[]],[<<>>],<<255>>]],
    Take = [{take,[T,S,C,D]} ||
        T <- [<<"a">>,[<<"a">>,"!",invalid],
            [<<"a">>,98|<<255>>],[<<"a">>,"!"|<<255>>]],
        S <- ["a",invalid], C <- [false,true], D <- [leading,trailing]],
    Cases = lists:usort(Search ++ Take ++
        [{replace,["abc",<<255>>,"x"]},
         {lexemes,[[],invalid]}, {lexemes,[[],<<>>]},
         {take,[<<97,255>>,[97,97],true,leading]},
         {take,[<<"a">>,[97|tail],true,leading]},
         {to_integer,[lists:duplicate(4096,$9)]},
         {to_integer,[list_to_binary(lists:duplicate(4096,$9))]}]),
    Rows = [{F,A,string_port_cases:run(F,A)} || {F,A} <- Cases],
    ok = file:write_file(filename:join(Directory,"audit-regressions.etf"),term_to_binary(Rows)),
    io:format("~p permanent audit regressions~n",[length(Rows)]),
    ok.
