-module(otp_megaco474_observer).
-export([worker/1,run/0]).
-file("otp_megaco474_observer.erl",1).

worker(Parent) -> Parent ! {finished,run()}.
run() ->
    [observe({config,F,[V]},fun()->apply(megaco_config_misc,F,[V]) end)||F<-[verify_bool,verify_int,verify_strict_int,verify_uint,verify_strict_uint],V<-values()] ++
    [observe({config,F,[V,Max]},fun()->apply(megaco_config_misc,F,[V,Max]) end)||F<-[verify_int,verify_strict_int,verify_uint,verify_strict_uint],V<-values(),Max<-[-1,0,1,255,infinity,bad]] ++
    [observe({config,F,[V,Min,Max]},fun()->apply(megaco_config_misc,F,[V,Min,Max]) end)||F<-[verify_int,verify_strict_int],V<-values(),Min<-[-1,0,1,bad],Max<-[-1,0,1,255,infinity,bad]] ++
    lists:append([timer(T)||T<-timers()]) ++
    lists:append([termination(C,T)||C<-configs(),T<-ids()]) ++
    [observe({decode,C,T},fun()->megaco_binary_term_id:decode(C,T) end)||C<-configs(),T<-external_ids()] ++
    [observe({raw_encode,W,C,I},fun()->apply(megaco_binary_term_id_gen,F,[I,C]) end)||{W,F}<-[{false,encode_without_wildcards},{true,encode_with_wildcards}],C<-[[8,8,8],[4,4],[1,7],[]],I<-[[],["00000000","00000001","11111111"],["*"],["$"],["0000","1111"],["1","0101010"],["invalid"],bad]] ++
    [observe({raw_decode,W,C,I},fun()->case W of false->megaco_binary_term_id_gen:decode_without_wildcards(I,C);true->megaco_binary_term_id_gen:decode_with_wildcards([[215]],I,C) end end)||W<-[false,true],C<-[[8,8,8],[4,4],[1,7],[]],I<-[[],[0,1,255],[255],[256],[-1],bad]].
values() -> [infinity,-(1 bsl 64),-1,0,1,255,1 bsl 64,0.0,1.0,true,false,[],bad].
timers() ->
    [infinity,0,1,1 bsl 64,-1,0.5,bad,[],{megaco_incr_timer}] ++
    [{megaco_incr_timer,W,F,I,R}||W<-[0,1,100],F<-[0,1,2],I<-[-10,0,3],R<-[0,1,2,infinity,infinity_restartable]] ++
    [{megaco_incr_timer,W,2,0,1}||W<-[-1,1.5,bad]] ++
    [{megaco_incr_timer,1,F,0,1}||F<-[-1,1.5,bad]] ++
    [{megaco_incr_timer,1,2,I,1}||I<-[1.5,bad]] ++
    [{megaco_incr_timer,1,2,0,R}||R<-[-1,1.5,bad]].
timer(T) ->
    [observe({timer,verify,T},fun()->megaco_timer:verify(T) end),
     observe({timer,init,T},fun()->megaco_timer:init(T) end),
     observe({timer,restart,T},fun()->megaco_timer:restart(T) end),
     observe({timer,sequence,T},fun()->advance(megaco_timer:init(T),4,[]) end)].
advance({W,timeout},_,A) -> lists:reverse([{W,timeout}|A]);
advance({W,T},0,A) -> lists:reverse([{W,T}|A]);
advance({W,T},N,A) -> advance(megaco_timer:restart(T),N-1,[{W,T}|A]).
configs() -> [[8,8,8],[4,4],[1,7],3,[],bad].
ids() ->
    [bad,[],{megaco_term_id,false,["root"]},{megaco_term_id,true,["*"]},{megaco_term_id,true,["$"]},
     {megaco_term_id,false,[]},{megaco_term_id,false,["00000000","00000001","11111111"]},
     {megaco_term_id,false,["0000","1111"]},{megaco_term_id,false,["1","0101010"]},
     {megaco_term_id,true,["00000001","00011110","*"]},{megaco_term_id,true,["$","00011110","$"]},
     {megaco_term_id,true,["00000001","$","$"]},{megaco_term_id,true,["00000001","01*"]},
     {megaco_term_id,false,["2"]},{megaco_term_id,false,["000000000"]},{megaco_term_id,invalid,bad}].
external_ids() ->
    [bad,[],{'TerminationID',[],[255,255,255,255,255,255,255,255]},
     {'TerminationID',[],[0,1,255]},{'TerminationID',[],[15]},{'TerminationID',[],[]},
     {'TerminationID',[[215]],[0,0,0]},{'TerminationID',[[87]],[0,0,0]},
     {'TerminationID',[[255]],[0,1,255]},{'TerminationID',bad,bad},
     {'TerminationID',[],[256]},{'TerminationID',[],[-1]},{'TerminationID',[],bad}].
termination(C,T) ->
    [observe({encode,C,T},fun()->megaco_binary_term_id:encode(C,T) end),
     observe({roundtrip,C,T},fun()->case megaco_binary_term_id:encode(C,T) of {ok,E}->{E,megaco_binary_term_id:decode(C,E)};Other->Other end end)].
observe(Name,F) ->
    try F() of V -> {Name,{returned,V}}
    catch C:R:S -> {Name,{caught,C,R,S}}
    end.
