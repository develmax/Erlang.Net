param([string] $OtpRoot, [string] $Tool, [string] $Output, [string] $Source)

$ErrorActionPreference='Stop'
$s=(Resolve-Path $Source).Path
$a=[Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile=$a.GetType('Erlang.Compiler.CodeGeneration',$true).GetMethod('CompileBeamFile')
$outputPath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath,$compile.Invoke($null,[object[]]@($s,$null,$null,$null)))
$e=@'
[S,P,O]=init:get_plain_arguments(),
{ok,M,R}=compile:file(S,[binary,deterministic]),
ok=file:write_file(O,R),
{ok,C}=file:read_file(P),
Read=fun(Bin,Name,Arity) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,Name,Arity,_,Is}]=[F||F={function,N,A,_,_}<-Fs,N=:=Name,A=:=Arity],
    Is
end,
lists:foreach(fun({Label,Bin}) ->
    Is=Read(Bin,run,3),
    io:format("~s ~0p~n",[Label,Is]),
    [{call,1,{M,select,1}},{test,Shape,_,[{x,0}|_]}|_]=
        lists:dropwhile(fun(I)->I=/={call,1,{M,select,1}} end,Is),
    true=lists:member(Shape,[is_tuple,is_tagged_tuple]),
    {module,M}=code:load_binary(M,S,Bin),
    {match,{state,state},value}=M:run({entry,key,value},key,state),
    {fallback,{state,state},value}=M:run({entry,fallback,value},key,state),
    {error,reason}=M:run({error,reason},key,state),
    Invalid={entry,other,value},
    {'EXIT',{{case_clause,Invalid},_}}=(catch M:run(Invalid,key,state)),
    {'EXIT',{{case_clause,invalid},_}}=(catch M:run(invalid,key,state)),
    {value,state,key}=M:tail({entry,key,value},key,state),
    no=M:tail({entry,other,value},key,state),
    Tail=Read(Bin,tail,3),
    io:format("~s tail ~0p~n",[Label,Tail]),
    [{call_last,3,{M,pass,3},_}]=[I||I={call_last,3,{Module,pass,3},_}<-Tail,Module=:=M],
    true=lists:member({get_tuple_element,{x,0},2,{x,0}},Tail),
    {continued,{value,state,key}}=M:continue_after({entry,key,value},key,state),
    {continued,no}=M:continue_after({entry,other,value},key,state),
    Continue=Read(Bin,continue_after,3),
    io:format("~s continue ~0p~n",[Label,Continue]),
    true=lists:member({call,3,{M,pass,3}},Continue),
    Arity=Read(Bin,arity_fallback,3),
    io:format("~s arity ~0p~n",[Label,Arity]),
    true=lists:any(fun(I)->case I of
        {select_tuple_arity,_,_,{list,[2,_,3,_]}} -> true;
        _ -> false
    end end,Is),
    {match,state}=M:arity_fallback({entry,fallback},fallback,state),
    {match,state,value}=M:arity_fallback({entry,key,value},key,state),
    fallback=M:arity_fallback({entry,fallback},key,state),
    tagged=M:arity_fallback({other_tag,value},key,state),
    {other,{unknown_tag,value}}=M:arity_fallback({unknown_tag,value},key,state),
    [{select_val,{x,1},_,{list,[{atom,entry},_,{atom,other_tag},_]}}]=
        [I || I={select_val,_,_,_} <- Arity],
    off=M:arity_fallback(off,key,state),
    {other,{entry,other}}=M:arity_fallback({entry,other},key,state),
    {other,{entry,key,value,more}}=M:arity_fallback({entry,key,value,more},key,state),
    {retained,{ignored,key,unused}}=M:projection({ignored,key,unused},key,state),
    {value,state,value}=M:projection({copy,key,value,unused},key,state),
    {value,state,key}=M:projection({call,key,unused,extra,value},key,state),
    {matched,state}=M:projection({repeat,a,a},key,state),
    {other,{repeat,a,b}}=M:projection({repeat,a,b},key,state),
    {other,{ignored,other,unused}}=M:projection({ignored,other,unused},key,state),
    Projection=Read(Bin,projection,3),
    io:format("~s projection ~0p~n",[Label,Projection]),
    []=[I || I={get_tuple_element,_,3,_} <- Projection],
    [{get_tuple_element,{x,0},2,{x,0}}]=
        [I || I={get_tuple_element,{x,0},2,{x,0}} <- Projection],
    accepted=M:guard_route([], {ok}),
    accepted=M:guard_route(<<"abc">>, not_tuple),
    rejected=M:guard_route([], not_tuple),
    rejected=M:guard_route(not_list_or_binary, {ok}),
    {other,<<"abc">>}=M:guard_entry(other, <<"abc">>),
    accepted=M:guard_failure({false}, <<"abc">>),
    rejected=M:guard_failure(not_tuple, <<"abc">>),
    lists:foreach(fun(Name)->
        GuardCode=Read(Bin,Name,2),
        io:format("~s ~p ~0p~n",[Label,Name,GuardCode]),
        true=lists:any(fun(I)->case I of {test,is_list,_,_}->true; _->false end end,GuardCode),
        true=lists:any(fun(I)->case I of {test,is_binary,_,_}->true; _->false end end,GuardCode),
        []=[I || I={bif,Bif,_,_,_} <- GuardCode, Bif=:=is_list orelse Bif=:=is_binary]
    end,[guard_route,guard_entry]),
    {ok,value}=M:nested_tagged({outer,{inner,key,value}},key),
    lists:foreach(fun(Input)->{other,Input}=M:nested_tagged(Input,key) end,
                  [not_a_tuple,{outer},{outer,{inner,key}},
                   {outer,{wrong,key,value}},{wrong,{inner,key,value}},{outer,{inner,bad,value}}]),
    Tagged=Read(Bin,nested_tagged,2),
    io:format("~s tagged ~0p~n",[Label,Tagged]),
    [{2,outer},{3,inner}]=
        [{Arity,Tag} || {test,is_tagged_tuple,_,[_,Arity,{atom,Tag}]} <- Tagged],
    Fresh={outer,{inner,{leaf,key},value},value},
    {ok,value}=M:fresh_nested(Fresh,key),
    {other,Fresh}=M:fresh_nested(Fresh,bad_key),
    RepeatMiss={outer,{inner,{leaf,key},value},wrong},
    {other,RepeatMiss}=M:fresh_nested(RepeatMiss,key),
    FreshCode=Read(Bin,fresh_nested,2),
    io:format("~s fresh tagged ~0p~n",[Label,FreshCode]),
    [{3,outer},{3,inner},{2,leaf}]=
        [{Arity,Tag} || {test,is_tagged_tuple,_,[{x,_},Arity,{atom,Tag}]} <- FreshCode],
    Argument={tagged,ignored,value},
    {{first,{ok,value},last},[last,Argument,first]}=M:argument_case(first,Argument,last),
    OtherArgument={wrong,ignored,value},
    {{first,{other,OtherArgument},last},[last,OtherArgument,first]}=
        M:argument_case(first,OtherArgument,last),
    ArgumentCode=Read(Bin,argument_case,3),
    io:format("~s argument case ~0p~n",[Label,ArgumentCode]),
    [{test,is_tagged_tuple,_,[{x,_},3,{atom,tagged}]}]=
        [I || I={test,is_tagged_tuple,_,[_,3,{atom,tagged}]} <- ArgumentCode],
    {large,7}=M:guarded_case({guarded,7},5),
    {named,key}=M:guarded_case({guarded,key},5),
    {named,<<"key">>}=M:guarded_case({guarded,<<"key">>},5),
    {list,[key]}=M:guarded_case([key],5),
    {other,{guarded,4}}=M:guarded_case({guarded,4},5),
    {other,{wrong,key}}=M:guarded_case({wrong,key},5),
    7=M:guarded_reject({guarded,7}),
    {'EXIT',{{case_clause,{guarded,key}},_}}=(catch M:guarded_reject({guarded,key})),
    Guarded=Read(Bin,guarded_case,2),
    io:format("~s guarded case ~0p~n",[Label,Guarded]),
    true=lists:any(fun(I)->case I of {test,is_list,_,[{x,_}]}->true; _->false end end,Guarded),
    GuardedReject=Read(Bin,guarded_reject,1),
    io:format("~s guarded rejection ~0p~n",[Label,GuardedReject]),
    [{case_end,{x,0}}]=[I||I={case_end,_}<-GuardedReject],
    Rec={parity_record,old,kept},
    {Rec,{parity_record,new,kept}}=M:update_one(Rec,new),
    {Rec,Rec}=M:update_one(Rec,old),
    {Rec,{parity_record,new,other}}=M:update_many(Rec,new,other),
    {parity_record,new,same}=M:update_known(new),
    {'EXIT',{{badrecord,wrong_record},_}}=(catch M:update_one(wrong_record,new)),
    lists:foreach(fun({Function,Arity,Expected}) ->
        Code=Read(Bin,Function,Arity),
        io:format("~s ~p ~0p~n",[Label,Function,Code]),
        Expected=[Mode || {update_record,{atom,Mode},_,_,_,_} <- Code]
    end,[{update_one,2,[reuse]},{update_many,3,[copy]},{update_known,1,[]}]),
    {old,{parity_record,old,updated}}=M:read_checked(Rec),
    old=M:record_case(Rec),
    {'EXIT',{{badrecord,wrong_record},_}}=(catch M:record_case(wrong_record)),
    [old]=M:read_shadow(Rec,[Rec]),
    {'EXIT',{{badrecord,wrong_record},_}}=(catch M:read_shadow(Rec,[wrong_record])),
    Checked=Read(Bin,read_checked,1),
    io:format("~s checked record ~0p~n",[Label,Checked]),
    []=[I||I={badrecord,_}<-Checked],
    [{test,is_tagged_tuple,_,[_,3,{atom,parity_record}]}]=
        [I||I={test,is_tagged_tuple,_,[_,3,{atom,parity_record}]}<-Checked],
    CaseRecord=Read(Bin,record_case,1),
    io:format("~s checked case ~0p~n",[Label,CaseRecord]),
    [{badrecord,_}]=[I||I={badrecord,_}<-CaseRecord],
    kept=M:record_select(Rec),
    {old,kept,old}=M:record_args(Rec),
    old=M:record_saved(Rec),
    {other,wrong_record}=M:record_saved(wrong_record),
    kept=M:record_after_branch(Rec,true),
    kept=M:record_after_branch(Rec,false),
    {'EXIT',{{badrecord,wrong_record},_}}=(catch M:record_after_branch(wrong_record,false)),
    lists:foreach(fun({Function,Count})->
        Code=Read(Bin,Function,1),
        io:format("~s ~p ~0p~n",[Label,Function,Code]),
        Count=length([I||I={badrecord,_}<-Code])
    end,[{record_select,1},{record_args,1},{record_saved,0}]),
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
