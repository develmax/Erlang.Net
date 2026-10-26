namespace Erlang.Differential;

internal static class ProcLibStartupCaseSources
{
    private const string Header = "-module(oracle_proc_start). -export([run/0,ack/1,reject/1,silent/0,die/1,hold/1,ack_hold/2]). ";
    private const string Helpers = " ack(R)->proc_lib:init_ack(R). reject(Mode)->proc_lib:init_fail({rejected,self()},{Mode,boom}). silent()->receive never->ok end. die(Reason)->exit(Reason). hold(P)->proc_lib:init_ack({ok,self()}),receive stop->P!stopped end. ack_hold(R,P)->proc_lib:init_ack({R,self()}),receive stop->P!stopped end. clean()->{monitors,[]}=process_info(self(),monitors),receive {'DOWN',_,process,_,_}->stale;{'EXIT',_,_}->stale after 0->ok end. invalid_timeout(T)->R=try proc_lib:start(oracle_proc_start,silent,[],T) catch error:timeout_value->caught end,{monitors,[{process,C}]}=process_info(self(),monitors),exit(C,kill),receive {'DOWN',_,process,C,killed}->{R,is_process_alive(C)} end.";
    public const string StartThree = Header + "run()->proc_lib:start(oracle_proc_start,ack,[ready])." + Helpers;
    public const string MaximumTimeout = Header + "run()->proc_lib:start(oracle_proc_start,ack,[ready],4294967295)." + Helpers;
    public const string StartFour = Header + "run()->proc_lib:start(oracle_proc_start,ack,[ready],infinity)." + Helpers;
    public const string LinkThree = Header + "run()->proc_lib:start_link(oracle_proc_start,ack,[ready])." + Helpers;
    public const string LinkFour = Header + "run()->{ok,C}=proc_lib:start_link(oracle_proc_start,hold,[self()],infinity),{links,L}=process_info(self(),links),R=lists:member(C,L),C!stop,receive stopped->R end." + Helpers;
    public const string FailTwo = Header + "run()->put('$ancestors',[self()]),caught=try proc_lib:init_fail(arbitrary,{throw,boom}) catch throw:boom->caught end,receive {nack,P,arbitrary}->P=:=self() end." + Helpers;
    public const string FailThree = Header + "run()->caught=try proc_lib:init_fail(self(),arbitrary,{exit,boom}) catch exit:boom->caught end,receive {nack,P,arbitrary}->P=:=self() end." + Helpers;
    public const string RejectError = Header + "run()->{rejected,C}=proc_lib:start(oracle_proc_start,reject,[error]),{false,ok}={is_process_alive(C),clean()},ok." + Helpers;
    public const string RejectExit = Header + "run()->{rejected,C}=proc_lib:start(oracle_proc_start,reject,[exit]),{false,ok}={is_process_alive(C),clean()},ok." + Helpers;
    public const string RejectThrow = Header + "run()->{rejected,C}=proc_lib:start(oracle_proc_start,reject,[throw]),{false,ok}={is_process_alive(C),clean()},ok." + Helpers;
    public const string ZeroTimeout = Header + "run()->{error,timeout}=proc_lib:start(oracle_proc_start,silent,[],0),clean()." + Helpers;
    public const string LinkTimeout = Header + "run()->{error,timeout}=proc_lib:start_link(oracle_proc_start,silent,[],0),clean()." + Helpers;
    public const string FiniteTimeout = Header + "run()->{error,timeout}=proc_lib:start(oracle_proc_start,silent,[],10),clean()." + Helpers;
    public const string Mailbox = Header + "run()->P=self(),Ref=make_ref(),self()!unrelated,self()!{ack,P,fake},self()!{'DOWN',Ref,process,P,fake},ready=proc_lib:start(oracle_proc_start,ack,[ready]),receive unrelated->ok end,receive {ack,P,fake}->ok end,receive {'DOWN',Ref,process,P,fake}->ok end." + Helpers;
    public const string Registered = Header + "run()->register(proc_lib_start_parent,self()),R=proc_lib:start(oracle_proc_start,ack,[ready]),unregister(proc_lib_start_parent),R." + Helpers;
    public const string AtomTimeout = Header + "run()->invalid_timeout(invalid)." + Helpers;
    public const string NegativeTimeout = Header + "run()->invalid_timeout(-1)." + Helpers;
    public const string LargeTimeout = Header + "run()->invalid_timeout(4294967296)." + Helpers;
    public const string FloatTimeout = Header + "run()->invalid_timeout(0.0)." + Helpers;
    public const string InvalidMfa = Header + "run()->caught=try proc_lib:start(17,ack,[]) catch error:function_clause->caught end,clean()." + Helpers;
    public const string ImproperArgs = Header + "run()->{error,{badarg,_}}=proc_lib:start(oracle_proc_start,ack,[ready|tail]),clean()." + Helpers;
    public const string MissingMfa = Header + "run()->{error,{undef,_}}=proc_lib:start(proc_start_missing,missing,[]),clean()." + Helpers;
    public const string NormalBeforeAck = Header + "run()->{error,normal}=proc_lib:start(oracle_proc_start,die,[normal]),clean()." + Helpers;
    public const string ErrorReturn = Header + "run()->{R,C}=proc_lib:start(oracle_proc_start,ack_hold,[{error,payload},self()]),V={is_process_alive(C),R},C!stop,receive stopped->V end." + Helpers;
    public const string FlushMonitor = Header + "run()->{ready,C}=proc_lib:start(oracle_proc_start,ack_hold,[ready,self()]),{true,ok}={is_process_alive(C),clean()},C!stop,receive stopped->ok end." + Helpers;
    public const string FailStack = Header + "run()->S=[{proc_lib_trace,frame,0,[]}],ok=try proc_lib:init_fail(self(),rejected,{throw,boom,S}) catch throw:boom:Trace->S=Trace,ok end,receive {nack,P,rejected}->P=:=self() end." + Helpers;
    public const string FailBadClass = Header + "run()->caught=try proc_lib:init_fail(self(),rejected,{invalid,boom,[]}) catch error:badarg->caught end,receive {nack,P,rejected}->P=:=self() end." + Helpers;
    public const string FailMalformed = Header + "run()->caught=try proc_lib:init_fail(self(),rejected,{invalid,boom}) catch error:{case_clause,{invalid,boom}}->caught end,receive {nack,P,rejected}->P=:=self() end." + Helpers;
    public const string FailMissingParent = Header + "run()->caught=try proc_lib:init_fail(rejected,{exit,boom}) catch error:{badmatch,undefined}->caught end,receive {nack,_,_}->stale after 0->ok end." + Helpers;
    public const string LinkedReject = Header + "run()->process_flag(trap_exit,true),{rejected,C}=proc_lib:start_link(oracle_proc_start,reject,[error]),{false,ok}={is_process_alive(C),clean()},ok." + Helpers;
    public const string TimeoutMailbox = Header + "run()->P=self(),self()!{'EXIT',P,unrelated},{error,timeout}=proc_lib:start(oracle_proc_start,silent,[],0),receive {'EXIT',P,unrelated}->ok end." + Helpers;
}
