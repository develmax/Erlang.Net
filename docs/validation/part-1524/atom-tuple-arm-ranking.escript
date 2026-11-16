#!/usr/bin/env escript
-mode(compile).
main([Root,Generated,PathFile]) ->
    {ok, Content} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Line) || Line <- binary:split(Content, <<10>>, [global]), Line =/= <<>>],
Norm=fun(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L}<-Is],lists:seq(1,length([L || {label,L}<-Is])))),
    Fix=fun F({label,L})->{label,maps:get(L,Labels,L)};
        F({f,L})->{f,maps:get(L,Labels,L)};
        F({literal,V})->{literal,V};
        F({make_fun3,M,I,_,Dst,Args})->{make_fun3,M,I,uniq,F(Dst),F(Args)};
        F(T) when is_tuple(T)->list_to_tuple([F(X)||X<-tuple_to_list(T)]);
        F([H|T])->[F(H)|F(T)];
        F(V)->V
    end,
    [Fix(I)||I<-Is]
end,
Read=fun(P)->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Norm(Is)}||{function,N,A,_,Is}<-Fs])
end,
    Findings = lists:flatmap(fun(Path) ->
        OtpPath=filename:join(Root,Path),
        Original=Read(OtpPath),
        Candidate=Read(filename:join(Generated,Path)),
        case beam_lib:chunks(OtpPath,[abstract_code]) of
            {ok,{_,[{abstract_code,{raw_abstract_v1,Forms}}]}} ->
                [{Path,Key,{classify(Clauses),End}} ||
                    {function,_,Name,Arity,Clauses} <- Forms,
                    Key <- [{Name,Arity}],
                    {Frame,End} <- [frame_difference(maps:get(Key,Original,[]),maps:get(Key,Candidate,[]))],
                    Frame =:= true,
                    atom_tuple_remote_case(Clauses)];
            _ -> []
        end
    end,Paths),
    Groups=lists:foldl(fun({Path,Key,Shape},Acc) ->
        maps:update_with(Shape,fun(Group)->[{Path,Key}|Group] end,[{Path,Key}],Acc)
    end,#{},Findings),
    Ranked=lists:reverse(lists:sort([
        {length(lists:usort([Path||{Path,_}<-Group])),length(Group),Shape,lists:sublist(Group,3)}
        || {Shape,Group}<-maps:to_list(Groups)])),
    io:format("~B modules with abstract code scanned; ~B function candidates~n",
        [length(lists:usort([P||{P,_,_}<-Findings])),length(Findings)]),
    lists:foreach(fun({Modules,Functions,Shape,Examples})->
        io:format("~B modules, ~B functions: ~0p ~0p~n",[Modules,Functions,Shape,Examples])
    end,lists:sublist(Ranked,30)).

frame_difference(Original,Candidate) ->
    case first_difference(Original,Candidate) of
        {{allocate,Before,Live},{allocate,After,Live}} when After > Before ->
            {true,case Before of 0->zero_frame; _->nonzero_frame end};
        _ -> {false,none}
    end.

classify([{clause,_,Args,Guards,Body}]) ->
    Parameters = case lists:all(fun(A)->element(1,A)=:=var end,Args) of true->variables; false->pattern end,
    {single,Parameters,if Guards =:= [] -> unguarded; true -> guarded end,body(Body)};
classify(Clauses) ->
    {multiple,case lists:any(fun({clause,_,_,Guards,_})->Guards=/=[] end,Clauses) of true->guarded; false->unguarded end,
     length(Clauses)}.

atom_tuple_remote_case([{clause,_,_,[],[{'case',_,{call,_,{remote,_,{atom,_,_},{atom,_,_}},_},Arms}]}]) ->
    lists:usort([pattern_kind(P) || {clause,_,[P],[],_} <- Arms]) =:= [atom,tuple] andalso
        lists:all(fun({clause,_,_,G,_}) -> G =:= [] end, Arms);
atom_tuple_remote_case(_) -> false.

body([{'case',_,_,Arms}]) ->
    {atom_and_tuple_arms, lists:usort([{pattern_kind(P), branch(Body)} || {clause,_,[P],_,Body} <- Arms])};
body([Only]) -> {one,expression(Only)};
body([First|Rest]) -> {sequence,expression(First),expression(lists:last(Rest))}.

expression({'case',_,Value,Arms}) ->
    {'case',expression(Value),lists:usort([pattern_kind(Pattern) || {clause,_,[Pattern],_,_} <- Arms]),
     case lists:any(fun({clause,_,_,Guards,_}) -> Guards =/= [] end, Arms) of true -> guarded; false -> unguarded end};
expression({call,_,{remote,_,{atom,_,_},{atom,_,_}},_}) -> remote_call;
expression({call,_,{atom,_,_},_}) -> local_call;
expression({match,_,L,R}) -> {match,element(1,L),expression(R)};
expression({op,_,Op,_,_}) -> {binary,Op};
expression(Expr) -> element(1,Expr).

branch([Only]) -> expression(Only);
branch([First|Rest]) -> {sequence,expression(First),expression(lists:last(Rest))}.

pattern_kind({match,_,_,_}) -> alias;
pattern_kind({var,_,'_'}) -> wildcard;
pattern_kind(Pattern) -> element(1,Pattern).

first_difference([I|Expected],[I|Actual]) -> first_difference(Expected,Actual);
first_difference([E|_],[A|_]) -> {E,A};
first_difference([], [A|_]) -> {end_of_function,A};
first_difference([E|_], []) -> {E,end_of_function};
first_difference([],[]) -> same.


