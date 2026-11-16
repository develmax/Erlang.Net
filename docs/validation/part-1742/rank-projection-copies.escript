-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Rows = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Matches = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    io:format("~B modules, ~B functions with Y-to-X projection copies that use direct X projections in OTP.~n",
        [length(Matches), lists:sum([length(Fs)||{_,Fs}<-Matches])]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Matches],
    ok.

compare(Official, Generated) ->
    Reference = read(Official),
    Candidate = read(Generated),
    lists:usort([F || {F,Code} <- maps:to_list(Candidate),
          {ok,Ref} <- [maps:find(F,Reference)],
          {Field,X} <- lists:usort(candidates(Code)),
          lists:any(fun
             ({get_tuple_element,_,I,D}) -> I =:= Field andalso reg(D) =:= X;
             (_) -> false
          end, Ref)]).

read(File) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(File),
    maps:from_list([{{Name,Arity},Is} || {function,Name,Arity,_,Is} <- Fs]).

candidates([{get_tuple_element,_,Field,{y,_}=Slot}|Tail]) ->
    case find_copy(Tail,Slot,[]) of
        {ok,X} -> [{Field,X}|candidates(Tail)];
        no -> candidates(Tail)
    end;
candidates([_|Tail]) -> candidates(Tail);
candidates([]) -> [].

find_copy([{move,Slot,{x,_}=X}|_],Slot,Between) ->
    case lists:any(fun(I)->contains(I,X) end,Between) of
        true -> no;
        false -> {ok,X}
    end;
find_copy([I|Tail],Slot,Between) ->
    case lists:member(op(I),[get_tuple_element,move,line]) andalso not contains(I,Slot) of
        true -> find_copy(Tail,Slot,[I|Between]);
        false -> no
    end;
find_copy([],_,_) -> no.

contains({literal,_},_) -> false;
contains({tr,R,_},R) -> true;
contains(R,R) -> true;
contains(T,R) when is_tuple(T) -> contains(tuple_to_list(T),R);
contains(L,R) when is_list(L) -> lists:any(fun(I)->contains(I,R) end,L);
contains(_,_) -> false.
reg({tr,R,_}) -> R;
reg(R) -> R.
op(T) when is_tuple(T) -> element(1,T);
op(A) -> A.

