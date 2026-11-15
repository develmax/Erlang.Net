-mode(compile).
main([Official, Generated]) ->
    Files = filelib:wildcard("**/*.beam", Generated),
    Rows = lists:append([rows(Official, Generated, F) || F <- Files]),
    Counts = lists:foldl(fun({P,_,_,Shape,_}, Acc) ->
        maps:update_with(Shape, fun({Paths,N}) -> {Paths#{P=>true},N+1} end, {#{P=>true},1}, Acc)
    end, #{}, Rows),
    io:format("Missing OTP inlined failure helpers, grouped by name origin, compiler options and caller kinds.~n"),
    [io:format("~B modules ~B helpers ~0p~n", [M,C,K]) ||
        {M,C,K} <- lists:reverse(lists:sort([{map_size(P),N,S} || {S,{P,N}} <- maps:to_list(Counts)]))],
    [io:format("~s ~p/~B ~0p ~0p~n", [Path,Name,Arity,Shape,Callers]) ||
        {Path,Name,Arity,Shape,Callers} <- Rows].

rows(Official, Generated, Path) ->
    Reference = filename:join(Official,Path),
    {beam_file,Module,_,_,_,Fs} = beam_disasm:file(Reference),
    {beam_file,_,_,_,_,Other} = beam_disasm:file(filename:join(Generated,Path)),
    Actual = maps:from_list([{{N,A},true} || {function,N,A,_,_} <- Other]),
    Missing = [{N,A} || {function,N,A,_,_} <- Fs, is_inlined(N), not maps:is_key({N,A},Actual)],
    case Missing of
        [] -> [];
        _ ->
            Options = compile_options(Reference),
            [begin
                Callers = [{F,A} || {function,F,A,_,Ops} <- Fs, {F,A} =/= {N,Arity},
                           lists:any(fun(Op) -> calls(Op, {Module,N,Arity}) end, Ops)],
                {Path,N,Arity,{kind(N),Options,length(Callers),caller_kind(Callers)},Callers}
             end || {N,Arity} <- Missing]
    end.

is_inlined(Name) ->
    string:find(atom_to_list(Name), "-inlined-") =/= nomatch.

kind(Name) ->
    case lists:prefix("-inlined-", atom_to_list(Name)) of
        true -> named_target;
        false -> ssa_generated
    end.

caller_kind([{Caller,_}]) ->
    case re:run(atom_to_binary(Caller), "-fun-[0-9]+-$", [{capture,none}]) of
        match -> anonymous;
        nomatch -> other
    end;
caller_kind(_) -> other.

calls({call,_,Target}, Target) -> true;
calls({call_only,_,Target}, Target) -> true;
calls({call_last,_,Target,_}, Target) -> true;
calls(_, _) -> false.

compile_options(Path) ->
    case beam_lib:chunks(Path,[abstract_code,compile_info]) of
        {ok,{_,[{abstract_code,{raw_abstract_v1,Forms}},{compile_info,Info}]}} ->
            Options = proplists:get_value(options,Info,[]) ++
                lists:flatten([case A of L when is_list(L) -> L; _ -> [A] end ||
                                   {attribute,_,compile,A} <- Forms]),
            case {lists:member(inline,Options),proplists:is_defined(inline,lists:filter(fun is_tuple/1,Options))} of
                {true,true} -> both;
                {true,false} -> automatic;
                {false,true} -> explicit;
                _ -> default
            end;
        _ -> unknown
    end.
