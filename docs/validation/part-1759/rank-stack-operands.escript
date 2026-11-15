-mode(compile).

main([Official,Generated]) ->
    Paths = filelib:wildcard("**/*.beam",Generated),
    Counts = lists:foldl(fun(P,Acc0) ->
        R=functions(filename:join(Official,P)),
        G=functions(filename:join(Generated,P)),
        lists:foldl(fun({F,Code},Acc) ->
            case maps:find(F,R) of
                {ok,Ref} when Ref =/= Code ->
                    case first_difference(Ref,Code) of
                        {allocate,slots,higher} ->
                            lists:foldl(fun(S,M) ->
                                maps:update_with(S,fun({Files,N}) -> {Files#{P=>true},N+1} end,
                                                 {#{P=>true},1},M)
                            end,Acc,[all|classify_code(Code)]);
                        _ -> Acc
                    end;
                _ -> Acc
            end
        end,Acc0,maps:to_list(G))
    end,#{},Paths),
    Ranked=lists:reverse(lists:sort([{map_size(Files),N,S} || {S,{Files,N}}<-maps:to_list(Counts)])),
    io:format("Structural features of functions whose first difference is a larger allocate frame with equal live X count. Rows overlap.~n"),
    [io:format("~B modules ~B functions ~0p~n",[M,N,S]) || {M,N,S} <- Ranked],
    ok.

classify_code(Code) ->
    Ops=[opcode(I) || I <- Code],
    Allocations=length([ok || Op <- Ops, lists:member(Op,[allocate,allocate_heap,allocate_zero,allocate_heap_zero])]),
    Flags=[{multiple_allocations,Allocations>1},
           {trim,lists:member(trim,Ops)},
           {exception_handler,lists:any(fun(Op) -> lists:member(Op,['try','catch']) end,Ops)},
           {receive_loop,lists:any(fun(Op) -> lists:member(Op,[loop_rec,wait,wait_timeout]) end,Ops)},
           {debug,lists:member(debug_line,Ops)},
           {ordinary_call,lists:any(fun(Op) -> lists:member(Op,[call,call_ext,call_fun,call_fun2,apply,send]) end,Ops)}],
    case present_shape(Flags) of
        {allocator_region,one_frame_no_trim_or_handler} ->
            [single_frame|lists:usort([{unsupported_y,opcode(I)} || I <- Code, has_y(I), not supported(I)])];
        Other -> [Other]
    end.

present_shape(Flags) ->
    case [Name || {Name,true} <- Flags, Name =/= ordinary_call] of
        [] -> {allocator_region,one_frame_no_trim_or_handler};
        [Name|_] -> {allocator_region,Name}
    end.
first_difference([I|R],[I|G]) -> first_difference(R,G);
first_difference([{allocate,S,R}|_],[{allocate,S,G}|_]) -> {allocate,live,order(R,G)};
first_difference([{allocate,R,L}|_],[{allocate,G,L}|_]) -> {allocate,slots,order(R,G)};
first_difference([{allocate,RS,RL}|_],[{allocate,GS,GL}|_]) ->
    {allocate,slots_and_live,order(RS,GS),order(RL,GL)};
first_difference([R|_],[G|_]) -> {opcode(R),opcode(G)};
first_difference([],[_|_]) -> extra;
first_difference([_|_],[]) -> shortened;
first_difference(undefined,_) -> added;
first_difference(_,undefined) -> missing.

order(R,G) when G > R -> higher;
order(R,G) when G < R -> lower.

opcode({test,Name,_,_}) -> Name;
opcode({test,Name,_,_,_}) -> Name;
opcode({test,Name,_,_,_,_}) -> Name;
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

has_y({y,N}) when is_integer(N) -> true;
has_y({literal,_}) -> false;
has_y({atom,_}) -> false;
has_y(T) when is_tuple(T) -> has_y(tuple_to_list(T));
has_y([H|T]) -> has_y(H) orelse has_y(T);
has_y(_) -> false.

supported({bif,_,_,_,_}) -> true;
supported({gc_bif,_,_,_,_,_}) -> true;
supported(I) ->
    lists:member(opcode(I),[move,get_hd,get_tl,make_fun3,put_list,get_tuple_element,put_tuple2,update_record,
                           get_list,badmatch,badrecord,case_end,swap,call_fun2,loop_rec,wait_timeout,init_yregs,
                           select_val,select_tuple_arity,is_lt,is_ge,is_eq,is_ne,is_eq_exact,is_ne_exact,
                           is_integer,is_float,is_number,is_atom,is_pid,is_reference,is_port,is_nil,is_binary,
                           is_bitstr,is_list,is_nonempty_list,is_tuple,test_arity,is_function,is_boolean,
                           is_function2,is_map,has_map_fields,is_tagged_tuple,is_any_native_record]).