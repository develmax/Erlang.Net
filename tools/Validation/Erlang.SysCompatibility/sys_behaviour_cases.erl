-module(sys_behaviour_cases).
-export([run/1,init/1,callback_mode/0,handle_event/4,handle_event/2,handle_call/2,handle_info/2,terminate/2,terminate/3,code_change/3,code_change/4]).
run(tail_badfun)->tail_fun(bad);
run(non_tail_badfun)->non_tail_fun(bad);
run(tail_badarity)->tail_fun(fun()->ok end);
run(non_tail_badarity)->non_tail_fun(fun()->ok end);
run(gen_event)->
    {ok,P}=gen_event:start(),
    try
        ok=gen_event:add_handler(P,?MODULE,gen_event),
        ok=sys:log(P,true),ok=gen_event:sync_notify(P,increment),
        [{?MODULE,false,1}]=sys:get_state(P),
        [{?MODULE,false,2}]=sys:replace_state(P,fun({M,I,N})->{M,I,N+1} end),
        ok=sys:suspend(P),ok=sys:change_code(P,?MODULE,old,extra),ok=sys:resume(P),
        [{?MODULE,false,12}]=sys:get_state(P),
        ok=sys:terminate(P,normal),ok
    after exit(P,kill) end;
run(gen_statem)->
    {ok,P}=gen_statem:start(?MODULE,gen_statem,[]),
    try
        ok=sys:log(P,{true,2}),ok=gen_statem:cast(P,increment),
        {active,1}=sys:get_state(P),
        {active,2}=sys:replace_state(P,fun({S,N})->{S,N+1} end),
        ok=sys:suspend(P),ok=sys:change_code(P,?MODULE,old,extra),ok=sys:resume(P),
        {active,12}=sys:get_state(P),
        ok=sys:terminate(P,normal),ok
    after exit(P,kill) end.
init(gen_event)->{ok,0};
init(gen_statem)->{ok,active,0}.
callback_mode()->handle_event_function.
handle_event(cast,increment,_,N)->{keep_state,N+1}.
handle_event(increment,N)->{ok,N+1}.
handle_call(get,N)->{ok,N,N}.
handle_info(_,N)->{ok,N}.
terminate(_,_) -> ok.
terminate(_,_,_) -> ok.
code_change(_,N,_)->{ok,N+10}.
code_change(_,S,N,_)->{ok,S,N+10}.

tail_fun(F)->F(one,two,three).
non_tail_fun(F)->R=F(one,two,three),{ok,R}.
