-mode(compile).
main([Before,After,PathsFile]) ->
    {ok,Data} = file:read_file(PathsFile),
    Paths = [binary_to_list(P) || P <- binary:split(Data,<<10>>,[global]), P =/= <<>>],
    Counts = [orphans(Before,After,P) || P <- Paths],
    [io:format("~s ~0p~n",[P,Miss]) || {_,P,Miss} <- Counts, Miss =/= []],
    io:format("~B new LC helpers in ~B images checked. ~B have no caller other than themselves.~n",
        [lists:sum([N || {N,_,_}<-Counts]),length(Paths),lists:sum([length(M) || {_,_,M}<-Counts])]).

orphans(Before,After,P) ->
    O = read(filename:join(Before,P)),
    N = read(filename:join(After,P)),
    New = [F || {Name,_}=F <- maps:keys(N), is_helper(Name), not maps:is_key(F,O)],
    T = lists:foldl(fun({F,Code},Acc) ->
        lists:foldl(fun(I,A) ->
            case target(I) of
                Target when Target =/= F -> A#{Target=>true};
                _ -> A
            end
        end,Acc,Code)
    end,#{},maps:to_list(N)),
    {length(New),P,[F || F<-New, not maps:is_key(F,T)]}.

target({call,A,{_,F,A}}) -> {F,A};
target({call_only,A,{_,F,A}}) -> {F,A};
target({call_last,A,{_,F,A},_}) -> {F,A};
target(_) -> none.

is_helper(Name) ->
    re:run(atom_to_binary(Name), "^-.*-lc[$].[0-9]+/[0-9]+-[0-9]+-$", [{capture,none}]) =:= match.
read(Path) ->
    case filelib:is_regular(Path) of
        true ->
            {beam_file,_,_,_,_,Fs}=beam_disasm:file(Path),
            maps:from_list([{{Name,A},Code} || {function,Name,A,_,Code}<-Fs]);
        false -> #{}
    end.
