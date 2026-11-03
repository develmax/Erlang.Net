-module(monitor_original645).
-export([observe/1,capture/1]).
observe(Mode) ->
  Expected=get(stored),
  try arm(),case Mode of down -> take(Expected); cancel -> cancel() end of
    V -> {value,V}
  catch C:R:S -> {exception,C,R,S}
  end.
arm() ->
  Child=spawn(fun() -> receive go -> R=erts_internal:counters_new(1),exit({reason,R}) end end),
  Tag={payload,[#{resource=>get(stored)}]},
  Ref=erlang:monitor(process,Child,[{tag,Tag}]),
  put(child645,Child),put(ref645,Ref),erase(stored),erlang:garbage_collect(),Ref.
take(Expected) ->
  get(child645)!go,
  receive
    {{payload,[#{resource:=P}]},Ref,process,Child,{reason,R}} ->
      Ref=erase(ref645),Child=erase(child645),
      true=is_reference(P),true=is_reference(R),true=(P=:=Expected),true=(R=/=Expected),3=erts_debug:flat_size(R),true
  after 5000 -> erlang:error(missing_down645)
  end.
cancel() ->
  Ref=erase(ref645),Child=erase(child645),true=erlang:demonitor(Ref,[flush]),
  Joined=erlang:monitor(process,Child),exit(Child,kill),
  receive
    {'DOWN',Joined,process,Child,killed} -> true;
    {'DOWN',Joined,process,Child,noproc} -> true
  after 5000 -> erlang:error(child_cleanup645)
  end.
make_tag(atomics) -> atomics:new(1,[]);
make_tag(counters) -> erts_internal:counters_new(1);
make_tag(regex) -> {ok,Pattern}=re:compile("x"),element(5,Pattern);
make_tag(binary) -> element(2,binary:compile_pattern(<<"x">>));
make_tag(ordinary_reference_control) -> make_ref().
original_case(Family,Mode) ->
  Parent=self(),
  {Pid,Ref}=spawn_monitor(fun() -> put(stored,make_tag(Family)),Parent!{self(),observe(Mode)} end),
  receive
    {Pid,Value} -> receive {'DOWN',Ref,process,Pid,normal} -> Value end;
    {'DOWN',Ref,process,Pid,Reason} -> {worker_exit,Reason}
  after 15000 -> exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,{worker_timeout,Family,Mode}
  end.
capture(Directory) ->
  Rows=[{F,M,original_case(F,M)} || F <- [atomics,counters,regex,binary,ordinary_reference_control],M <- [down,cancel]],
  lists:foreach(fun({I,Row})->ok=file:write_file(filename:join(Directory,"outcome-"++integer_to_list(I)++".etf"),term_to_binary(Row,[{minor_version,2}])) end,lists:zip(lists:seq(0,9),Rows)),
  ok=file:write_file(filename:join(Directory,"all-outcomes.etf"),term_to_binary(Rows,[{minor_version,2}])),
  case lists:all(fun({_,_,{value,true}})->true;(_)->false end,Rows) of true->ok;false->erlang:error(observer_outcome645) end.
