-module(code_port_cases).
-export([write/1, worker/3, cases/0, lifecycle/1, write_lifecycle/1]).

write(Directory) ->
    true = code:set_path([]),
    Rows = [{F,A,run(F,A)} || {F,A} <- cases()],
    ok = file:write_file(filename:join(Directory,"contracts.etf"),term_to_binary(Rows)),
    Exports = lists:sort(code:module_info(exports) -- [{module_info,0},{module_info,1}]),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(Exports)),
    ok.

run(Function, Arguments) ->
    {Pid,Ref} = spawn_monitor(?MODULE,worker,[Function,Arguments,self()]),
    receive {Pid,Result} ->
        receive {'DOWN',Ref,process,Pid,normal} -> Result end
    end.

worker(Function, Arguments, Parent) -> Parent ! {self(),observe(Function,Arguments)}.

observe(Function, Arguments) ->
    try apply(code,Function,Arguments) of
        Result -> {returned,Result}
    catch Class:Reason:Stack -> {caught,Class,Reason,Stack}
    end.

cases() ->
    Bad = [42,{},<<>>],
    One = [load_file,ensure_loaded,delete,purge,soft_purge,is_loaded,
           get_object_code,stick_mod,unstick_mod,is_sticky,which,lib_dir,
           priv_dir,stick_dir,unstick_dir,del_path,del_paths,where_is_file,
           ensure_modules_loaded,atomic_load,prepare_loading,finish_loading,get_doc],
    [{F,[A]} || F <- One, A <- Bad] ++
    [{F,[A]} || F <- [set_path,add_path,add_patha,add_pathz,add_paths,add_pathsa,add_pathsz],
                A <- [42,{},<<>>,bad]] ++
    [{F,[[],bad]} || F <- [set_path,add_path,add_patha,add_pathz,add_paths,add_pathsa,add_pathsz]] ++
    [{lib_dir,[42,42]}, {load_binary,[42,[],<<>>]}, {load_abs,[42]},
     {load_abs,[[],42]}, {load_binary,[missing_code_port,[x],<<>>]},
     {load_binary,[missing_code_port,[],<<>>]},
     {replace_path,[42,42]}, {replace_path,[a,b,bad]},
     {get_doc,[ignored,#{sources=>[]}]}, {get_doc,[ignored,#{}]},
     {get_doc,[missing_code_port,#{sources=>[eep48]}]},
     {objfile_extension,[]}, {get_mode,[]}, {get_path,[]},
     {set_path,[[]]}, {set_path,[[42]]}, {set_path,[[[x]]]},
     {set_path,[[],cache]}, {clear_cache,[]},
     {load_file,[missing_code_port]}, {ensure_loaded,[missing_code_port]},
     {load_abs,["missing_code_port"]}, {get_object_code,[missing_code_port]},
     {is_loaded,[missing_code_port]}, {is_sticky,[missing_code_port]},
     {delete,[missing_code_port]}, {purge,[missing_code_port]}, {soft_purge,[missing_code_port]},
     {which,[missing_code_port]}, {where_is_file,["missing_code_port.beam"]},
     {where_is_file,[[],"anything"]},
     {where_is_file,[[{"cached",["present"]}],"present"]},
     {where_is_file,[[{"cached",["present"]}],"absent"]},
     {get_doc,[missing_code_port]},
     {atomic_load,[[]]}, {prepare_loading,[[]]}, {finish_loading,[{'$prepared$',[]}]},
     {atomic_load,[[a,a,a,b,b]]}, {prepare_loading,[[a,a]]},
     {prepare_loading,[[missing_code_port]]}, {ensure_modules_loaded,[[]]},
     {ensure_modules_loaded,[[missing_code_port]]},
     {add_paths,[[]]}, {add_pathsa,[[]]}, {add_pathsz,[[]]},
     {add_path,[[]]}, {add_patha,[[]]}, {add_pathz,[[]]},
     {del_paths,[[]]}, {del_path,[missing_code_port]},
     {stick_mod,[missing_code_port]}, {unstick_mod,[missing_code_port]},
     {module_status,[42]}, {module_status,[[42]]}, {module_status,[[missing_code_port,42]]}].

write_lifecycle(Directory) ->
    {ok,Bytes} = file:read_file(filename:join(Directory,"code_port_loaded.beam")),
    true = code:set_path([]),
    Result = lifecycle(Bytes),
    ok = file:write_file(filename:join(Directory,"lifecycle.etf"),term_to_binary(Result)).

lifecycle(Bytes) ->
    M = code_port_loaded,
    R1 = code:load_binary(M,"code-port-memory",Bytes),
    R2 = code:is_loaded(M), R3 = code:which(M), R4 = M:value(),
    R5 = code:ensure_loaded(M), R6 = code:module_status(M), R7 = code:module_status([M]),
    R8 = lists:keyfind(M,1,code:all_loaded()),
    R9 = lists:keyfind("code_port_loaded",1,code:all_available()),
    R10 = lists:member(M,code:modified_modules()),
    R11 = code:get_chunk(Bytes,"AtU8"), R12 = code:module_md5(Bytes),
    R13 = code:load_binary(M,"code-port-second",Bytes),
    R14 = code:load_binary(M,"code-port-third",Bytes),
    R15 = code:soft_purge(M), R16 = code:delete(M), R17 = code:soft_purge(M),
    R18 = code:is_loaded(M),
    {ok,Prepared} = code:prepare_loading([{M,"code-port-prepared",Bytes}]),
    R19 = code:finish_loading(Prepared), R20 = M:value(),
    R21 = code:atomic_load([{M,"code-port-atomic",Bytes}]),
    R22 = code:atomic_load([{M,"code-port-refused",Bytes}]),
    R23 = code:purge(M), R24 = code:delete(M), R25 = code:soft_purge(M),
    [R1,R2,R3,R4,R5,R6,R7,R8,R9,R10,R11,R12,R13,R14,R15,R16,R17,R18,
     R19,R20,R21,R22,R23,R24,R25].
