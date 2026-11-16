-mode(compile).
main([Official, Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    Totals = lists:foldl(fun(Path, Acc) ->
        Expected = read(filename:join(Official, Path)),
        Actual = read(filename:join(Generated, Path)),
        Rows = [{Key, Grade} || {Key, Code} <- maps:to_list(Actual),
                {ok, Want} <- [maps:find(Key, Expected)],
                Grade <- [class(Want, Code)], Grade =/= skip],
        lists:foldl(fun(Grade, A) ->
            Keys = [Key || {Key, G} <- Rows, G =:= Grade],
            maps:update_with(Grade,
                fun({Modules, Functions, Example}) ->
                    {Modules + 1, Functions + length(Keys), Example}
                end, {1, length(Keys), {Path, hd(Keys)}}, A)
        end, Acc, lists:usort([G || {_, G} <- Rows]))
    end, #{}, Paths),
    io:format("First frame divergence in ~B differing modules.~n",[length(Paths)]),
    Rows = lists:reverse(lists:sort([{M,F,G,E} || {G,{M,F,E}} <- maps:to_list(Totals)])),
    [io:format("~B modules, ~B functions: ~0p example ~0p~n",[M,F,G,E])
        || {M,F,G,E} <- lists:sublist(Rows,50)].

read(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name,Arity},[normalize(I) || I <- Code,
                   not lists:member(op(I),[label,line,func_info])]}
                   || {function,Name,Arity,_,Code} <- Functions]).
normalize({f,0}) -> {f,0};
normalize({f,_}) -> {f,local_target};
normalize({literal,_}=I) -> I;
normalize(T) when is_tuple(T) -> list_to_tuple([normalize(V) || V <- tuple_to_list(T)]);
normalize(List) when is_list(List) -> [normalize(V) || V <- List];
normalize(V) -> V.

class([I|Want],[I|Code]) -> class(Want,Code);
class([{allocate,Want,Live}|_],[{allocate,Have,Live}|Code]) when Have > Want ->
    {allocate, shape(Code)};
class([{allocate_heap,Want,_,Live}|_],[{allocate_heap,Have,_,Live}|Code]) when Have > Want ->
    {allocate_heap, shape(Code)};
class([{test,Kind,_,_}|_]=Want,[Alloc|Code]) ->
    case lists:member(op(Alloc),[allocate,allocate_heap]) of
        true ->
            LaterFrame = lists:any(fun(I) -> lists:member(op(I),[allocate,allocate_heap]) end, Want),
            {early_frame,Kind,LaterFrame,front(skip_setup(Code))};
        false -> skip
    end;
class(_,_) -> skip.

skip_setup([{move,{x,_},{y,_}}|T]) -> skip_setup(T);
skip_setup([{init_yregs,_}|T]) -> skip_setup(T);
skip_setup(Code) -> Code.
front([{test,K,_,_}|_]) -> K;
front([{move,{y,_},{x,_}}|_]) -> reload;
front([I|_]) -> op(I);
front([]) -> empty.
op(T) when is_tuple(T) -> element(1,T);
op(V) -> V.

shape(Code) ->
    Ops = [op(I) || I <- Code],
    case lists:any(fun(K) -> lists:member(K,Ops) end, ['try','catch','try_end','catch_end']) of
        true -> handler;
        false ->
            case lists:any(fun(K) -> lists:member(K,Ops) end, [loop_rec,wait,wait_timeout,remove_message]) of
                true -> receive_block;
                false -> ordinary_shape(Code, Ops)
            end
    end.
ordinary_shape(Code, Ops) ->
    {case lists:any(fun has_local_target/1, Code) of true -> branching; false -> straight end,
     lists:member(trim,Ops), lists:member(allocate,Ops) orelse lists:member(allocate_heap,Ops)}.
has_local_target({f,0}) -> false;
has_local_target({f,local_target}) -> true;
has_local_target({literal,_}) -> false;
has_local_target(I) when is_tuple(I) -> lists:any(fun has_local_target/1, tuple_to_list(I));
has_local_target(Is) when is_list(Is) -> lists:any(fun has_local_target/1, Is);
has_local_target(_) -> false.