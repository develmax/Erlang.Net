-module(proplist_get_value_contract).
-export([run/0,run/1,names/0,stack_observations/0]).

names()->['empty-two','empty-three','missing-two','missing-three','atom-shorthand','atom-before-tuple','tuple-before-atom','first-tuple-wins','explicit-undefined','singleton-stops-two','singleton-stops-three','triple-stops-two','triple-stops-three','empty-tuple-ignored','nonmatching-tuple-arities','nonproperties-ignored','integer-exact','float-exact','large-integer-key','tuple-key-exact','list-key-exact','improper-list-key','map-key-exact','empty-list-key','empty-tuple-key','binary-key','bitstring-key','reference-key','pid-key','function-key','default-reference-identity','value-function-identity','improper-atom-match','improper-tuple-match','improper-singleton-match','improper-triple-match','improper-reached-two','improper-reached-three','nonlist-atom-two','nonlist-atom-three','nonlist-integer-two','nonlist-integer-three','nonlist-float-two','nonlist-float-three','nonlist-tuple-two','nonlist-tuple-three','nonlist-map-two','nonlist-map-three','nonlist-binary-two','nonlist-binary-three','large-missing-two','large-terminal-match','large-improper-early-match','stack-depth-0-arity-2','stack-depth-1-arity-2','stack-depth-2-arity-2','stack-depth-0-arity-3','stack-depth-1-arity-3','stack-depth-2-arity-3'].

run()->[{Name,capture(Name)} || Name<-names()].

capture(Name)->
    try run(Name) of Value->{ok,Value}
    catch Class:Reason:Stack->{raised,Class,Reason,Stack}
    end.

run('empty-two')->undefined=proplists:get_value(key,[]),done;
run('empty-three')->{fallback,42}=proplists:get_value(key,[],{fallback,42}),done;
run('missing-two')->undefined=proplists:get_value(key,[other,{other,one}]),done;
run('missing-three')->[a|tail]=proplists:get_value(key,[other],[a|tail]),done;
run('atom-shorthand')->true=proplists:get_value(key,[key]),done;
run('atom-before-tuple')->true=proplists:get_value(key,[key,{key,false}],fallback),done;
run('tuple-before-atom')->false=proplists:get_value(key,[{key,false},key]),done;
run('first-tuple-wins')->first=proplists:get_value(key,[{key,first},{key,second}]),done;
run('explicit-undefined')->undefined=proplists:get_value(key,[{key,undefined}],fallback),done;
run('singleton-stops-two')->undefined=proplists:get_value(key,[{key},{key,later}]),done;
run('singleton-stops-three')->fallback=proplists:get_value(key,[{key},{key,later}],fallback),done;
run('triple-stops-two')->undefined=proplists:get_value(key,[{key,a,b},key]),done;
run('triple-stops-three')->{a,b}=proplists:get_value(key,[{key,a,b},{key,later}],{a,b}),done;
run('empty-tuple-ignored')->found=proplists:get_value(key,[{},{key,found}]),done;
run('nonmatching-tuple-arities')->found=proplists:get_value(key,[{other},{other,a,b},{key,found}]),done;
run('nonproperties-ignored')->found=proplists:get_value(key,[42,1.5,[],[key],<<7>>,{},{key,found}]),done;
run('integer-exact')->integer=proplists:get_value(1,[{1.0,float},{1,integer}]),done;
run('float-exact')->float=proplists:get_value(1.0,[{1,integer},{1.0,float}]),done;
run('large-integer-key')->found=proplists:get_value(18446744073709551617,[{18446744073709551616,wrong},{18446744073709551617,found}]),done;
run('tuple-key-exact')->found=proplists:get_value({1},[{{1.0},wrong},{{1},found}]),done;
run('list-key-exact')->found=proplists:get_value([1],[{[1.0],wrong},{[1],found}]),done;
run('improper-list-key')->found=proplists:get_value([1|tail],[{[1|other],wrong},{[1|tail],found}]),done;
run('map-key-exact')->found=proplists:get_value(#{a=>1},[{#{a=>1.0},wrong},{#{a=>1},found}]),done;
run('empty-list-key')->found=proplists:get_value([],[[],{[],found}]),done;
run('empty-tuple-key')->found=proplists:get_value({},[{},{{},found}]),done;
run('binary-key')->found=proplists:get_value(<<1,2>>,[{<<1>>,wrong},{<<1,2>>,found}]),done;
run('bitstring-key')->found=proplists:get_value(<<1:1>>,[{<<1>>,wrong},{<<1:1>>,found}]),done;
run('reference-key')->R=make_ref(),S=make_ref(),found=proplists:get_value(R,[{S,wrong},{R,found}]),done;
run('pid-key')->P=self(),found=proplists:get_value(P,[P,{P,found}]),done;
run('function-key')->F=fun(X)->X end,found=proplists:get_value(F,[F,{F,found}]),done;
run('default-reference-identity')->R=make_ref(),R=proplists:get_value(key,[],R),done;
run('value-function-identity')->F=fun(X)->X end,F=proplists:get_value(key,[{key,F}]),done;
run('improper-atom-match')->true=proplists:get_value(key,[key|tail]),done;
run('improper-tuple-match')->found=proplists:get_value(key,[{key,found}|tail]),done;
run('improper-singleton-match')->fallback=proplists:get_value(key,[{key}|tail],fallback),done;
run('improper-triple-match')->fallback=proplists:get_value(key,[{key,a,b}|tail],fallback),done;
run('improper-reached-two')->ok=try proplists:get_value(key,[other|tail]) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('improper-reached-three')->ok=try proplists:get_value(key,[other,{other,1}|tail],fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-atom-two')->ok=try proplists:get_value(key,not_list) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,not_list,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-atom-three')->ok=try proplists:get_value(key,not_list,fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,not_list,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-integer-two')->ok=try proplists:get_value(key,42) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,42,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-integer-three')->ok=try proplists:get_value(key,42,fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,42,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-float-two')->ok=try proplists:get_value(key,1.5) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,1.5,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-float-three')->ok=try proplists:get_value(key,1.5,fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,1.5,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-tuple-two')->ok=try proplists:get_value(key,{}) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,{},undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-tuple-three')->ok=try proplists:get_value(key,{},fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,{},fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-map-two')->ok=try proplists:get_value(key,#{a=>1}) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,FrameArgs,[{file,"proplists.erl"},{line,222}]}|_]=Stack,true=FrameArgs=:=[key,#{a=>1},undefined],ok end,done;
run('nonlist-map-three')->ok=try proplists:get_value(key,#{a=>1},fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,FrameArgs,[{file,"proplists.erl"},{line,222}]}|_]=Stack,true=FrameArgs=:=[key,#{a=>1},fallback],ok end,done;
run('nonlist-binary-two')->ok=try proplists:get_value(key,<<7>>) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,<<7>>,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('nonlist-binary-three')->ok=try proplists:get_value(key,<<7>>,fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,<<7>>,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,ok end,done;
run('large-missing-two')->L=lists:duplicate(12000,other),undefined=proplists:get_value(key,L),done;
run('large-terminal-match')->L=lists:duplicate(12000,other),found=proplists:get_value(key,L++[{key,found}]),done;
run('large-improper-early-match')->L=lists:duplicate(12000,other),found=proplists:get_value(key,L++[{key,found}|tail]),done;
run('stack-depth-0-arity-2')->Old=erlang:system_flag(backtrace_depth,0),ok=try proplists:get_value(key,[other|tail]) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,1=length(Stack),ok after erlang:system_flag(backtrace_depth,Old) end,done;
run('stack-depth-1-arity-2')->Old=erlang:system_flag(backtrace_depth,1),ok=try proplists:get_value(key,[other|tail]) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,1=length(Stack),ok after erlang:system_flag(backtrace_depth,Old) end,done;
run('stack-depth-2-arity-2')->Old=erlang:system_flag(backtrace_depth,2),ok=try proplists:get_value(key,[other|tail]) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,undefined],[{file,"proplists.erl"},{line,222}]}|_]=Stack,2=length(Stack),ok after erlang:system_flag(backtrace_depth,Old) end,done;
run('stack-depth-0-arity-3')->Old=erlang:system_flag(backtrace_depth,0),ok=try proplists:get_value(key,[other|tail],fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,1=length(Stack),ok after erlang:system_flag(backtrace_depth,Old) end,done;
run('stack-depth-1-arity-3')->Old=erlang:system_flag(backtrace_depth,1),ok=try proplists:get_value(key,[other|tail],fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,1=length(Stack),ok after erlang:system_flag(backtrace_depth,Old) end,done;
run('stack-depth-2-arity-3')->Old=erlang:system_flag(backtrace_depth,2),ok=try proplists:get_value(key,[other|tail],fallback) of _->unexpected catch error:function_clause:Stack->[{proplists,get_value,[key,tail,fallback],[{file,"proplists.erl"},{line,222}]}|_]=Stack,2=length(Stack),ok after erlang:system_flag(backtrace_depth,Old) end,done.

stack_observations()->
    [{Arity,Mode,Depth,observe_stack(Arity,Mode,Depth)} || Arity<-[2,3],Mode<-[tail,non_tail],Depth<-[0,1,2]].

observe_stack(Arity,Mode,Depth)->
    Old=erlang:system_flag(backtrace_depth,Depth),
    try invoke_stack(Arity,Mode,[other|tail]) of Value->{returned,Value}
    catch Class:Reason:Stack->{Class,Reason,Stack}
    after erlang:system_flag(backtrace_depth,Old)
    end.

invoke_stack(2,tail,Properties)->proplists:get_value(key,Properties);
invoke_stack(3,tail,Properties)->proplists:get_value(key,Properties,fallback);
invoke_stack(2,non_tail,Properties)->Value=proplists:get_value(key,Properties),{returned,Value};
invoke_stack(3,non_tail,Properties)->Value=proplists:get_value(key,Properties,fallback),{returned,Value}.
