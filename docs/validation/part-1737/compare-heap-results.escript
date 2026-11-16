-mode(compile).

main([Root, Before, After, PathList]) ->
    {ok, Raw} = file:read_file(PathList),
    Paths = [binary_to_list(P) || P <- binary:split(Raw, <<10>>, [global]), P =/= <<>>],
    Rows = [{P, compare(read(Before,Root,P), read(After,Root,P), read(Root,Root,P))} || P <- Paths],
    Changed = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    Moves = [M || {_, Fs} <- Changed, {_, Ms} <- Fs, M <- Ms],
    io:format("~B heap tests retain X0 for construction in ~B functions across ~B modules with the same before and after sites.~n",
        [length(Moves),lists:sum([length(Fs) || {_,Fs} <- Changed]),length(Changed)]),
    io:format("~B also use X0 in the corresponding OTP call and construction sequence.~n",
        [length([M || {_,direct}=M <- Moves])]),
    Groups = lists:foldl(fun({K,_},A) -> maps:update_with(K,fun(N)->N+1 end,1,A) end,#{},Moves),
    [io:format("~p ~B~n",[K,N]) || {N,K} <- lists:reverse(lists:sort([{N,K} || {K,N} <- maps:to_list(Groups)]))].

read(Directory, Root, P) ->
    Candidate = filename:join(Directory,P),
    Actual = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Actual),
    maps:from_list([{{N,A},scan([I || I <- Code, not lists:member(op(I),[line,init_yregs])])}
                    || {function,N,A,_,Code} <- Functions]).

scan([Call,{move,{x,0},{y,_}=Slot},{test_heap,Words,0},Build|T]=Code) ->
    case ordinary(Call) andalso constructor(Build) andalso contains(Build,Slot) of
        true -> [{{Call,Words,op(Build)},saved}|scan(T)];
        false -> scan(tl(Code))
    end;
scan([Call,{test_heap,Words,Live},Build|T]=Code) when Live >= 1 ->
    case ordinary(Call) andalso constructor(Build) andalso contains(Build,{x,0}) of
        true -> [{{Call,Words,op(Build)},direct}|scan(T)];
        false -> scan(tl(Code))
    end;
scan([_|T]) -> scan(T);
scan([]) -> [].

ordinary(I) -> lists:member(op(I),[call,call_ext,apply,call_fun,call_fun2]).
constructor(I) -> lists:member(op(I),[put_tuple2,put_list,update_record]).
contains(V,V) -> true;
contains({literal,_},_) -> false;
contains(T,V) when is_tuple(T) -> contains(tuple_to_list(T),V);
contains(L,V) when is_list(L) -> lists:any(fun(I) -> contains(I,V) end,L);
contains(_,_) -> false.
op(T) when is_tuple(T) -> element(1,T);
op(V) -> V.

compare(Before, After, Otp) ->
    maps:fold(fun(Key, New, Acc) ->
        Old = maps:get(Key,Before,[]),
        Ref = maps:get(Key,Otp,[]),
        case sites(Old) =:= sites(New) andalso Old =/= New of
            true ->
                RefFlags = case sites(Ref) =:= sites(New) of
                    true -> [F || {_,F} <- Ref];
                    false -> lists:duplicate(length(New),unknown)
                end,
                Moves = [{K,Expected} || {{{Call,Words,K},saved},{{Call,Words,K},direct},Expected}
                                         <- lists:zip3(Old,New,RefFlags)],
                case Moves of [] -> Acc; _ -> [{Key,Moves}|Acc] end;
            false -> Acc
        end
    end,[],After).
sites(F) -> [S || {S,_} <- F].
