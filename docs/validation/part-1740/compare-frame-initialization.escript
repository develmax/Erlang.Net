-mode(compile).
main([Root, Before, After, PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Values = [{read(Before,Root,P),read(After,Root,P)} || P <- Paths],
    Groups = lists:sum([B-A || {{B,_},{A,_}} <- Values]),
    Slots = lists:sum([B-A || {{_,B},{_,A}} <- Values]),
    io:format("~B fewer init_yregs instructions and ~B fewer slot clears in ~B changed images.~n",
              [Groups,Slots,length(Paths)]).

read(Directory, Root, P) ->
    Candidate = filename:join(Directory,P),
    Actual = case filelib:is_regular(Candidate) of true -> Candidate; false -> filename:join(Root,P) end,
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(Actual),
    Counts = [length(Y) || {function,_,_,_,Code} <- Fs, {init_yregs,{list,Y}} <- Code],
    {length(Counts),lists:sum(Counts)}.
