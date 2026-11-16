#!/usr/bin/env escript
-mode(compile).
main([Root,Generated,PathFile]) ->
    {ok, Content} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Line) || Line <- binary:split(Content, <<10>>, [global]), Line =/= <<>>],
Norm=fun(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L}<-Is],lists:seq(1,length([L || {label,L}<-Is])))),
    Fix=fun F({label,L})->{label,maps:get(L,Labels,L)};
        F({f,L})->{f,maps:get(L,Labels,L)};
        F({literal,V})->{literal,V};
        F({make_fun3,M,I,_,Dst,Args})->{make_fun3,M,I,uniq,F(Dst),F(Args)};
        F(T) when is_tuple(T)->list_to_tuple([F(X)||X<-tuple_to_list(T)]);
        F([H|T])->[F(H)|F(T)];
        F(V)->V
    end,
    [Fix(I)||I<-Is]
end,
Read=fun(P)->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Norm(Is)}||{function,N,A,_,Is}<-Fs])
end,
    Rows=lists:flatmap(fun(Path) ->
        R=Read(filename:join(Root,Path)),
        C=Read(filename:join(Generated,Path)),
        [{Path,K,relocations(B),relocations(maps:get(K,C,[]))} ||
            {K,B}<-maps:to_list(R),relocations(B)>relocations(maps:get(K,C,[]))]
    end,Paths),
    io:format("~B modules, ~B functions with fewer move-to-trim sequences than OTP~n",
              [length(lists:usort([P || {P,_,_,_}<-Rows])),length(Rows)]),
    lists:foreach(fun(Row)->io:format("~0p~n",[Row]) end,Rows).

relocations([{move,{y,S},{y,D}},{trim,N,_}|Rest]) when S<N,D>=N ->
    1+relocations(Rest);
relocations([_|Rest])->relocations(Rest);
relocations([])->0.