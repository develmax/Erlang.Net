-module(proc_lib_remote_payload).
-export([maker/1, child/1]).

maker(Capture) ->
    true = register(maps:get(marker, Capture), self()),
    fun() -> child(Capture) end.

child(Capture) ->
    Parent = maps:get(parent, Capture),
    Marker = maps:get(marker, Capture),
    Label = maps:get(label, Capture),
    ok = proc_lib:set_label(Label),
    Snapshot = #{captured => Capture, node => node(),
                 initial_call => proc_lib:initial_call(self()),
                 raw_initial_call => get('$initial_call'),
                 ancestors => get('$ancestors'), group_leader => group_leader(),
                 links => process_info(self(), links),
                 label => proc_lib:get_label(self()), constructor_marker => whereis(Marker)},
    Parent ! {remote_ready, self(), Snapshot},
    receive
        {finish, Parent} -> ok
    after 15000 -> exit(payload_finish_timeout)
    end.
