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
    Counts=lists:foldl(fun(Path, Acc0) ->
        Ref=Read(filename:join(Root,Path)),
        Got=Read(filename:join(Generated,Path)),
        lists:foldl(fun(Key, Acc) ->
            case first_difference(maps:get(Key,Ref,[]),maps:get(Key,Got,[])) of
                same -> Acc;
                {Expected,Actual} ->
                    Class=category(Expected,Actual,maps:get(Key,Ref,[]),maps:get(Key,Got,[])),
                    maps:update_with(Class, fun({N, Mods, Example}) -> {N+1, sets:add_element(Path,Mods), Example} end,
                        {1,sets:from_list([Path]),{Path,Key,Expected,Actual}}, Acc)
            end
        end, Acc0, lists:usort(maps:keys(Ref) ++ maps:keys(Got)))
    end, #{}, Paths),
    Rows=lists:reverse(lists:sort([{sets:size(Mods), Count, Class, Example} || {Class,{Count,Mods,Example}} <- maps:to_list(Counts)])),
    io:format("~B differing files scanned. First differing instruction per function, after label normalization.~n",[length(Paths)]),
    lists:foreach(fun({Modules,Functions,Class,Example}) -> io:format("~B modules, ~B functions ~0p ~0p~n", [Modules,Functions,Class,Example]) end,
        lists:sublist(Rows,80)).

opcode({test,Name,_,_})->{test,Name};
opcode({test,Name,_,_,_,_})->{test,Name};
opcode({line,_})->line;
opcode(I) when is_tuple(I)->element(1,I);
opcode(I)->I.
first_difference([I|Expected],[I|Actual]) -> first_difference(Expected,Actual);
first_difference([E|_],[A|_]) -> {E,A};
first_difference([], [A|_]) -> {end_of_function,A};
first_difference([E|_], []) -> {E,end_of_function};
first_difference([],[]) -> same.



category({allocate,ES,L},{allocate,AS,L},R,G) when ES < AS ->
    {oversized,ES,following(R),following(G)};
category(_,_,_,_) -> other.

following(Is) ->
    Tail=lists:dropwhile(fun(I)->opcode(I)=/=allocate end,Is),
    lists:sublist([opcode(I)||I<-lists:nthtail(min(1,length(Tail)),Tail)],3).
