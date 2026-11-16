-module(beam_trim_tagged_payload).
-export([run/2]).

run(Arg, Queue) ->
    {ok, Pid} = supervisor:start_child(example_sup, Arg),
    monitor(process, Pid),
    {Pid, queue:in(Pid, Queue)}.
