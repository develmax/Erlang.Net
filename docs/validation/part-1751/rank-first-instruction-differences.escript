-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = lists:sort([{length(Fs),P,Fs} || P <- Paths,
                  Fs <- [compare(filename:join(Official,P),filename:join(Generated,P))]]),
    Almost = lists:takewhile(fun({N,_,_}) -> N =< 1 end, Rows),
    Zero = length([ok || {0,_,_} <- Almost]),
    One = length(Almost) - Zero,
    io:format("~B images have all disassembled functions matching after label and fun identity normalization. ~B differ in one function.~n",[Zero,One]),
    [io:format("~s ~0p~n",[P,Fs]) || {_,P,Fs} <- Almost],
    Counts = lists:foldl(fun({_,P,Fs}, Acc0) ->
           lists:foldl(fun({_,Shape}, Acc) ->
               maps:update_with(Shape, fun({Modules,N}) ->
                   {Modules#{P=>true},N+1}
               end, {#{P=>true},1}, Acc)
           end, Acc0, Fs)
       end, #{}, Rows),
    Ranked = lists:reverse(lists:sort([{map_size(Ms),N,Shape} || {Shape,{Ms,N}} <- maps:to_list(Counts)])),
    io:format("First differing instructions ranked by number of modules:~n"),
    [io:format("~B modules ~B functions ~0p~n",[M,N,S]) || {M,N,S} <- lists:sublist(Ranked, 40)],
    ok.

compare(Reference,Generated) ->
    R=functions(Reference),
    G=functions(Generated),
    [{F,first_difference(maps:get(F,R,undefined),maps:get(F,G,undefined))}
        || F <- lists:usort(maps:keys(R)++maps:keys(G)),
           maps:get(F,R,undefined) =/= maps:get(F,G,undefined)].

first_difference([I|R],[I|G]) -> first_difference(R,G);
first_difference([R|_],[G|_]) -> {opcode(R),opcode(G)};
first_difference([],[_|_]) -> extra;
first_difference([_|_],[]) -> shortened;
first_difference(undefined,_) -> added;
first_difference(_,undefined) -> missing.

opcode({test,Name,_,_}) -> Name;
opcode({bif,Name,_,_,_}) -> {bif,Name};
opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},normalize(Is)} || {function,N,A,_,Is} <- Fs]).

normalize(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L} <- Is],lists:seq(1,length([L || {label,L} <- Is])))),
    [fix(I,Labels) || I <- Is].

fix({label,L},Labels) -> {label,maps:get(L,Labels,L)};
fix({f,L},Labels) -> {f,maps:get(L,Labels,L)};
fix({literal,V},_) -> {literal,V};
fix({make_fun3,M,I,_,Dst,Args},Ls) -> {make_fun3,M,I,uniq,fix(Dst,Ls),fix(Args,Ls)};
fix(T,Ls) when is_tuple(T) -> list_to_tuple([fix(X,Ls) || X <- tuple_to_list(T)]);
fix([H|T],Ls) -> [fix(H,Ls)|fix(T,Ls)];
fix(V,_) -> V.
