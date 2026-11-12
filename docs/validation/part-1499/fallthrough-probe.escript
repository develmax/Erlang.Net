#!/usr/bin/env escript
-mode(compile).
main([Root, Prior, Current, Case]) ->
    {Path, Module, Function, Arity, Args} = scenario(Case),
    Ref = filename:join(Root,Path),
    Old = filename:join(Prior,Path),
    New = filename:join(Current,Path),
    lists:foreach(fun({Tag,File}) -> disasm(Tag,File,Function,Arity) end,
        [{otp,Ref},{previous,Old},{current,New}]),
    R = execute(Ref,Module,Function,Args),
    O = execute(Old,Module,Function,Args),
    N = execute(New,Module,Function,Args),
    io:format("~B observations, previous vs OTP ~B differences, current vs OTP ~B differences, previous vs current ~B differences~n",
        [length(R),diff(R,O),diff(R,N),diff(O,N)]),
    O = N.

scenario("core_lib") ->
    X=cerl:c_var(x), Y=cerl:c_var(y), L=cerl:c_atom(ok),
    Basic=[X,Y,L,cerl:c_int(17),cerl:c_nil()],
    More=[cerl:c_cons(A,B) || A <- Basic, B <- Basic]
         ++ [cerl:c_seq(A,B) || A <- Basic, B <- Basic]
         ++ [cerl:c_let([A],B,C) || A <- [X,Y], B <- Basic, C <- Basic]
         ++ [cerl:c_fun([A],B) || A <- [X,Y], B <- Basic]
         ++ [cerl:c_call(A,B,[C]) || A <- Basic, B <- Basic, C <- Basic]
         ++ [cerl:c_catch(A) || A <- Basic]
         ++ [cerl:c_try(A,[X],B,[X,Y,cerl:c_var(z)],C) || A <- Basic, B <- Basic, C <- Basic],
    Args=[[V,E] || V <- [x,y,z], E <- Basic++More++[cerl:c_tuple(More),cerl:c_values(More)]],
    {"lib/compiler-10.0.6/ebin/core_lib.beam",core_lib,is_var_used,2,Args};
scenario("edlin_key") ->
    Alphabet=[0,8,9,27,31,32,59,64,65,79,91,97,126,127,255],
    Chars=[[]]++[[X] || X <- Alphabet]++[[X,Y] || X <- Alphabet,Y <- Alphabet]
         ++ [[27,91,X,Y] || X <- "0129;", Y <- Alphabet],
    Modes=[none,meta,meta_meta,meta_o,meta_csi,meta_left_sq_bracket,{csi,[]},{csi,"1"},
           {csi,";1"},{finished,[27,91,65]},{invalid,[27,0]}],
    Args=[[Chars0,Mode] || Chars0 <- Chars, Mode <- Modes],
    {"lib/stdlib-8.1/ebin/edlin_key.beam",edlin_key,get_valid_escape_key,2,Args}.

disasm(Tag,File,Function,Arity) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(File),
    Name = case Function of is_var_used -> vu_expr; _ -> Function end,
    [Code]=[C || {function,N,A,_,C} <- Fs,N=:=Name,A=:=Arity],
    Returns=returns(Code),
    Keys=[K || {_,K} <- Returns],
    Jumps=[L || {jump,{f,L}} <- Code, lists:keymember(L,1,Returns)],
    io:format("~p ~p/~B: ~B excess return blocks, ~B jumps to return blocks~n",
       [Tag,Name,Arity,length(Keys)-length(lists:usort(Keys)),length(Jumps)]).

returns([{label,L},{move,S,{x,0}},{deallocate,N},return|Tail]) -> [{L,{S,N}}|returns(Tail)];
returns([{label,L},{deallocate,N},return|Tail]) -> [{L,{{x,0},N}}|returns(Tail)];
returns([_|Tail]) -> returns(Tail);
returns([]) -> [].

execute(File,Module,F,Args) ->
    code:unstick_mod(Module),
    code:purge(Module),
    {ok,Bin}=file:read_file(File),
    {module,Module}=code:load_binary(Module,File,Bin),
    [try apply(Module,F,A) of V -> {ok,V} catch C:E -> {C,E} end || A <- Args].

diff(L,R) -> length([ok || {A,B} <- lists:zip(L,R),A=/=B]).