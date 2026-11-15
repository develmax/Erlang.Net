-mode(compile).

main([Otp, Previous, Current, PathList]) ->
    {ok, Raw} = file:read_file(PathList),
    Paths = [binary_to_list(P) || P <- binary:split(Raw, <<10>>, [global]), P =/= <<>>],
    Rows = [{P, changes(filename:join(Otp,P),image(Previous,Otp,P),image(Current,Otp,P))} || P <- Paths],
    Selected = [{P,Fs} || {P,[_|_]=Fs} <- Rows],
    Flat = lists:append([Fs || {_,Fs} <- Selected]),
    Equal = [{P,Fs} || {P,Changes} <- Selected, Fs <- [[R || R={_,_,_,true} <- Changes]], Fs =/= []],
    io:format("~B functions in ~B modules use fewer Y slots. Total allocated slots decrease by ~B across those functions.~n",
        [length(Flat),length(Selected),lists:sum([B-C || {_,B,C,_} <- Flat])]),
    io:format("The first allocation now matches OTP in ~B functions in ~B modules.~n",
        [lists:sum([length(Fs)||{_,Fs}<-Equal]),length(Equal)]),
    [io:format("~s ~0p~n", [P, Fs]) || {P,Fs} <- Equal],
    ok.

changes(Otp,Previous,Current) ->
    R=read(Otp), B=read(Previous), C=read(Current),
    [{Key,Before,After, CurrentAlloc =:= maps:get(Key,R,undefined)} ||
      {Key,{_,After}=CurrentAlloc} <- maps:to_list(C),
      {ok,{_,Before}} <- [maps:find(Key,B)], After < Before].

read(P) ->
    case beam_disasm:file(P) of
        {beam_file,_,_,_,_,Fs} ->
            maps:from_list([{{N,A},{I,element(2,I)}} || {function,N,A,_,Is} <- Fs,
                I <- lists:sublist([Op || Op <- Is, is_tuple(Op),
                    lists:member(element(1,Op),[allocate,allocate_heap])],1)]);
        {error,beam_lib,{file_error,_,enoent}} -> #{}
    end.

image(Directory, Otp, Path) ->
    Candidate = filename:join(Directory,Path),
    case filelib:is_regular(Candidate) of
        true -> Candidate;
        false -> filename:join(Otp,Path)
    end.