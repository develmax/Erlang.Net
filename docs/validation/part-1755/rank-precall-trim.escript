-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Rows = [{P,compare(filename:join(Official,P),filename:join(Generated,P))} || P <- Paths],
    Candidates = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    Counts = [otp_aligned(Cases) || {_,Fs} <- Candidates, {_,Cases} <- Fs],
    io:format("~B modules ~B functions with a foldable frame trim before an ordinary call, ~B frame sizes align with OTP's first frame.~n",
              [length(Candidates),length(Counts),lists:sum(Counts)]),
    [io:format("~s ~0p~n",[P,Fs]) || {P,Fs} <- Candidates],
    ok.

otp_aligned(Cases) ->
    length([ok || {_,_,true} <- Cases]).

compare(Reference,Generated) ->
    R=functions(Reference), G=functions(Generated),
    [{F,Matches} || {F,Code} <- maps:to_list(G), {ok,ROriginal} <- [maps:find(F,R)],
                    [Expected|_] <- [[Slots || I <- ROriginal, Slots <- frame(I)]],
                    Matches <- [[{Slots,Kept,Kept =:= Expected} || {Slots,Kept} <- search(Code)]],
                    Matches =/= []].

functions(P) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Code} || {function,N,A,_,Code} <- Fs]).

search([I|More]) ->
    case frame(I) of
        [Slots] ->
            case before_trim(More,Slots,[]) of
                {ok,Kept} -> [{Slots,Kept}|search(More)];
                none -> search(More)
            end;
        [] -> search(More)
    end;
search([]) -> [].

before_trim([{trim,Removed,Kept}|More],Slots,Used) when Removed > 0, Removed + Kept =:= Slots ->
    case lists:all(fun(Y) -> Y >= Removed end,Used) andalso ordinary_call(More) of
        true -> {ok,Kept};
        false -> none
    end;
before_trim([I|More],Slots,Used) ->
    case local(I) of
        true -> before_trim(More,Slots,stack_roots(I)++Used);
        false -> none
    end;
before_trim([],_,_) -> none.

ordinary_call([{Kind,_,_}|_]) when Kind =:= call; Kind =:= call_ext -> true;
ordinary_call([I|Rest]) -> local(I) andalso ordinary_call(Rest);
ordinary_call([]) -> false.

local(I) when is_tuple(I) ->
    lists:member(element(1,I),[init_yregs,move,swap,get_tuple_element,get_list,put_tuple2,put_list,line,test_heap]);
local(_) -> false.

stack_roots({init_yregs,_}) -> [];
stack_roots({literal,_}) -> [];
stack_roots({y,N}) -> [N];
stack_roots(T) when is_tuple(T) -> lists:append([stack_roots(I) || I <- tuple_to_list(T)]);
stack_roots(T) when is_list(T) -> lists:append([stack_roots(I) || I <- T]);
stack_roots(_) -> [].

frame({allocate,S,_}) -> [S];
frame({allocate_heap,S,_,_}) -> [S];
frame(_) -> [].
