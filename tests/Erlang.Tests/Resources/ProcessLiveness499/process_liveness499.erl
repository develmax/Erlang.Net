-module(process_liveness499).
-export([run/0,run_stacks/0,probe/1]).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[case_0,case_1,case_2,case_3,case_4,case_5,case_6,case_7,case_8,case_9,case_10,case_11,case_12,case_13,case_14,case_15,case_16,case_17,case_18,case_19,case_20,case_21,case_22,case_23,case_24,case_25,case_26,case_27,case_28,case_29,case_30,case_31,case_32,case_33,case_34,case_35,case_36,case_37,case_38,case_39,case_40,case_41,case_42,case_43,case_44,case_45,case_46,case_47,case_48,case_49,case_50,case_51,case_52,case_53,case_54,case_55,case_56,case_57,case_58,case_59,case_60,case_61,case_62,case_63,case_64].
probe(case_0)->erlang:apply(erlang,opaque(is_process_alive),[opaque(raw)]);
probe(case_1)->erlang:apply(erlang,opaque(is_process_alive),[opaque(true)]);
probe(case_2)->erlang:apply(erlang,opaque(is_process_alive),[opaque(false)]);
probe(case_3)->erlang:apply(erlang,opaque(is_process_alive),[opaque(undefined)]);
probe(case_4)->erlang:apply(erlang,opaque(is_process_alive),[opaque('модуль')]);
probe(case_5)->erlang:apply(erlang,opaque(is_process_alive),[opaque('')]);
probe(case_6)->erlang:apply(erlang,opaque(is_process_alive),[opaque(0)]);
probe(case_7)->erlang:apply(erlang,opaque(is_process_alive),[opaque(-1)]);
probe(case_8)->erlang:apply(erlang,opaque(is_process_alive),[opaque(1 bsl 128)]);
probe(case_9)->erlang:apply(erlang,opaque(is_process_alive),[opaque(0.0)]);
probe(case_10)->erlang:apply(erlang,opaque(is_process_alive),[opaque(1.5)]);
probe(case_11)->erlang:apply(erlang,opaque(is_process_alive),[opaque([])]);
probe(case_12)->erlang:apply(erlang,opaque(is_process_alive),[opaque([a])]);
probe(case_13)->erlang:apply(erlang,opaque(is_process_alive),[opaque([a|tail])]);
probe(case_14)->erlang:apply(erlang,opaque(is_process_alive),[opaque({})]);
probe(case_15)->erlang:apply(erlang,opaque(is_process_alive),[opaque({a,b})]);
probe(case_16)->erlang:apply(erlang,opaque(is_process_alive),[opaque(#{})]);
probe(case_17)->erlang:apply(erlang,opaque(is_process_alive),[opaque(#{a=>b})]);
probe(case_18)->erlang:apply(erlang,opaque(is_process_alive),[opaque(<<>>)]);
probe(case_19)->erlang:apply(erlang,opaque(is_process_alive),[opaque(<<1,2>>)]);
probe(case_20)->erlang:apply(erlang,opaque(is_process_alive),[opaque(<<1:1>>)]);
probe(case_21)->erlang:apply(erlang,opaque(is_process_alive),[opaque(fun erlang:abs/1)]);
probe(case_22)->erlang:apply(erlang,opaque(is_process_alive),[opaque(fixed_ref())]);
probe(case_23)->erlang:apply(erlang,opaque(is_process_alive),[opaque(remote_pid())]);
probe(case_24)->erlang:apply(erlang,opaque(is_process_alive),[opaque(remote_pid_old())]);
probe(case_25)->erlang:apply(erlang,opaque(is_process_alive),[opaque(old_local_pid())]);
probe(case_26)->erlang:apply(erlang,opaque(is_process_alive),[opaque(missing_local_pid())]);
probe(case_27)->caller(raw);
probe(case_28)->caller(true);
probe(case_29)->caller(false);
probe(case_30)->caller(undefined);
probe(case_31)->caller('модуль');
probe(case_32)->caller('');
probe(case_33)->caller(0);
probe(case_34)->caller(-1);
probe(case_35)->caller(1 bsl 128);
probe(case_36)->caller(0.0);
probe(case_37)->caller(1.5);
probe(case_38)->caller([]);
probe(case_39)->caller([a]);
probe(case_40)->caller([a|tail]);
probe(case_41)->caller({});
probe(case_42)->caller({a,b});
probe(case_43)->caller(#{});
probe(case_44)->caller(#{a=>b});
probe(case_45)->caller(<<>>);
probe(case_46)->caller(<<1,2>>);
probe(case_47)->caller(<<1:1>>);
probe(case_48)->caller(fun erlang:abs/1);
probe(case_49)->caller(fixed_ref());
probe(case_50)->caller(remote_pid());
probe(case_51)->erlang:apply(erlang,opaque(is_alive),[]);
probe(case_52)->{erlang:is_alive(),erlang:is_alive()};
probe(case_53)->erlang:apply(erlang,opaque(is_process_alive),[self()]);
probe(case_54)->lifecycle(kill);
probe(case_55)->lifecycle(boom);
probe(case_56)->lifecycle(normal);
probe(case_57)->dynamic_flag(false);
probe(case_58)->dynamic_flag(true);
probe(case_59)->dynamic_flag(other);
probe(case_60)->dynamic_flag([]);
probe(case_61)->dynamic_flag(0);
probe(case_62)->dynamic_flag(#{});
probe(case_63)->dynamic_flag(<<>>);
probe(case_64)->dynamic_flag(fun erlang:abs/1).

opaque(V)->put(process_liveness499_input,V),erase(process_liveness499_input).
caller(V)->R=erlang:apply(erlang,opaque(is_process_alive),[opaque(V)]),{continued,R}.
remote_pid()->binary_to_term(<<131,88,119,13,"external@host",1:32,0:32,0:32>>).
remote_pid_old()->binary_to_term(<<131,103,119,13,"external@host",1:32,0:32,1:8>>).
old_local_pid()->binary_to_term(<<131,88,119,13,"nonode@nohost",1:32,0:32,42:32>>).
missing_local_pid()->list_to_pid("<0.32767.0>").
fixed_ref()->binary_to_term(<<131,90,3:16,119,13,"external@host",0:32,1:32,2:32,3:32>>).
lifecycle(Reason)->
 Parent=self(),
 {Pid,Ref}=spawn_monitor(fun()->Parent!{ready,self()},receive stop->ok end end),
 try
  receive {ready,Pid}->ok end,
  Before=erlang:is_process_alive(Pid),
  true=erlang:exit(Pid,Reason),
  After=erlang:is_process_alive(Pid),
  erlang:exit(Pid,kill),
  receive {'DOWN',Ref,process,Pid,_}->ok end,
  {Before,After,erlang:is_process_alive(Pid)}
 after erlang:exit(Pid,kill),erlang:demonitor(Ref,[flush]) end.
dynamic_flag(Value)->
 Key={erts_internal,dynamic_node_name},
 try persistent_term:put(Key,Value), {erlang:is_alive(),erlang:is_alive()}
 after persistent_term:erase(Key) end.
