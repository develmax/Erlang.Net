-module(self_message_token779).
-export([run/0,worker/2,append/2,grow/1]).

run() ->
    {observe(tokenless),observe(tokenful)}.

observe(Mode) ->
    erlang:seq_trace(sequential_trace_token,[]),
    Parent = self(),
    {Pid,Ref} = spawn_monitor(self_message_token779,worker,[Parent,Mode]),
    receive
        {Pid,Result} ->
            receive {'DOWN',Ref,process,Pid,normal} -> Result end;
        {'DOWN',Ref,process,Pid,Reason} -> {worker_down,Mode,Reason}
    end.

worker(Parent,Mode) ->
    erlang:seq_trace(sequential_trace_token,[]),
    Result = try sample(Mode)
        catch Class:Reason:Stack -> {caught,Mode,Class,Reason,Stack}
    end,
    erlang:seq_trace(sequential_trace_token,[]),
    Parent ! {self(),Result}.

sample(Mode) ->
    process_flag(message_queue_data,on_heap),
    A = append(<<>>,7),
    B = grow(A),
    true = erlang:garbage_collect(),
    true = erlang:garbage_collect(),
    Label = {token_label,self()},
    case Mode of
        tokenless -> ok;
        tokenful -> erlang:seq_trace(label,Label)
    end,
    Message = {self_message_token,B},
    Before = binary:referenced_byte_size(B),
    self() ! Message,
    receive Received = {self_message_token,C} ->
        SameTuple = erts_debug:same(Message,Received),
        SameBinary = erts_debug:same(B,C),
        LabelSame = case Mode of
            tokenless -> none;
            tokenful ->
                {label,ReceivedLabel} = erlang:seq_trace_info(label),
                erts_debug:same(Label,ReceivedLabel)
        end,
        AfterReferenced = binary:referenced_byte_size(B),
        Next = append(B,9),
        {Mode,Before,SameTuple,SameBinary,LabelSame,
         byte_size(B),byte_size(C),byte_size(Next),AfterReferenced,
         binary:referenced_byte_size(Next),B,C,Next}
    end.

append(B,N) -> <<B/binary,N>>.
grow(B) -> <<B/binary,0:4088>>.
