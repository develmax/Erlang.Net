-module(beam_tagged_record_lookup).
-export([message/2]).
-record(state,{entries}).

message({event, Key, process, _, selected}, #state{entries=Entries}=State) ->
    case lookup(Key, 2, Entries) of
        {value,{Id,Key},Remaining} ->
            _ = case inspect(Id) of
                {ready,_} -> apply_update(Id);
                _ -> ignored
            end,
            {reply,State#state{entries=Remaining}};
        false ->
            {reply,State}
    end;
message({event, Key, process, _, _Other}, #state{entries=Entries}=State) ->
    {reply,State#state{entries=lists:keydelete(Key,2,Entries)}};
message(_Other, State) ->
    {reply,State}.

lookup(_, _, invalid) -> wrong_result;
lookup(_, _, inconsistent) -> {value,{unexpected,bad_key},[]};
lookup(Key, Index, Entries) -> lists:keytake(Key,Index,Entries).

inspect(Id) -> put(inspection,Id), inspect_result(Id).
inspect_result(enabled) -> {ready,now};
inspect_result(_) -> ignore.
apply_update(Id) -> put(applied,Id).
