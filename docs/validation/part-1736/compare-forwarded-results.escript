-mode(compile).

main([Root, Before, After, PathList]) ->
    {ok, Raw} = file:read_file(PathList),
    Paths = [binary_to_list(P) || P <- binary:split(Raw, <<10>>, [global]), P =/= <<>>],
    Data = [{P, compare(read(Before,Root,P), read(After,Root,P), read(Root,Root,P))} || P <- Paths],
    Changed = [{P,Fs} || {P,[_|_]=Fs} <- Data],
    Moves = [M || {_, Fs} <- Changed, {_, Ms} <- Fs, M <- Ms],
    io:format("~B result saves before branches disappear in ~B functions across ~B modules with the same previous and current sites.~n",
        [length(Moves),lists:sum([length(Fs) || {_,Fs} <- Changed]),length(Changed)]),
    io:format("~B also omit the save in OTP functions with the same call and branch sequence.~n",
        [length([M || {_,false}=M <- Moves])]),
    Groups = lists:foldl(fun({K,_},A) -> maps:update_with(K,fun(N)->N+1 end,1,A) end,#{},Moves),
    [io:format("~p ~B~n",[K,N]) || {N,K} <- lists:reverse(lists:sort([{N,K} || {K,N} <- maps:to_list(Groups)]))].

read(Directory, Root, P) ->
    Candidate = filename:join(Directory,P),
    Actual = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Actual),
    maps:from_list([{{N,A},scan([I || I <- Code, not lists:member(op(I),[line,init_yregs])])}
                    || {function,N,A,_,Code} <- Functions]).

scan([Call,{move,{x,0},{y,_}},Next|T]=Code) ->
    case ordinary(Call) andalso branch(Next) of
        true -> [{{Call,branch_kind(Next)},true}|scan([Next|T])];
        false -> scan(tl(Code))
    end;
scan([Call,Next|T]=Code) ->
    case ordinary(Call) andalso branch(Next) of
        true -> [{{Call,branch_kind(Next)},false}|scan([Next|T])];
        false -> scan(tl(Code))
    end;
scan(_) -> [].

ordinary(I) -> lists:member(op(I),[call,call_ext,apply,call_fun,call_fun2]).
branch({test,_,_,_}) -> true;
branch(I) -> lists:member(op(I),[select_val,select_tuple_arity]).
branch_kind({test,K,_,_}) -> K;
branch_kind(I) -> op(I).
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
                Moves = [{K,Expected} || {{{Call,K},true},{{Call,K},false},Expected} <- lists:zip3(Old,New,RefFlags)],
                case Moves of [] -> Acc; _ -> [{Key,Moves}|Acc] end;
            false -> Acc
        end
    end,[],After).
sites(F) -> [S || {S,_} <- F].
