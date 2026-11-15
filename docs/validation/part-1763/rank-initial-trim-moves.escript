-mode(compile).
main([Official, Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Rows = lists:append([rows(Official, Generated, P) || P <- Paths]),
    io:format("~B functions in ~B modules move an unwritten Y slot between allocate and initial trim.~n",
        [length(Rows), length(lists:usort([P || {P,_,_,_,_} <- Rows]))]),
    NoFrame = [P || {P,_,_,_,false} <- Rows],
    io:format("~B such functions in ~B modules have no frame in OTP.~n",
        [length(NoFrame), length(lists:usort(NoFrame))]),
    [io:format("~s ~0p ~0p ~0p OTP frame: ~p~n",[P,F,M,T,R]) || {P,F,M,T,R} <- Rows].

rows(Official, Generated, P) ->
    Ref = read(filename:join(Official,P)),
    Current = read(filename:join(Generated,P)),
    [{P,F,M,T,has_frame(R)}
        || {F,Code} <- maps:to_list(Current), {ok,R} <- [maps:find(F,Ref)],
        {M,T} <- [initial_trim(Code)], M =/= []].

has_frame(Code) -> lists:keymember(allocate,1,Code) orelse lists:keymember(allocate_heap,1,Code).

initial_trim([{allocate,_,_}|Is]) -> after_allocate(Is, [], []);
initial_trim([{allocate_heap,_,_,_}|Is]) -> after_allocate(Is, [], []);
initial_trim([_|Is]) -> initial_trim(Is);
initial_trim([]) -> {[],none}.

after_allocate([{line,_}|Is], Defined, Moves) ->
    after_allocate(Is, Defined, Moves);
after_allocate([{init_yregs,{list,Killed}}|Is], Defined, Moves) ->
    after_allocate(Is, ordsets:union(ordsets:from_list(Killed),Defined), Moves);
after_allocate([{move,{y,_}=From,{y,_}=To}|Is], Defined, Moves) ->
    case ordsets:is_element(From,Defined) of
        true -> after_allocate(Is,ordsets:add_element(To,Defined),Moves);
        false -> after_allocate(Is,Defined,[{From,To}|Moves])
    end;
after_allocate([{trim,_,_}=T|_], _, Moves) -> {lists:reverse(Moves),T};
after_allocate(_,_,_) -> {[],none}.

read(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Is} || {function,N,A,_,Is} <- Fs]).
