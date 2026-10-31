-module(group_leader_root_observer).
-export([run/0,child/1]).
run() ->
    Before = group_leader(),
    E = binary_to_term(<<131,88,119,24,103,114,111,117,112,95,108,101,97,100,101,114,95,114,111,111,116,64,114,101,109,111,116,101,85,102,119,136,17,34,51,68,18,52,86,120>>),
    try
        true = group_leader(E,self()),
        Current = group_leader(),
        Own = process_info(self(),group_leader),
        {Pid,Ref} = spawn_monitor(group_leader_root_observer,child,[self()]),
        Child = receive {Pid,Value} -> Value end,
        receive {'DOWN',Ref,process,Pid,normal} -> ok end,
        {E,Current,Own,Child}
    after
        true = group_leader(Before,self())
    end.
child(Parent) -> Parent ! {self(),group_leader()}.
