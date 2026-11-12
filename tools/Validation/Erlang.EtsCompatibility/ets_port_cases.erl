%% Focused ETS port observations. Test infrastructure; not a replacement ETS backend.
-module(ets_port_cases).
-export([write/1,capture/2,worker/3,scenario/1,scenario_worker/2,fold/2,init/1,init_broken/1,make_dump/1,verify_dump/1]).

write(Directory) ->
    Foreign=filename:join(Directory,"original.dump"),
    ForeignData=make_dump(Foreign),
    ok=file:write_file(filename:join(Directory,"dump-data.etf"),term_to_binary(ForeignData)),
    ok=file:write_file(filename:join(Directory,"features.etf"),term_to_binary([
        {enabled_features,erl_features:enabled()},{keywords,erl_features:keywords()}])),
    Cases = [{F,Args} || {F,A} <- ets:module_info(exports),
                          F =/= module_info, F =/= internal_request_all,
                          Args <- args(F,A)],
    Rows = [{F,Args,capture(F,Args)} || {F,Args} <- Cases],
    ok=file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Rows)),
    Scenarios=[{S,scenario_capture(S)} || S <- [storage,folds,initialization,continuations,match_specs,query,all_tables,shell_match_spec]],
    ok=file:write_file(filename:join(Directory,"effects.etf"),term_to_binary(Scenarios)),
    _=file:make_dir(filename:join(Directory,"filesystem")),
    FileCases=[{files,filename:join(Directory,"filesystem"),Size,Extended,Verify}
               || Size<-[0,1,99,100,101,401], Extended<-[[],[object_count],[md5sum],[object_count,md5sum]], Verify<-[false,true]],
    FileRows=[{S,scenario_capture(S)} || S<-FileCases++[{foreign_dump,Foreign}]++[
        {dets_bridge,filename:join(Directory,"filesystem"),Type} || Type<-[set,bag,duplicate_bag]]++[
        {corrupt_dump,filename:join(Directory,"filesystem"),Kind,V} ||
            Kind<-[bad_header,count_mismatch,missing_footer,bad_footer_count,bad_footer_md5,future_version,bad_existing,
                   missing_name,bad_named,bad_compressed,bad_read_tuple],V<-[false,true]]++[
        {private_save,filename:join(Directory,"filesystem")}]],
    ok=file:write_file(filename:join(Directory,"filesystem.etf"),term_to_binary(FileRows)),
    BrowseCases=[{browse,Height,Width,Inputs} || {Height,Width,Inputs} <- [
        {40,80,["q\n"]},{2,30,["p2\n","c\n","q\n"]},{2,30,["pbad\n","q\n"]},
        {2,30,["/alpha\n","c\n","q\n"]},{2,30,["/[\n","q\n"]},
        {2,30,["k\n"]},{40,80,[]},{1,12,["\n","q\n"]},
        {default,default,["q\n"]},{2,default,["q\n"]}]],
    ok=file:write_file(filename:join(Directory,"browse.etf"),term_to_binary([{S,scenario_capture(S)}||S<-BrowseCases++[regex_captures,display_inventory]])),
    ok=file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(ets:module_info(exports))),
    ok.

args(i,_) -> [];
args(all,_) -> [];
args(fun2ms,_) -> [[0],[not_a_fun],[fun ?MODULE:init/1]];
args(file2tab,1) -> [];
args(file2tab,2) -> [["ignored",X] || X <- [42,[bad],[{verify,no}], [{table,a},{table,b}]]];
args(tabfile_info,_) -> [[42]];
args(tab2file,2) -> [[missing_table,"ignored"]];
args(tab2file,3) -> [[missing_table,"ignored",[]]] ++ [[ets_port_live,"ignored",X] || X <- [42,[bad],[{extended_info,[bad]}],[{extended_info,42}],[{sync,no}],[{extended_info,[]},{extended_info,[]}]]];
args(F,_) when F=:=foldl;F=:=foldr -> [[not_a_fun,0,missing_table],
    [not_a_fun,0,ets_port_live],[fun ?MODULE:fold/2,[],ets_port_live],[fun ?MODULE:init/1,[],ets_port_live]];
args(init_table,_) -> [[missing_table,not_a_fun],[ets_port_live,not_a_fun],
    [ets_port_live,fun ?MODULE:init/1],[ets_port_live,fun ?MODULE:fold/2],[ets_port_live,fun ?MODULE:init_broken/1]];
args(from_dets,_) -> [[ets_port_live,missing_dets]];
args(to_dets,_) -> [[ets_port_live,missing_dets]];
args(repair_continuation,_) -> [['$end_of_table',bad], [bad,bad], [{a,1,2,bad,[],3},[]], [{a,k,e,1,bad,[],2,3},[]]];
args(match_spec_run,_) -> [[[],bad],[bad,bad]];
args(test_ms,_) -> [[X,M] || X <- [0,{}, {a,1}], M <- [[],bad,[{'_',[],['$_']}],[{{'$1','$2'},[],['$1']}]]];
args(new,_) -> [[ets_port_test,X] || X <- [bad,[bad],[{keypos,0}],[{heir,bad}]]];
args(is_compiled_ms,_) -> [[bad],[42],[[]]];
args(match_spec_compile,_) -> [[X] || X <- [[],bad,[{'_',[],['$_']}],[{'_',[],[true]}],[bad]]];
args(whereis,_) -> [[missing_table],[42]];
args(info,2) -> [[ets_port_live,X] || X <- [type,protection,named_table,size,keypos,name,heir,compressed,write_concurrency,read_concurrency,fixed]];
args(table,2) -> [[ets_port_live,X] || X <- [[{n_objects,0}],[{traverse,bad}],[{traverse}],[{traverse,bad,extra}],
    [{traverse,select}|bad],[{traverse,first_next}|bad]]];
args(tab2list,_) -> [[ets_port_live]|default_args(1)];
args(delete_all_objects,_) -> [[ets_port_live]|default_args(1)];
args(match_delete,_) -> [[ets_port_live,{1,'_'}],[ets_port_live,'_']|default_args(2)];
args(lookup,_) -> [[ets_port_live,X] || X<-[1,1.0,9]]++default_args(2);
args(lookup_element,3) -> [[ets_port_live,1,X] || X<-[1,2,3,0,-1,1.0]]++default_args(3);
args(lookup_element,4) -> [[ets_port_live,9,X,default] || X<-[1,2,3,0,-1,1.0]]++default_args(4);
args(update_counter,3) -> [[ets_port_live,1,X] || X<-[2,{2,3},[{2,1},{2,2}],{2,100,20,0}]]++default_args(3);
args(update_element,3) -> [[ets_port_live,1,X] || X<-[{2,changed},[{2,one},{2,two}],{1,5}]]++default_args(3);
args(F,1) when F=:=select; F=:=select_reverse; F=:=match; F=:=match_object -> [['$end_of_table']|default_args(1)];
args(F,A) -> [lists:duplicate(A, X) || X <- [bad,0,[],{},<<>>], F=/=internal_delete_all].
default_args(A) -> [lists:duplicate(A,X)||X<-[bad,0,[],{},<<>>]].

capture(F,Args) ->
    Parent=self(),
    spawn(fun()->worker(F,Args,Parent) end),
    receive {_,Result}->Result end.

worker(F,Args,To) ->
    T=ets:new(ets_port_live,[named_table,ordered_set]),
    Id=ets:whereis(T),
    ets:insert(T,[{1,10},{2,20}]),
    Result = try apply(ets,F,Args) of
        Value when is_reference(Value) -> {ok,reference};
        Value -> {ok,Value}
    catch C:R:S -> {exception,C,R,S}
    after ets:delete(T)
    end,
    To!{self(),canonical(Result,Id)}.

%% Alpha-rename only this worker's table identity, retaining all alias occurrences.
%% No stack frame, reason, location or other reference is removed.
canonical(T,Id) when T=:=Id -> table_reference;
canonical(T,Id) when is_tuple(T) -> list_to_tuple([canonical(V,Id)||V<-tuple_to_list(T)]);
canonical([H|T],Id) -> [canonical(H,Id)|canonical(T,Id)];
canonical(T,_) -> T.

scenario_capture(Scenario) ->
    Parent=self(),spawn(fun()->scenario_worker(Scenario,Parent)end),
    receive {_,Value}->Value end.
scenario_worker(Scenario,To) ->
    Result=try scenario(Scenario) catch C:R:S -> {exception,C,R,S} end,
    To!{self(),Result}.

scenario(storage) ->
    [begin
         T=ets:new(ets_port_test,[Type,named_table,public]),
         true=ets:insert(T,[{1,a},{2,b},{1,a},{1,c},{3,d}]),
         Result={Type,lists:sort(ets:tab2list(T)),ets:lookup(T,1),ets:member(T,2),
                 ets:lookup_element(T,2,2),ets:lookup_element(T,9,2,none),
                 ets:insert_new(T,[{4,e},{2,f}]),ets:lookup(T,4),
                 lists:sort(ets:take(T,1)),ets:select_count(T,[{'_',[],[true]}]),
                 ets:select_delete(T,[{'_',[],[true]}]),ets:tab2list(T)},
         ets:delete(T), Result
     end || Type <- [set,ordered_set,bag,duplicate_bag]];
scenario(folds) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    ets:insert(T,[{X,X*X} || X <- lists:seq(1,20)]),
    L=ets:foldl(fun ?MODULE:fold/2,[],T),
    R=ets:foldr(fun ?MODULE:fold/2,[],T),
    E=try ets:foldl(fun(_,_) -> erlang:error(fold_failure) end,[],T)
      catch error:fold_failure -> caught end,
    Result={L,R,E,ets:info(T,fixed)},ets:delete(T),Result;
scenario(initialization) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    ets:insert(T,{old,data}),
    true=ets:init_table(T,fun ?MODULE:init/1),
    Rows=ets:tab2list(T),
    E=try ets:init_table(T,fun(read)-> { [{ok,1},bad],fun(close)->put(closed,true);(read)->end_of_input end} end)
      catch exit:_ -> {caught,get(closed)} end,
    Result={Rows,E,ets:tab2list(T)},ets:delete(T),Result;
scenario(continuations) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    ets:insert(T,[{X,X} || X <- lists:seq(1,25)]),
    MS=[{'_',[],['$_']}],
    S=drain(select,ets:select(T,MS,3)),
    R=drain(select_reverse,ets:select_reverse(T,MS,4)),
    M=drain(match,ets:match(T,{'$1','_'},5)),
    O=drain(match_object,ets:match_object(T,'_',6)),
    C=ets:select(T,MS,2),{Head,Cont}=C,
    Repaired=ets:repair_continuation(Cont,MS),
    Result={S,R,M,O,Head,Cont=:=Repaired},ets:delete(T),Result;
scenario(match_specs) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    ets:insert(T,[{X,X} || X <- lists:seq(1,10)]),
    MS=[{{'$1','$2'},[{'>','$1',4}],[{{'$1',{'+','$2',100}}}]}],
    C=ets:match_spec_compile(MS),
    Run=ets:match_spec_run(ets:tab2list(T),C),
    N=ets:select_replace(T,MS),
    true=ets:match_delete(T,{2,'_'}),
    Result={ets:is_compiled_ms(C),Run,N,ets:tab2list(T),ets:delete_all_objects(T),ets:tab2list(T)},
    ets:delete(T),Result;
scenario(query) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    ets:insert(T,[{X,X*X} || X <- lists:seq(1,23)]),
    Result=[qlc:e(ets:table(T,Opts)) || Opts <- [[],[{traverse,first_next}],[{traverse,last_prev}],
        [{n_objects,1}],[{traverse,{select,[{{'$1','$2'},[{'>','$1',10}],['$_']}]}}]]],
    ets:delete(T),Result;
scenario(all_tables) ->
    T=ets:new(ets_port_test,[named_table]),
    Result=lists:member(T,ets:all()),
    ets:delete(T),Result;
scenario({files,Directory,Size,Extended,Verify}) ->
    File=filename:join(Directory,"table.dump"),
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    Data=[{X,<<X:32>>,X/3} || X<-lists:seq(1,Size)],
    true=ets:insert(T,Data),
    ok=ets:tab2file(T,File,[{extended_info,Extended},{sync,true}]),
    true=ets:delete(T),
    Info=ets:tabfile_info(File),
    {ok,T}=ets:file2tab(File,[{verify,Verify}]),
    Loaded=ets:tab2list(T),
    true=ets:delete(T),
    ok=file:delete(File),
    {Info,Data=:=Loaded,length(Loaded)};
scenario({browse,Height,Width,Inputs}) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    ets:insert(T,[{1,"alpha"},{2,"beta"},{3,"gamma"}]),
    Device=ets_port_device:start(Inputs),
    Old=group_leader(),true=group_leader(Device,self()),
    Result=try case {Height,Width} of
        {default,default}->ets:i(T);
        {_,default}->ets:i(T,Height);
        _->ets:i(T,Height,Width)
    end after group_leader(Old,self()) end,
    Output=ets_port_device:snapshot(Device),Device!stop,
    Alive=ets:whereis(T)=/=undefined,
    case Alive of true->ets:delete(T);false->ok end,
    {Result,Output,Alive};
scenario(regex_captures) ->
    {ok,Re}=re:compile(<<"(a)(b)?">>,[]),
    [re:run(Subject,Re,Options) || Subject<-[<<"a">>,<<"ab">>,<<"z">>],
        Options<-[[],[{capture,all,binary}],[{capture,none}]]];
scenario(display_inventory) ->
    T=ets:new(ets_port_test,[ordered_set,named_table]),
    Device=ets_port_device:start([]),Old=group_leader(),group_leader(Device,self()),
    Result=try ets:i() after group_leader(Old,self()) end,
    Output=ets_port_device:snapshot(Device),Device!stop,ets:delete(T),
    {Result,lists:sublist(string:split(Output,"\n",all),2),string:find(Output,"ets_port_test")=/=nomatch};
scenario(shell_match_spec) ->
    AST={'fun',0,{clauses,[{clause,0,[{tuple,0,[{var,0,'K'},{var,0,'V'}]}],[],[{var,0,'K'}]}]}},
    {value,F,[]}=erl_eval:expr(AST,[],none),
    MS=ets:fun2ms(F),
    {MS,ets:test_ms({one,two},MS)};
scenario({dets_bridge,Directory,Type}) ->
    File=filename:join(Directory,"bridge.dets"),_=file:delete(File),
    T=ets:new(ets_port_test,[Type,named_table]),
    true=ets:insert(T,[{a,1},{a,2},{a,1},{b,3}]++[{X rem 11,X}||X<-lists:seq(1,251)]),
    {ok,D}=dets:open_file(ets_port_disk,[{file,File},{type,Type}]),
    ok=dets:insert(D,{old,ignored}),
    D=ets:to_dets(T,D),
    Disk=lists:sort(dets:match_object(D,'_')),
    true=ets:delete_all_objects(T),true=ets:insert(T,{kept,0}),
    true=ets:from_dets(T,D),
    Loaded=lists:sort(ets:tab2list(T)),
    ok=dets:close(D),ets:delete(T),ok=file:delete(File),
    {Disk,Loaded};
scenario({foreign_dump,File}) ->
    {ok,T}=ets:file2tab(File,[{verify,true}]),
    Data=ets:tab2list(T),
    Result={Data,ets:info(T,type),ets:info(T,keypos),ets:info(T,compressed)},
    ets:delete(T),Result;
scenario({corrupt_dump,Directory,Kind,Verify}) ->
    File=filename:join(Directory,"corrupt.dump"),_=file:delete(File),
    Ext=case Kind of missing_footer->[md5sum];bad_footer_md5->[md5sum];bad_existing->[md5sum];bad_footer_count->[object_count];_->[] end,
    Size=case Kind of count_mismatch->3;_->2 end,
    Major=case Kind of future_version->2;_->1 end,
    Fields0=[
        {name,ets_corrupt_test},{type,set},{protection,public},{named_table,true},{keypos,1},{size,Size},
        {major_version,Major},{minor_version,0},{extended_info,Ext}],
    Fields=case Kind of
        missing_name->[{size,Size}|tl(Fields0)];
        bad_named->lists:keyreplace(named_table,1,Fields0,{named_table,unknown});
        bad_compressed->Fields0++[{compressed,unknown}];
        bad_read_tuple->Fields0++[{read_concurrency,true,extra}];
        _->Fields0 end,
    Header=case Kind of bad_header->bad_header;_->list_to_tuple(Fields) end,
    End=case Kind of
        bad_footer_count->[['$end_of_table',[{count,3}]]];
        K when K=:=bad_footer_md5;K=:=bad_existing->[['$end_of_table',[{md5,<<0:128>>}]]];_->[] end,
    Name=make_ref(),{ok,Name}=disk_log:open([{name,Name},{file,File},{repair,truncate}]),
    ok=disk_log:log_terms(Name,[Header,{a,1},{b,2}]++End),ok=disk_log:close(Name),
    {TableName,Opts}=case Kind of
        bad_existing->Target=ets:new(ets_existing_target,[ordered_set,named_table]),
            ets:insert(Target,{kept,0}),{ets_existing_target,[{table,Target},{verify,Verify}]};
        _->{ets_corrupt_test,[{verify,Verify}]} end,
    R=ets:file2tab(File,Opts),
    Result=case R of {ok,T}->{ok,lists:sort(ets:tab2list(T))};Other->Other end,
    Alive=ets:whereis(TableName)=/=undefined,
    case Alive of true->ets:delete(TableName);false->ok end,
    ok=file:delete(File),{Result,Alive};
scenario({private_save,Directory}) ->
    File=filename:join(Directory,"private.dump"),_=file:delete(File),
    T=ets:new(ets_private_save,[private,named_table]),ets:insert(T,{kept,1}),
    Parent=self(),spawn(fun()->Parent!{private_result,ets:tab2file(T,File)}end),
    R=receive {private_result,Value}->Value end,
    Exists=case file:read_file_info(File) of {error,enoent}->false;_->true end,
    Result={R,Exists,ets:info(T,fixed),ets:tab2list(T)},ets:delete(T),Result.

make_dump(File) ->
    T=ets:new(ets_cross_reference,[ordered_set,named_table,private,compressed,{keypos,2}]),
    Data=[{row,X,#{text=><<"alpha">>,float=>X/7,big=>(1 bsl 130)+X}} || X<-lists:seq(1,41)],
    true=ets:insert(T,Data),
    ok=ets:tab2file(T,File,[{extended_info,[object_count,md5sum]},{sync,true}]),
    ets:delete(T),Data.
verify_dump(File) ->
    {ok,T}=ets:file2tab(File,[{verify,true}]),
    Expected=[{row,X,#{text=><<"alpha">>,float=>X/7,big=>(1 bsl 130)+X}} || X<-lists:seq(1,41)],
    Expected=ets:tab2list(T),2=ets:info(T,keypos),true=ets:info(T,compressed),
    ets:delete(T),ok.

fold(Row,Acc) -> [Row|Acc].
init(read) -> {[{1,a},{2,b}],fun(read)->end_of_input;(close)->ok end};
init(close) -> ok.
init_broken(read) -> {[{kept,7}|bad],fun(close)->put(closed,true) end};
init_broken(close) -> ok.
drain(_, '$end_of_table') -> [];
drain(F,{Rows,C}) -> Rows++drain(F,apply(ets,F,[C])).
