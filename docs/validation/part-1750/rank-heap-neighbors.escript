-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam", Generated),
    ClassRows = [{P,classes(filename:join(Official,P), filename:join(Generated,P))}
                 || P <- Paths],
    Counts = lists:foldl(fun({P,Rows}, Acc0) ->
                  lists:foldl(fun({Class,F}, Acc) ->
                      maps:update_with(Class, fun({Modules,Funs,N}) ->
                          {Modules#{P=>true},Funs#{{P,F}=>true},N+1}
                      end, {#{P=>true},#{{P,F}=>true},1}, Acc)
                  end, Acc0, Rows)
              end, #{}, ClassRows),
    Ranked = lists:reverse(lists:sort([{map_size(Ms),map_size(Fs),N,C}
                                      || {C,{Ms,Fs,N}} <- maps:to_list(Counts)])),
    [io:format("~B modules ~B functions ~B sites ~0p~n",[M,F,N,C]) || {M,F,N,C} <- Ranked],
    ok.

classes(Origin,Generated) ->
    R = functions(Origin), C = functions(Generated),
    [{{Prev,After,RefAfter},F} || {F,CG} <- maps:to_list(C),
                  {ok,RG} <- [maps:find(F,R)],
                  [Sz || {Sz,_,_,_} <- RG] =:= [Sz || {Sz,_,_,_} <- CG],
                  {{_,Ref,_,RefAfter},{_,Cur,Prev,After}} <- lists:zip(RG,CG),
                  Ref < Cur].

functions(File) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(File),
    maps:from_list([{{Name,A},heaps(Code,none,[])} || {function,Name,A,_,Code} <- Fs]).

heaps([{test_heap,Size,Live}=I|Rest],Prev,Acc) ->
    After = case Rest of [Next|_] -> opcode(Next); [] -> none end,
    heaps(Rest,opcode(I),[{Size,Live,Prev,After}|Acc]);
heaps([I|Rest],_,Acc) ->
    heaps(Rest,opcode(I),Acc);
heaps([],_,Acc) ->
    lists:reverse(Acc).

opcode({test,Test,_,_}) -> Test;
opcode({bif,Name,_,_,_}) -> {bif,Name};
opcode({gc_bif,Name,_,_,_,_}) -> {gc_bif,Name};
opcode(I) when is_tuple(I) -> element(1,I);
opcode(I) -> I.
