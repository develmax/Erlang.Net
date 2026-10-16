namespace Erlang.Differential;

internal static class ProcLibCaseSources
{
    private const string Header = "-module(oracle_proc_lib). -export([run/0,child/1,linked_child/1]). ";
    private const string Helpers = " child(_)->proc_lib:init_ack({hd(get('$ancestors')),get('$initial_call')}). linked_child(_)->proc_lib:init_ack(self()),receive stop->ok end.";
    public const string MfaSpawn = Header + "run()->P=self(),C=proc_lib:spawn(?MODULE,child,[P]),receive {ack,C,{A,I}}->{A=:=P,I} end." + Helpers;
    public const string MfaLink = Header + "run()->C=proc_lib:spawn_link(?MODULE,linked_child,[self()]),receive {ack,C,C}->ok end,{links,L}=process_info(self(),links),R=lists:member(C,L),C!stop,R." + Helpers;
    public const string FunSpawn = Header + "run()->P=self(),C=proc_lib:spawn(fun()->proc_lib:init_ack({hd(get('$ancestors')),get('$initial_call')}) end),receive {ack,C,{A,{M,_,Arity}}}->{A=:=P,M=:=?MODULE,Arity} end." + Helpers;
    public const string FunLink = Header + "run()->C=proc_lib:spawn_link(fun()->proc_lib:init_ack(self()),receive stop->ok end end),receive {ack,C,C}->ok end,{links,L}=process_info(self(),links),R=lists:member(C,L),C!stop,R." + Helpers;
    public const string AckOne = Header + "run()->put('$ancestors',[self()]),R=proc_lib:init_ack(arbitrary),receive {ack,P,arbitrary}->{R,P=:=self()} end." + Helpers;
    public const string AckTwo = Header + "run()->R=proc_lib:init_ack(self(),{anything,17}),receive {ack,P,{anything,17}}->{R,P=:=self()} end." + Helpers;
    public const string Registered = Header + "run()->register(proc_lib_parent,self()),C=proc_lib:spawn(?MODULE,child,[self()]),R=receive {ack,C,{A,_}}->A end,unregister(proc_lib_parent),R." + Helpers;
    public const string Inherited = Header + "run()->put('$ancestors',[earlier]),put(private_key,private_value),P=self(),C=proc_lib:spawn(fun()->proc_lib:init_ack({get('$ancestors'),get(private_key)}) end),receive {ack,C,{[P,earlier],undefined}}->ok end." + Helpers;
    public const string InvalidAncestors = Header + "run()->put('$ancestors',[bad|tail]),P=self(),C=proc_lib:spawn(fun()->proc_lib:init_ack(get('$ancestors')) end),receive {ack,C,[P]}->ok end." + Helpers;
    public const string MissingAncestors = Header + "run()->try proc_lib:init_ack(value) catch error:{badmatch,undefined}->ok end." + Helpers;
    public const string EmptyAncestors = Header + "run()->put('$ancestors',[]),try proc_lib:init_ack(value) catch error:{badmatch,[]}->ok end." + Helpers;
    public const string ImproperAncestors = Header + "run()->put('$ancestors',[self()|tail]),R=proc_lib:init_ack(value),receive {ack,P,value}->{R,P=:=self()} end." + Helpers;
    public const string InvalidFun = Header + "run()->try proc_lib:spawn(not_a_fun) catch error:function_clause->ok end." + Helpers;
    public const string InvalidModule = Header + "run()->try proc_lib:spawn(17,child,[]) catch error:function_clause->ok end." + Helpers;
    public const string InvalidArgs = Header + "run()->try proc_lib:spawn(?MODULE,child,[a|tail]) catch error:function_clause->ok end." + Helpers;
    public const string NonzeroFun = Header + "run()->process_flag(trap_exit,true),C=proc_lib:spawn_link(fun(X)->X end),receive {'EXIT',C,{{badarity,{_,[]}},_}}->ok end." + Helpers;
    public const string MissingMfa = Header + "run()->process_flag(trap_exit,true),C=proc_lib:spawn_link(proc_lib_missing_module,missing,[]),receive {'EXIT',C,{undef,_}}->ok end." + Helpers;
    public const string NativeInitialFun = Header + "run()->C=proc_lib:spawn(fun()->proc_lib:init_ack(process_info(self(),initial_call)) end),receive {ack,C,{initial_call,{proc_lib,init_p,3}}}->ok end." + Helpers;
    public const string NativeInitialMfa = "-module(oracle_proc_lib). -export([run/0,child/0]). run()->C=proc_lib:spawn(?MODULE,child,[]),receive {ack,C,{initial_call,{proc_lib,init_p,5}}}->ok end. child()->proc_lib:init_ack(process_info(self(),initial_call)).";
}
