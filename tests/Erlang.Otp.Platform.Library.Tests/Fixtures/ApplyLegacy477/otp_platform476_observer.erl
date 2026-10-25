-module(otp_platform476_observer).
-export([worker/1,run/0,identity_reference/0]).
-file("otp_platform476_observer.erl",1).

worker(Parent) -> Parent ! {finished,run()}.
run() ->
    [observe({parameter,M,P,V},fun()->M:param_type(P,V) end) ||
        {M,Ps}<-parameters(),P<-Ps,V<-values()] ++
    [observe({default,M,P},fun()->M:param_default(P) end) ||
        {M,Ps}<-parameters(),P<-Ps] ++
    [observe({df,Text,Flavor},fun()->disksup:parse_df(Text,Flavor) end) ||
        Text<-disk_lines(),Flavor<-[posix,susv3,bad]] ++
    [observe({cpu_dummy,V},fun()->cpu_sup:dummy_reply(V) end) ||
        V<-["n","1","5","f",{"u",false,false},{"u",true,true},bad,[],"q",{bad,1,2}]] ++
    [observe({callback,M,F,S},fun()->apply(M,F,[unexpected,S]) end) ||
        M<-[cpu_sup,os_mon_sysinfo,nteventlog,odbc],F<-[handle_cast],S<-states()] ++
    [observe({info,M,S},fun()->M:handle_info(unexpected,S) end) ||
        M<-[os_mon_sysinfo,nteventlog],S<-states()] ++
    [observe({upgrade,M,S},fun()->M:code_change(old,S,extra) end) ||
        M<-[disksup,memsup,os_sup,odbc],S<-states()] ++
    [observe({os_stop,S},fun()->os_mon:stop(S) end)||S<-states()] ++
    [observe({odbc_stop,S},fun()->odbc_app:stop(S) end)||S<-states()] ++
    [observe({odbc_supervisor,A},fun()->odbc_sup:init(A) end)||A<-[[],[connection],[{child,1}],[a,b],bad,[a|bad]]] ++
    [observe({odbc_guard,F,Args},fun()->apply(odbc,F,Args) end)||
        {F,As}<-odbc_calls(),Args<-As].

parameters() ->
    [{disksup,[disk_space_check_interval,disk_almost_full_threshold,disksup_posix_only,unknown]},
     {memsup,[memsup_system_only,memory_check_interval,memsup_helper_timeout,system_memory_high_watermark,process_memory_high_watermark,unknown]},
     {os_sup,[os_sup_errortag,os_sup_own,os_sup_syslogconf,os_sup_enable,os_sup_mfa,unknown]}].
values() ->
    [-1,0,1,2,30,16#10000000000000000,-0.5,0.0,0.05,0.8,1.0,1.1,true,false,other,
     [],"abc",[0],[10],[127],[255],[256],[16#10ffff],[16#110000],<<"abc">>,#{},
     {os_sup,error_report,[]},{mod,func,[a]},{1,func,[]},{mod,1,[]},{mod,func,bad},
     {second,1},{millisecond,1},{microsecond,1},{nanosecond,1},{second,0},{second,-1},
     {bad,1},{1,1},{1000,1},{second,bad},[1|bad]].
states() -> [undefined,[],#{value=>42},{state,1},17].
disk_lines() ->
    ["", "bad", "/dev/sda 1024 20 1004 2% /\n",
     "/dev/sda 1024 20 1004 2% /mount with spaces\r\nnext line",
     "dev 243949060 2380 86690680 65% 2029724 37555 0% /\n",
     "dev 0 0 0 0% /zero", "dev -1 2 -3 101% /negative",
     "dev 999999999999999999999999999999 0 12 99% /large\n",
     "dev x 2 3 4% /bad", "dev 1 2 x 4% /bad", "dev 1 2 3 x% /bad",
     "dev 1 2 3 4 /without-percent", "dev 1 2 3 4%     /space\n\nnext",
     "dev 1 2 3 4% 0 0 0% /inode\rnext", "dev 1"].
identity_reference() ->
    [observe({df_identity,Text,Flavor},fun()->disksup:parse_df(Text,Flavor) end) ||
        Text<-[bad,<<"dev">>,[1|bad]],Flavor<-[posix,susv3,bad]].
odbc_calls() ->
    [{connect,[[bad,[]],[[],bad],[<<"DSN=x">>,[]]]},
     {disconnect,[[bad],[0],[[]]]},
     {commit,[[bad,commit],[bad,rollback],[bad,bad]]},
     {commit,[[bad,commit,infinity],[bad,rollback,1],[bad,commit,0]]},
     {sql_query,[[bad,"select 1"],[bad,[]],[bad,bad]]},
     {sql_query,[[bad,"select 1",infinity],[bad,[],1],[bad,bad,0]]},
     {select_count,[[bad,"select 1"],[bad,[]],[bad,bad]]},
     {select_count,[[bad,"select 1",infinity],[bad,[],1],[bad,bad,0]]},
     {first,[[bad],[0],[[]]]},{last,[[bad],[0],[[]]]},
     {next,[[bad],[0],[[]]]},{prev,[[bad],[0],[[]]]},
     {first,[[bad,infinity],[bad,1],[bad,0]]},{last,[[bad,infinity],[bad,1],[bad,0]]},
     {next,[[bad,infinity],[bad,1],[bad,0]]},{prev,[[bad,infinity],[bad,1],[bad,0]]},
     {select,[[bad,next,1],[bad,{absolute,1},2],[bad,{relative,1},2]]},
     {select,[[bad,next,1,infinity],[bad,{absolute,1},2,1],[bad,{relative,1},2,0]]},
     {param_query,[[bad,"q",[]],[bad,[],[]],[bad,bad,bad]]},
     {param_query,[[bad,"q",[],infinity],[bad,[],[],1],[bad,bad,bad,0]]},
     {describe_table,[[bad,"table"],[bad,[]],[bad,bad]]},
     {describe_table,[[bad,"table",infinity],[bad,[],1],[bad,bad,0]]}].
observe(Name,F) ->
    try F() of V -> {Name,{returned,V}}
    catch C:R:S -> {Name,{caught,C,R,S}}
    end.
