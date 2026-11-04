-module(file_port_cases).
-include_lib("kernel/include/file.hrl").
-export([write/1,worker/3,scenario/2]).

write(Directory) ->
    Fixture=filename:join(Directory,"filesystem"),
    ok=file:make_dir(Fixture),
    ok=file:write_file(filename:join(Fixture,"payload"),<<0,1,2,255,"file">>),
    Exports = lists:sort(file:module_info(exports)--[{module_info,0},{module_info,1}]),
    Rows = cases(Exports)++filesystem_cases(Fixture),
    Captured = [begin reset_fixture(Fixture),{F,A,observe(F,A)} end || {F,A} <- Rows],
    GateDirectory=filename:join(Directory,"runtime"),ok=file:make_dir(GateDirectory),
    Runtime=[{scenario,[S,[GateDirectory]]} || S <- [raw_cycle,normal_cycle,ram_cycle,consult_cycle,compressed_cycle]],
    Gates=[{F,A,observe(F,A)} || {F,A}<-Runtime],
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(Exports)),
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Captured)),
    ok = file:write_file(filename:join(Directory,"runtime-gates.etf"),term_to_binary(Gates)),ok.

reset_fixture(Directory) ->
    ok=file:write_file_info(filename:join(Directory,"payload"),#file_info{atime=946684800},[{time,posix}]).

observe(F,A) ->
    Pid=spawn(?MODULE,worker,[F,A,self()]),
    receive {outcome,V}->V after 10000 -> exit({probe_timeout,F,A,Pid}) end.

worker(F,A,Reply) ->
    erase(),
    Outcome = try case F of scenario -> apply(?MODULE,scenario,A); _ -> apply(file,F,A) end of
        V -> {ok,V}
    catch C:R:S -> {exception,C,R,S} end,
    Reply ! {outcome,Outcome}.

cases(Exports) ->
    Invalid = [{bad,argument},42,<<1:1>>],
    Base = [{F,lists:duplicate(N,{bad,argument})} || {F,N} <- Exports],
    Variants = [{F,replace(I,V,lists:duplicate(N,{bad,argument}))} || {F,N} <- Exports,
                I <- lists:seq(1,N), V <- Invalid],
    Fd = fd(normal),
    Device = [{close,[Fd]},{read,[Fd,0]},{read,[Fd,2]},{read,[Fd,100]},
              {read,[Fd,-1]},{read_line,[Fd]},{write,[Fd,<<"hello">>]},
              {write,[Fd,[65,<<66>>]]},{write,[Fd,[999]]},
              {pread,[Fd,[{0,2},{10,2},{1,0}]]},{pread,[Fd,0,3]},
              {pread,[Fd,-1,3]},{pwrite,[Fd,[{0,<<"a">>},{-1,<<"b">>}]]},
              {pwrite,[Fd,1,<<"a">>]},{position,[Fd,cur]},
              {position,[Fd,{eof,-1}]},{truncate,[Fd]},{sync,[Fd]},
              {datasync,[Fd]},{allocate,[Fd,0,1]},{advise,[Fd,0,4,normal]},
              {read_file_info,[Fd]},{read_file_info,[Fd,[{time,posix}]]},
              {copy,[Fd,Fd]},{copy,[Fd,Fd,any_atom]},
              {copy_opened,[Fd,fd(writer),3]},{copy_opened,[Fd,fd(writer),infinity]},
              {copy_opened,[Fd,fd({fail,enospc}),10]},
              {copy_opened,[Fd,fd(writer),0]},
              {ipread_s32bu_p32bu,[Fd,0,infinity]},
              {ipread_s32bu_p32bu_int,[fd(indirect),0,3]},
              {ipread_s32bu_p32bu_int,[fd(indirect),0,2]},
              {ipread_s32bu_p32bu_int,[fd(zero),0,infinity]},
              {ipread_s32bu_p32bu_int,[fd(short),0,3]},
              {ipread_s32bu_p32bu_int,[Fd,0,1 bsl 31]},
              {ipread_s32bu_p32bu_int,[Fd,0,-1]}],
    Names = [{F,[{bad,argument},[]]} || F <- [delete,read_file,read_file_info,read_link_info]],
    Formats = [{format_error,[R]} || R <- [badarg,system_limit,terminated,enoent,eacces,unknown,
                                         {1,file,undefined_script}]],
    Opens = [{open,[{bad,argument},Mode]} || Mode <- [read,write,read_write,{binary,read},
                  {character,write},bad,[read],[{error,custom}], [raw,ram]]],
    Scripts = [{F,[{bad,argument}]} || F <- [eval,script,consult]],
    Paths = [{F,[[],{bad,argument}]} || F <- [path_eval,path_script,path_consult]],
    Scenarios = [{scenario,[S,[]]} || S <- [copy_effect,copy_short,send_callback,send_failure,
                        send_negative,send_bad_socket,send_bad_chunk,pid_requests,pid_batch,pid_terminated,server_dispatch,delete_tree]],
    lists:usort(Base++Variants++Device++Names++Formats++Opens++Scripts++Paths++Scenarios).

filesystem_cases(Directory) ->
    File=filename:join(Directory,"payload"),
    [{read_file,[File]},{read_file,[File,[]]},{read_file,[File,[raw]]},
     {read_file_info,[File]},{read_file_info,[File,[{time,posix}]]},
     {read_file_info,[File,[raw,{time,universal}]]},{raw_read_file_info,[File]},
     {read_link_info,[File,[{time,posix}]]},{list_dir,[Directory]},
     {list_dir_all,[Directory]}].

replace(I,V,List) -> {A,[_|B]}=lists:split(I-1,List),A++[V|B].
fd(Data) -> {file_descriptor,file_port_device,Data}.

scenario(copy_effect,[]) ->
    R=file:copy_opened(fd(normal),fd(writer),infinity),{R,get({fd(writer),written})};
scenario(copy_short,[]) ->
    R=file:copy_opened(fd(normal),fd(writer),2),{R,get({fd(writer),written})};
scenario(send_callback,[]) ->
    Send=fun(Data)->put(sent,[Data|case get(sent) of undefined->[];X->X end]),ok end,
    Fd=fd(normal),{ok,_}=file:position(Fd,2),
    R=file:sendfile(Fd,Send,1,4,[{chunk_size,2}]),
    {R,get(sent),file:position(Fd,cur)};
scenario(send_failure,[]) ->
    Fd=fd(normal),R=file:sendfile(Fd,fun(_)->{error,closed} end,1,0,[]),
    {R,file:position(Fd,cur)};
scenario(send_negative,[]) -> file:sendfile(fd(normal),fun(_)->ok end,0,-1,[]);
scenario(send_bad_socket,[]) -> file:sendfile(fd(normal),bad_socket,0,0,[]);
scenario(send_bad_chunk,[]) -> file:sendfile(fd(normal),fun(_)->ok end,0,1,[{chunk_size,bad}]);
scenario(pid_requests,[]) ->
    Pid=spawn(fun loop/0),
    R=[file:position(Pid,cur),file:sync(Pid),file:datasync(Pid),file:truncate(Pid),
       file:allocate(Pid,0,1),file:advise(Pid,0,1,normal),file:read_file_info(Pid,[]),file:close(Pid)],
    Pid ! stop,R;
scenario(pid_batch,[]) ->
    Pid=spawn(fun loop/0),
    R=[file:pread(Pid,[{0,2},{10,2}]),file:pwrite(Pid,[{0,<<"a">>},{-1,<<"b">>}])],
    Pid ! stop,R;
scenario(pid_terminated,[]) ->
    {Pid,Ref}=spawn_monitor(fun()->ok end),receive {'DOWN',Ref,_,_,_}->ok end,
    [file:close(Pid),file:sync(Pid)];
scenario(server_dispatch,[]) ->
    Server=whereis(file_server_2),true=unregister(file_server_2),
    Pid=spawn(fun record_loop/0),true=register(file_server_2,Pid),
    Info={file_info,undefined,undefined,undefined,undefined,undefined,undefined,
          undefined,undefined,undefined,undefined,undefined,undefined,undefined},
    try
        [file:get_cwd(),file:get_cwd('c:'),file:set_cwd([dir,"/",[name]]),
         file:delete(["n",97,me]),file:delete("file",[unknown]),
         file:delete("file",[{error,custom}]),file:rename(old,<<"new">>),
         file:make_dir(new_dir),file:del_dir(old_dir),file:list_dir(dir),file:list_dir_all(dir),
         file:read_link(path),file:read_link_all(path),file:altname(path),
         file:make_link(old,new),file:make_symlink(old,new),
         file:read_file(path),file:read_file(path,[]),
         file:write_file(path,[65,<<66>>,[[67]]]),
         file:write_file(path,[65],[append]),
         file:read_file_info(path),file:read_file_info(path,[{time,posix}]),
         file:read_link_info(path,[unknown]),file:write_file_info(path,Info),
         file:write_file_info(path,Info,[]),
         file:change_mode(path,8#644),file:change_owner(path,42),file:change_owner(path,42,43),
         file:change_group(path,43),file:change_time(path,{{2000,1,2},{3,4,5}}),
         file:change_time(path,{{2000,1,2},{3,4,5}},{{2001,2,3},{4,5,6}}),
         file:copy(source,destination),file:copy({source,[binary]},{destination,[exclusive]},some_atom),
         file:open(path,read_write),file:open(path,{binary,read}),file:open(path,{character,write}),
         file:consult(path),file:eval(path,[]),file:script(path,[])]
    after
        true=unregister(file_server_2),Pid ! stop,true=register(file_server_2,Server)
    end;
scenario(delete_tree,[]) ->
    Server=whereis(file_server_2),true=unregister(file_server_2),
    Pid=spawn(fun()->tree_loop([]) end),true=register(file_server_2,Pid),
    try
        R=file:del_dir_r("root"),Pid ! {trace,self()},
        receive {trace,Trace}->{R,Trace} end
    after
        true=unregister(file_server_2),Pid ! stop,true=register(file_server_2,Server)
    end;
scenario(raw_cycle,[Directory]) ->
    cycle(filename:join(Directory,"raw"),[raw,binary,read,write]);
scenario(normal_cycle,[Directory]) ->
    cycle(filename:join(Directory,"normal"),[binary,read,write]);
scenario(ram_cycle,[_]) -> cycle(<<"initial">>,[ram,binary,read,write]);
scenario(compressed_cycle,[Directory]) ->
    Name=filename:join(Directory,"compressed"),
    ok=file:write_file(Name,<<"compressed contents">>,[compressed]),
    {ok,Fd}=file:open(Name,[compressed,binary,read]),R=file:read(Fd,100),ok=file:close(Fd),R;
scenario(consult_cycle,[Directory]) ->
    Name=filename:join(Directory,"terms"),
    ok=file:write_file(Name,<<"{hello,[1,2,3]}.\n42.\n">>),
    file:consult(Name).

cycle(Item,Modes) ->
    case file:open(Item,Modes) of
        {ok,Fd} ->
            W=file:write(Fd,<<"abcdef">>),P=file:position(Fd,0),R=file:read(Fd,3),
            PR=file:pread(Fd,1,2),PW=file:pwrite(Fd,2,<<"XY">>),
            S=file:sync(Fd),C=file:close(Fd),{W,P,R,PR,PW,S,C};
        Error -> Error
    end.

record_loop() -> receive
    {'$gen_call',{Pid,Tag},Request} -> Pid ! {Tag,{observed,Request}},record_loop();
    stop -> ok
end.

tree_loop(Log) -> receive
    {'$gen_call',{Pid,Tag},Request} ->
        Reply=case Request of
            {read_link_info,"root"} -> {ok,#file_info{type=directory}};
            {read_link_info,"root/dir"} -> {ok,#file_info{type=directory}};
            {read_link_info,_} -> {ok,#file_info{type=symlink}};
            {list_dir_all,"root"} -> {ok,["dir","link","file"]};
            {list_dir_all,"root/dir"} -> {error,eacces};
            {del_dir,"root/dir"} -> {error,enotempty};
            _ -> ok
        end,
        Pid ! {Tag,Reply},tree_loop([Request|Log]);
    {trace,From} -> From ! {trace,lists:reverse(Log)},tree_loop(Log);
    stop -> ok
end.

loop() -> receive
    {file_request,From,Ref,{pread,At,N}} ->
        From ! {file_reply,Ref,file_port_device:pread(fd(normal),At,N)},loop();
    {file_request,From,Ref,{pwrite,At,Data}} ->
        From ! {file_reply,Ref,file_port_device:pwrite(fd(normal),At,Data)},loop();
    {file_request,From,Ref,{position,_}} -> From ! {file_reply,Ref,{ok,0}},loop();
    {file_request,From,Ref,_} -> From ! {file_reply,Ref,ok},loop();
    stop -> ok
end.
