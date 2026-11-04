%% DETS contract observer, authored for this port. Original OTP is the oracle.
-module(dets_port_cases).
-export([run/1, errors/0, boot_init/1, error_worker/2, run_worker/3, bad_fold/2, bad_traverse/1]).

boot_init([]) -> {ok, {{one_for_one, 10, 10}, []}}.

run(Dir) ->
    [{Type, table_case(Dir, Type)} || Type <- [set,bag,duplicate_bag]] ++
    [{counter,counter(Dir)}, {scan,scan(Dir)}, {callbacks,callbacks(Dir)},
     {bchunk,bchunk_case(Dir)}, {ets,ets_case(Dir)}, {qlc,qlc_case(Dir)},
     {owners,owners(Dir)}, {options,options_case(Dir)},
     {numbers,numbers(Dir)}, {complex,[complex(Dir,T) || T <- [set,bag,duplicate_bag]]},
     {modes,modes(Dir)}, {repair,repair_case(Dir)}, {fixed,fixed_case(Dir)},
     {callbacks_edge,callbacks_edge(Dir)}, {headers,headers(Dir)},
     {owner_death,owner_death(Dir)}, {errors_live,errors_live(Dir)}, {legacy,legacy(Dir)}, {buffer,buffer_case()}].

open(Dir, Name, Options) ->
    File = filename:join(Dir, atom_to_list(Name) ++ ".dets"),
    _ = file:delete(File),
    {ok, Name} = dets:open_file(Name,[{file,File},{auto_save,infinity}|Options]),
    Name.

table_case(Dir, Type) ->
    T = open(Dir, Type, [{type,Type}]),
    ok = dets:insert(T,[{1,a},{1,b},{1,b},{1.0,c},{2,d}]),
    Before = {lists:sort(dets:lookup(T,1)),dets:lookup(T,1.0),dets:info(T,size),dets:info(T,no_keys)},
    false = dets:insert_new(T,[{3,new},{1,old}]),
    [] = dets:lookup(T,3),
    true = dets:insert_new(T,[{3,first},{3,last}]),
    ok = dets:delete_object(T,{1,b}),
    Middle = {lists:sort(dets:lookup(T,1)),lists:sort(dets:lookup(T,3))},
    ok = dets:delete(T,3),
    Keys = lists:sort(keys(T,dets:first(T))),
    Info = {dets:info(T,type),dets:info(T,keypos),is_pid(dets:info(T,owner)),dets:info(T,version)},
    ok = dets:sync(T),
    true = dets:is_dets_file(dets:info(T,filename)),
    F = dets:info(T,filename),
    ok = dets:close(T),
    {ok,R} = dets:open_file(F),
    Reopened = lists:sort(dets:match_object(R,'_')),
    ok = dets:close(R),
    {Before,Middle,Keys,Info,Reopened}.

keys(_, '$end_of_table') -> [];
keys(T,K) -> [K|keys(T,dets:next(T,K))].

counter(Dir) ->
    T = open(Dir,counter,[]),
    ok = dets:insert(T,{k,1,10}),
    A = dets:update_counter(T,k,2),
    B = dets:update_counter(T,k,{3,-20}),
    ok = dets:insert(T,{big,1 bsl 180}),
    C = dets:update_counter(T,big,1 bsl 200),
    Result = {A,B,C,dets:lookup(T,k)},
    ok = dets:close(T), Result.

scan(Dir) ->
    T = open(Dir,scan,[]),
    ok = dets:insert(T,[{N,N*N} || N <- lists:seq(1,800)]),
    All = lists:sort(dets:match_object(T,'_')),
    Binds = lists:sort(dets:match(T,{'$1',9})),
    MS = [{{'$1','$2'},[{'>','$1',790}],['$1']}],
    Selected = lists:sort(chunks(dets:select(T,MS,2),select)),
    AllChunks = lists:sort(chunks(dets:match_object(T,'_',1),match_object)),
    BindChunks = lists:sort(chunks(dets:match(T,{'$1',9},0),match)),
    ok = dets:safe_fixtable(T,true), ok = dets:safe_fixtable(T,true),
    Fixed1 = dets:info(T,fixed),
    ok = dets:safe_fixtable(T,false), Fixed2 = dets:info(T,fixed),
    ok = dets:safe_fixtable(T,false), Fixed3 = dets:info(T,fixed),
    10 = dets:select_delete(T, [{{'$1','_'},[{'>','$1',790}],[true]}]),
    ok = dets:match_delete(T,{'_',9}),
    Remaining = dets:info(T,size),
    ok = dets:close(T),
    {All =:= AllChunks,Binds,Selected,BindChunks,Fixed1,Fixed2,Fixed3,Remaining}.

chunks('$end_of_table',_) -> [];
chunks({L,C},F) -> L ++ chunks(apply(dets,F,[C]),F).

callbacks(Dir) ->
    T = open(Dir,callbacks,[]),
    Init = fun(read) -> {[{1,a},{2,b},{3,c}], fun input_end/1}; (close) -> ok end,
    ok = dets:init_table(T,Init),
    Fold = dets:foldl(fun({K,_},A)->K+A end,0,T),
    FoldR = dets:foldr(fun({K,_},A)->K+A end,0,T),
    Traverse = lists:sort(dets:traverse(T,fun(X)->{continue,X} end)),
    One = length(dets:traverse(T,fun(X)->{done,X} end)),
    ok = dets:delete_all_objects(T),
    Empty = dets:first(T),
    ok = dets:close(T), {Fold,FoldR,Traverse,One,Empty}.

input_end(read) -> end_of_input;
input_end(close) -> ok.

bchunk_case(Dir) ->
    T = open(Dir,chunks_source,[{type,duplicate_bag}]),
    ok = dets:insert(T,[{a,1},{a,1},{a,2},{b,3}]),
    Format = dets:info(T,bchunk_format),
    true = dets:is_compatible_bchunk_format(T,Format),
    false = dets:is_compatible_bchunk_format(T,<<1,2>>),
    Bins = bchunks(T,start),
    U = open(Dir,chunks_target,[{type,duplicate_bag}]),
    ok = dets:init_table(U,fun(read)->{Bins,fun input_end/1};(close)->ok end,[{format,bchunk}]),
    Result = lists:sort(dets:match_object(U,'_')),
    ok = dets:close(T), ok = dets:close(U), Result.

bchunks(T,C) -> case dets:bchunk(T,C) of '$end_of_table' -> []; {Next,L} -> L ++ bchunks(T,Next) end.

ets_case(Dir) ->
    E = ets:new(transfer,[public]),
    true = ets:insert(E,[{a,1},{b,2}]),
    T = open(Dir,transfer,[]),
    ok = dets:from_ets(T,E),
    true = ets:delete_all_objects(E),
    E = dets:to_ets(T,E),
    Result = lists:sort(ets:tab2list(E)),
    true = ets:delete(E), ok = dets:close(T), Result.

qlc_case(Dir) ->
    T = open(Dir,query,[]),
    ok = dets:insert(T,[{1,a},{2,b},{3,c}]),
    A = lists:sort(qlc:e(dets:table(T))),
    B = lists:sort(qlc:e(dets:table(T,[{traverse,first_next}]))),
    C = lists:sort(qlc:e(dets:table(T,[{traverse,{select,[{{'$1','$2'},[{'>','$1',1}],['$_']}]}}]))),
    ok = dets:close(T), {A,B,C}.

owners(Dir) ->
    T = open(Dir,owners,[]),
    {ok,T} = dets:open_file(T,[{file,dets:info(T,filename)},{auto_save,infinity}]),
    Owner = dets:info(T,owner), {ok,T} = dets:pid2name(Owner),
    ok = dets:close(T), true = is_pid(dets:info(T,owner)),
    ok = dets:close(T),
    {dets:info(T),dets:close(T)}.

options_case(Dir) ->
    T = open(Dir,options,[{keypos,2},{min_no_slots,0},{delayed_write,{0,0}}]),
    ok = dets:insert(T,[{a,1},{b,2}]),
    Result = {dets:lookup(T,2),dets:info(T,keypos),dets:info(T,delayed_write),dets:info(T,no_slots)},
    ok = dets:close(T), Result.

errors() ->
    Inputs = [{lookup,[missing,k]}, {insert,[missing,{a,1}]}, {match,[bad_cont]},
              {safe_fixtable,[missing,wrong]}, {open_file,[t,[{type,ordered_set}]]},
              {select,[missing,[], -1]}, {repair_continuation,[bad_cont,[]]}],
    [{F,try apply(dets,F,A) of V -> {ok,V} catch C:R:S -> {C,R,S} end} || {F,A} <- Inputs].

error_worker(Parent, Tag) -> Parent ! {Tag, errors()}.

run_worker(Parent, Tag, Dir) -> Parent ! {Tag, run(Dir)}.

numbers(Dir) ->
    T = open(Dir,numbers,[]),
    ok = dets:insert(T,[{float,1.25},{1,7},{overflow,1 bsl 2000}]),
    A = dets:update_counter(T,float,0.5),
    B = dets:update_counter(T,1,{1,1.0}),
    C = value_call(update_counter,[T,overflow,1.0]),
    D = {lists:sort(dets:lookup_keys(T,[1,1.0,2.0])),dets:lookup(T,1),dets:lookup(T,2.0)},
    ok = dets:close(T), {A,B,C,D}.

complex(Dir,Type) ->
    T = open(Dir,list_to_atom("complex_" ++ atom_to_list(Type)),[{type,Type}]),
    Keys = {1,1.0,0.0,-0.0,{a,1},[1,2],<<1:3>>,#{1=>a,1.0=>b},'ключ',[]},
    Steps = [begin
        K = element((N rem tuple_size(Keys))+1,Keys),
        O = {K,N rem 7},
        R = case N rem 5 of
            0 -> dets:delete(T,K);
            1 -> dets:insert(T,[O,O]);
            2 -> dets:insert_new(T,[{{new,N},a},O]);
            3 -> dets:delete_object(T,{K,(N+3) rem 7});
            4 -> dets:insert(T,O)
        end,
        {R,lists:sort(dets:lookup(T,K)),dets:member(T,K),dets:info(T,size),dets:info(T,no_keys)}
    end || N <- lists:seq(1,80)],
    All = lists:sort(dets:match_object(T,'_')),
    F = dets:info(T,filename), ok = dets:close(T),
    {ok,T} = dets:open_file(T,[{file,F},{type,Type}]),
    All = lists:sort(dets:match_object(T,'_')),
    ok = dets:close(T), {Type,Steps,All}.

modes(Dir) ->
    T = open(Dir,modes,[{ram_file,true}]),
    ok = dets:insert(T,[{a,1},{b,2}]),
    true = dets:info(T,ram_file),
    F = dets:info(T,filename), ok = dets:sync(T), ok = dets:close(T),
    [begin
        [begin
            {ok,T} = dets:open_file(T,[{file,F},{access,read},{ram_file,Ram}]),
            Result = value_call(Op,Args),
            Data = dets:lookup(T,a), Closed = dets:close(T),
            redact({Result,Data,Closed},F)
        end || {Op,Args} <-
            [{insert,[T,{c,3}]},{delete,[T,a]},{delete_all_objects,[T]},
             {insert_new,[T,{c,3}]},{update_counter,[T,a,1]},
             {select_delete,[T,[{'_',[],[true]}]]}]]
    end || Ram <- [false,true]].

repair_case(Dir) ->
    T = open(Dir,repair,[]),
    Objects = [{N,N*N} || N <- lists:seq(1,300)],
    ok = dets:insert(T,Objects), F = dets:info(T,filename), ok = dets:close(T),
    {ok,Fd} = file:open(F,[raw,binary,read,write]),
    ok = file:pwrite(Fd,8,<<0:32>>), ok = file:close(Fd),
    Needs = dets:open_file(T,[{file,F},{repair,false}]),
    {ok,T} = dets:open_file(T,[{file,F}]),
    Objects = lists:sort(dets:match_object(T,'_')), ok = dets:close(T),
    {ok,T} = dets:open_file(T,[{file,F},{repair,force}]),
    Objects = lists:sort(dets:match_object(T,'_')), ok = dets:close(T),
    {ok,Fd2} = file:open(F,[raw,binary,read,write]),
    {ok,198} = file:position(Fd2,198), ok = file:truncate(Fd2), ok = file:close(Fd2),
    {ok,T} = dets:open_file(T,[{file,F}]),
    Empty = dets:info(T,size), ok = dets:close(T),
    {redact(Needs,F),Empty}.

fixed_case(Dir) ->
    T = open(Dir,fixed,[]), ok = dets:insert(T,[{1,a},{2,b}]),
    ok = dets:safe_fixtable(T,true), ok = dets:safe_fixtable(T,true),
    {_,[{Self,2}]} = dets:info(T,safe_fixed), Self = self(),
    ok = dets:delete_all_objects(T), A = dets:info(T,size),
    ok = dets:insert(T,{3,c}),
    Init = fun input_end/1,
    B = dets:init_table(T,Init),
    ok = dets:safe_fixtable(T,false), C = dets:info(T,fixed),
    ok = dets:safe_fixtable(T,false), D = dets:info(T,fixed),
    ok = dets:close(T), {A,B,C,D}.

callbacks_edge(Dir) ->
    T = open(Dir,callbacks_edge,[]),
    EmptyFold = dets:foldl(not_a_fun,initial,T),
    ok = dets:insert(T,[{1,a},{2,b},{3,c}]),
    Other = dets:traverse(T,fun(_)->custom_result end),
    Fixed = dets:info(T,fixed),
    ok = dets:safe_fixtable(T,false),
    E = ets:new(private_copy,[private]), true = ets:insert(E,{private,1}),
    ok = dets:from_ets(T,E), true = ets:delete(E),
    Single = dets:lookup(T,private),
    ok = dets:close(T), {EmptyFold,Other,Fixed,Single}.

headers(Dir) ->
    F = filename:join(Dir,"header.bin"),
    [begin
        B = case Len >= 20 of true -> <<0:128,V:32,0:((Len-20)*8)>>; false -> <<0:(Len*8)>> end,
        ok = file:write_file(F,B),
        {Len,V,redact(dets:is_dets_file(F),F),redact(dets:file_info(F),F)}
    end || {Len,V} <- [{0,0},{19,0},{20,8},{20,9},{20,10},{187,9},{188,9},{312,9}]].

owner_death(Dir) ->
    F = filename:join(Dir,"owner_death.dets"), _ = file:delete(F),
    Parent = self(),
    {Pid,Mon} = spawn_monitor(fun()->
        {ok,owner_death} = dets:open_file(owner_death,[{file,F}]),
        ok = dets:insert(owner_death,{saved,1}),
        Parent ! {opened,dets:info(owner_death,owner)}
    end),
    Worker = receive {opened,W} -> W end,
    WMon = erlang:monitor(process,Worker),
    receive {'DOWN',Mon,process,Pid,normal} -> ok end,
    receive {'DOWN',WMon,process,Worker,_} -> ok end,
    {ok,owner_death} = dets:open_file(owner_death,[{file,F},{repair,false}]),
    Data = dets:lookup(owner_death,saved), ok = dets:close(owner_death), Data.

errors_live(Dir) ->
    T = open(Dir,errors_live,[{keypos,2}]),
    ok = dets:insert(T,{a,1}),
    Inputs = [{insert,[T,{short}]},{insert,[T,[{a,1}|bad_tail]]},
              {lookup_keys,[T,[]]},{lookup_keys,[T,[1|bad_tail]]},
              {slot,[T,-1]},{slot,[T,wrong]},{bchunk,[T,wrong]},
              {update_counter,[T,missing,1]},
              {from_ets,[T,missing_ets]}, {to_ets,[T,missing_ets]},
              {foldl,[fun ?MODULE:bad_fold/2,0,T]}, {foldr,[fun ?MODULE:bad_fold/2,0,T]},
              {traverse,[T,fun ?MODULE:bad_traverse/1]}],
    Result = [{F,try apply(dets,F,A) of V -> {ok,V} catch C:R:S -> {C,R,S} end} || {F,A} <- Inputs],
    ok = dets:close(T), Result.

value_call(F,A) -> try apply(dets,F,A) catch C:R -> {C,R} end.

bad_fold(_,_) -> erlang:error(callback_failure).
bad_traverse(_) -> erlang:error(callback_failure).

buffer_case() ->
    B = prim_buffer:new(),
    Empty = buffer_call(read_iovec,[B,65]),
    Unlock = buffer_call(unlock,[B]),
    acquired = prim_buffer:try_lock(B), busy = prim_buffer:try_lock(B), ok = prim_buffer:unlock(B),
    ok = prim_buffer:write(B,[<<"a">>,<<"bc">>]),
    ok = prim_buffer:write(B,[<<"de">>]),
    Size = prim_buffer:size(B),
    Found = prim_buffer:find_byte_index(B,$d), Missing = prim_buffer:find_byte_index(B,$z),
    ok = prim_buffer:skip(B,2), Tail = prim_buffer:read(B,3),
    BadWrite = buffer_call(write,[B,[1]]), BadRead = buffer_call(read,[B,1]),
    ok = prim_buffer:write(B,[binary:copy(<<1>>,2050)]),
    ok = prim_buffer:write(B,[<<2>>]),
    Vec = prim_buffer:read_iovec(B,2051), Large = [byte_size(V) || V <- Vec],
    All = iolist_to_binary(Vec),
    {Empty,Unlock,Size,Found,Missing,Tail,BadWrite,BadRead,Large,byte_size(All),binary:last(All)}.

buffer_call(F,A) -> try apply(prim_buffer,F,A) catch C:R -> {C,R} end.

legacy(Dir) ->
    [begin
        F = filename:join(Dir,Name),
        case dets:open_file(F) of
            {ok,T} ->
                All = lists:sort(dets:match_object(T,'_')),
                Hash = dets:info(T,hash), Format = dets:info(T,bchunk_format),
                ok = dets:insert(T,{4,d}), ok = dets:close(T),
                {ok,U} = dets:open_file(F), Read = dets:lookup(U,4),
                Closed = dets:close(U),
                {Name,All,Hash,Format =:= undefined,redact(Read,F),redact(Closed,F)};
            Error -> {Name,redact(Error,F)}
        end
    end || Name <- ["version_8.dets","version_9a.dets","version_9b_phash.dat"]].

redact(Value,Value) -> file_path;
redact(T,F) when is_tuple(T) -> list_to_tuple([redact(V,F) || V <- tuple_to_list(T)]);
redact([H|T],F) -> [redact(H,F)|redact(T,F)];
redact(V,_) -> V.
