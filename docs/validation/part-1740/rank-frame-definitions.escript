-mode(compile).

main([Root, Candidate, PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Equal = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Different = filelib:wildcard("**/*.beam",Candidate),
    report(equal,Root,Equal),
    report(release_different,Root,Different),
    report(generated_different,Candidate,Different).

report(Group,Root,Paths) ->
    Counts = lists:foldl(fun(P, Acc) -> merge(scan(filename:join(Root,P)),Acc) end,#{},Paths),
    io:format("~p: ~B images.~n",[Group,length(Paths)]),
    [io:format("~B modules, ~B functions, ~B slots: ~p~n",[M,F,N,K])
        || {M,F,N,K} <- lists:reverse(lists:sort([{M,F,N,K} || {K,{M,F,N}} <- maps:to_list(Counts)]))].

merge(Funcs, Counts) ->
    Groups = lists:usort([G || {G,_} <- Funcs]),
    lists:foldl(fun(G,Acc) ->
        Matches = [N || {K,N} <- Funcs, K =:= G],
        F = length(Matches),
        N = lists:sum(Matches),
        maps:update_with(G,fun({M,T,S})->{M+1,T+F,S+N} end,{1,F,N},Acc)
    end,Counts,Groups).

scan(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    lists:append([maps:to_list(frames(Is,#{})) || {function,_,_,_,Is} <- Functions]).
frames([{allocate,_,_}|T], Counts) -> frame(T, Counts);
frames([{allocate_heap,_,_,_}|T], Counts) -> frame(T, Counts);
frames([_|T], Counts) -> frames(T,Counts);
frames([], Counts) -> Counts.

frame(Instructions, Counts) ->
    {Rest, Next} = setup(Instructions,[],Counts),
    frames(Rest,Next).
setup([{init_yregs,{list,Slots}}|T], Pending, Counts) ->
    setup(T,ordsets:union(Pending,ordsets:from_list(Slots)),Counts);
setup([{line,_}|T],Pending,Counts) ->
    setup(T,Pending,Counts);
setup([I|T]=Rest, Pending, Counts) ->
    case reads_writes(I) of
        {Kind, Src, Dst} ->
            Unread = ordsets:subtract(Pending,yregs(Src)),
            Writes = yregs(Dst),
            Number = length(ordsets:intersection(Unread,Writes)),
            Next = case Number of
                0 -> Counts;
                _ -> maps:update_with(Kind,fun(N)->N+Number end,Number,Counts)
            end,
            setup(T,ordsets:subtract(Unread,Writes),Next);
        stop -> {Rest,Counts}
    end;
setup([], _, Counts) -> {[],Counts}.

reads_writes({move,S,D}) ->
    Kind = case plain(S) of {x,_}->move_x; {y,_}->move_y; _->move_literal end,
    {Kind,[S],[D]};
reads_writes({get_tuple_element,S,_,D}) -> {get_tuple_element,[S],[D]};
reads_writes({get_list,S,H,T}) -> {get_list,[S],[H,T]};
reads_writes({get_head,S,D}) -> {get_hd,[S],[D]};
reads_writes({get_tail,S,D}) -> {get_tl,[S],[D]};
reads_writes(_) -> stop.
yregs(Vs) -> ordsets:from_list([Y || V <- Vs, {y,_}=Y <- [plain(V)]]).
plain({tr,R,_}) -> R;
plain(R) -> R.
