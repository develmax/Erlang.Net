%% Focused original OTP deletion oracle. No library implementation is replaced.
-module(prim_file_delete_cases).
-include_lib("kernel/include/file.hrl").
-export([run_worker/3,error_worker/2]).

run_worker(Parent,Tag,Dir) ->
    Existing = filename:join(Dir,"existing"),
    Missing = filename:join(Dir,"missing"),
    Folder = filename:join(Dir,"folder"),
    Unicode = filename:join(Dir,[16#416,16#65e5]),
    ReadOnly = filename:join(Dir,"readonly"),
    ok = file:write_file(Existing,<<1,2,3>>),
    ok = file:make_dir(Folder),
    ok = file:write_file(Unicode,<<4>>),
    ok = file:write_file(ReadOnly,<<5>>),
    ok = file:write_file_info(ReadOnly,#file_info{mode=8#444}),
    Rows = [{existing,prim_file:delete(Existing)},
            {second_delete,prim_file:delete(Existing)},
            {missing,prim_file:delete(Missing)},
            {directory,prim_file:delete(Folder)},
            {unicode,prim_file:delete(Unicode)},
            {readonly,prim_file:delete(ReadOnly)}],
    _ = file:write_file_info(ReadOnly,#file_info{mode=8#666}),
    _ = file:delete(ReadOnly),
    ok = file:del_dir(Folder),
    Pairs = [race(Dir,I) || I <- lists:seq(1,32)],
    ok = file:write_file(filename:join(Dir,"deletion-races.etf"),term_to_binary(Pairs)),
    Parent ! {Tag,Rows}.

error_worker(Parent,Tag) -> Parent ! {Tag,[]}.

race(Dir,I) ->
    F = filename:join(Dir,"race-" ++ integer_to_list(I)),
    ok = file:write_file(F,<<1>>),
    Parent = self(), Tag = make_ref(),
    Workers = [spawn(fun() ->
        Parent ! {Tag,ready,self()},
        receive {Tag,go} -> Parent ! {Tag,result,prim_file:delete(F)} end
    end) || _ <- [1,2]],
    [receive {Tag,ready,_} -> ok end || _ <- Workers],
    [P ! {Tag,go} || P <- Workers],
    [receive {Tag,result,R} -> R end || _ <- Workers].
