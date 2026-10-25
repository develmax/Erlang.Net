-module(otp_testanalysis475_observer).
-export([worker/1,run/0,groups/0,init_per_group/2,end_per_group/2]).
-file("otp_testanalysis475_observer.erl",1).

worker(Parent) -> Parent ! {finished,run()}.
groups() -> [{main,[parallel],[case_a,{group,nested}]},{nested,[sequence],[case_b,case_c]},{empty,[]}].
init_per_group(Name,Config) -> [{group,Name}|Config].
end_per_group(_Name,_Config) -> ok.
run() ->
    [observe({warnings,Opts,W},fun()->dialyzer_options:build_warnings(Opts,W) end)||Opts<-warning_sequences(),W<-[[],[warn_unknown],all_warnings()]] ++
    [observe({bad_warnings,Opts,W},fun()->dialyzer_options:build_warnings(Opts,W) end)||Opts<-[[bad],[no_return,bad],[0],[{bad,1}],[<<"bad">>],bad,[no_unused|bad]],W<-[[],[warn_unknown],bad]] ++
    [observe({config,Args},fun()->ct_config:prepare_config_list(Args) end)||Args<-configurations()] ++
    [observe({find,G,T,D},fun()->ct_groups:find_groups(?MODULE,G,T,D) end)||G<-[all,main,nested,empty,missing,[main],[main,nested],[[main,nested]],[],[main,missing]],T<-[all,case_b,[case_a,case_b],[],{other,test}],D<-[groups(),[{main,[{group,main}]}],[{main,[{group,missing}]}]]] ++
    [observe({expand,T,C},fun()->ct_groups:expand_groups(T,C,?MODULE) end)||T<-group_references(),C<-[[],confs(),[conf(main,[],[])]]] ++
    [observe({override,T,S},fun()->ct_groups:search_and_override(T,S,?MODULE) end)||T<-[[],confs(),[conf(main,[{suite,?MODULE}],[conf(nested,[],[case_b])])]],S<-overrides()] ++
    [observe({delete,T,A},fun()->ct_groups:delete_subs(T,A) end)||T<-[[],confs(),[conf(nested,[],[case_b])],[bad],bad],A<-[[],confs(),[conf(main,[],[conf(nested,[],[case_b])]),conf(nested,[],[case_b])]]] ++
    [observe({make,P,T},fun()->ct_groups:make_all_conf(?MODULE,P,T) end)||P<-[[],[parallel],[{repeat,2}]],T<-[all,case_a,[case_b],[],bad]] ++
    [observe({make_explicit,N,P,T},fun()->ct_groups:make_conf("C:/otp475/unused",?MODULE,N,P,T) end)||N<-[main,bad],P<-[[],[parallel]],T<-[[],[case_a],bad]] ++
    [observe({make_directory,P,T},fun()->ct_groups:make_all_conf("C:/otp475/unused",?MODULE,P,T) end)||P<-[[],[sequence]],T<-[all,[case_a],[]]] ++
    [observe({generated,A},fun()->dialyzer_utils:is_compiler_generated(A) end)||A<-[[],[0],[1],[-1],[42],[{4,8}],[{0,2}],[bad,7],[compiler_generated],[7,compiler_generated],bad,[1|bad]]] ++
    [observe({family,L},fun()->dialyzer_utils:family(L) end)||L<-[[],[{a,1}],[{b,2},{a,1},{a,1},{a,2}],[{1,a},{1.0,b}],[{a,[]}],[bad],[{a,b,c}],bad,[{a,1}|bad]]] ++
    [observe({foldable,V,M},fun()->dialyzer_utils:is_foldable(cerl:abstract(V),M) end)||V<-[ok,0,1.5,[],[1,2],{a,1},#{a=>1},{#{a=>1}},[#{a=>1}],<<1,2>>],M<-[false,true]] ++
    [observe({core,A},fun()->core_locations(A) end)||A<-[[],[1],[{2,3}],[compiler_generated],[0],[bad,9]]] ++
    [observe(compiler_options,fun()->dialyzer_utils:src_compiler_opts() end)] ++
    [observe({format,V},fun()->io_lib_format:fwrite("~ts: ~tP",["Unknown dialyzer warning option",V,25]) end)||V<-[bad,0,{bad,1},<<"bad">>]].
warning_sequences() -> [[]]++[[X]||X<-warning_options()]++[[X,Y]||X<-warning_options(),Y<-warning_options()].
warning_options() -> [no_return,no_unused,no_unknown,no_improper_lists,no_fun_app,no_match,no_opaque,no_fail_call,no_contracts,no_behaviours,no_undefined_callbacks,unmatched_returns,error_handling,no_missing_calls,specdiffs,overspecs,underspecs,no_underspecs,extra_return,no_extra_return,missing_return,no_missing_return,opaque_union,no_opaque_union,unknown,overlapping_contract].
all_warnings() -> [warn_behaviour,warn_bin_construction,warn_callgraph,warn_contract_extra_return,warn_contract_missing_return,warn_contract_not_equal,warn_contract_opaque,warn_contract_range,warn_contract_subtype,warn_contract_supertype,warn_contract_syntax,warn_contract_types,warn_failing_call,warn_fun_app,warn_map_construction,warn_matching,warn_non_proper_list,warn_not_called,warn_opaque,warn_opaque_union,warn_overlapping_contract,warn_return_no_exit,warn_return_only_exit,warn_undefined_callbacks,warn_unknown,warn_umatched_return].
configurations() ->
    [[],bad,[bad],[{ct_config,[]}],[{ct_config,bad}],[{ct_config,["C:/otp475/a.config","C:/otp475/b.config"]}],
     [{userconfig,[]}],[{userconfig,["callback","a","b"]}],
     [{userconfig,["first","a","and","second","b","c"]}],
     [{userconfig,["first","and","second"]}],[{userconfig,["and"]}],
     [{userconfig,bad}],[{userconfig,[bad]}],[{userconfig,["first"|bad]}],
     [{ct_config,[]},{userconfig,["callback","x"]}],
     [{ct_config,[]},{ct_config,bad}],[{userconfig,["first"]},{userconfig,bad}]].
conf(N,P,T) -> {conf,[{name,N}|P],{?MODULE,init_per_group},T,{?MODULE,end_per_group}}.
confs() -> [conf(main,[parallel],[case_a,conf(nested,[sequence],[case_b,case_c])]),conf(nested,[sequence],[case_b,case_c]),conf(empty,[],[])].
group_references() -> [[],case_a,[case_a,{group,main}],{group,main},{group,nested},{group,empty},{group,missing},{group,main,default},{group,main,[sequence]},{group,main,[],[{nested,[parallel]}]},{group,main,default,[{missing,[]}]},{group,main,bad},[bad],{group,0}].
overrides() -> [[],[{main,default}],[{main,[]}],[{nested,[parallel]}],[{main,[sequence],[{nested,[]}]}],[{main,default,[{missing,[]}]}],[{main,default,bad}],bad].
core_locations(A) ->
    L=cerl:ann_c_literal(A,42),V=cerl:ann_c_var([8],x),T=cerl:ann_c_tuple([compiler_generated],[L,V]),
    {dialyzer_utils:get_location(L,none),dialyzer_utils:get_location(T,none),dialyzer_utils:is_foldable(V,false),dialyzer_utils:is_foldable(T,true)}.
observe(Name,F) ->
    try F() of V -> {Name,{returned,V}}
    catch C:R:S -> {Name,{caught,C,R,S}}
    end.
