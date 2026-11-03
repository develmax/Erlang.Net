-module(mailbox_gc_root_observer).
-export([run/0]).
run() ->
    process_flag(message_queue_data, on_heap),
    fill(40),
    Before = process_info(self(), messages),
    On = process_info(self(), message_queue_data),
    on_heap = process_flag(message_queue_data, off_heap),
    true = erlang:garbage_collect(),
    Middle = process_info(self(), messages),
    Off = process_info(self(), message_queue_data),
    off_heap = process_flag(message_queue_data, on_heap),
    true = erlang:garbage_collect(),
    After = process_info(self(), messages),
    OnAgain = process_info(self(), message_queue_data),
    Drained = drain(40),
    Empty = process_info(self(), messages),
    {Before,On,Middle,Off,After,OnAgain,Drained,Empty}.
fill(0) -> ok;
fill(N) -> self() ! {item,N,<<N:16>>}, fill(N-1).
drain(0) -> [];
drain(N) -> receive Value -> [Value|drain(N-1)] end.
