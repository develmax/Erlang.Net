-module(beam_code_server_subject410).
-export([start_link/1,worker/2,probe/1,shutdown/0]).

start_link(Root) ->
    Parent=self(), Pid=spawn_link(fun()->owner_init(Parent,Root) end),
    receive {Pid,ready}->{ok,Pid} end.

owner_init(Parent,Root) ->
    process_flag(trap_exit,true),
    register(?MODULE,self()),
    {ok,Server}=code_server:start_link([Root,embedded]),
    Tag=make_ref(), Server!{system,{self(),Tag},get_status},
    receive
        {Tag,{status,Server,{module,code_server},[_,running,Owner,[],State]}} ->
            true=Owner=:=self(), Db=element(6,State), NameDb=element(7,State),
            Parent!{self(),ready}, owner_loop(Server,Db,NameDb)
    end.

owner_loop(Server,Db,NameDb) ->
    receive
        {From,Ref,ready}->
            From!{Ref,is_process_alive(Server) andalso whereis(code_server)=:=Server},
            owner_loop(Server,Db,NameDb);
        {From,Ref,tables}->
            _=ets:lookup(Db,init),_=ets:lookup(NameDb,"erts"),
            From!{Ref,true},owner_loop(Server,Db,NameDb);
        {From,Ref,shutdown}->
            Monitor=monitor(process,Server),exit(Server,shutdown),
            receive {'DOWN',Monitor,process,Server,shutdown}->ok end,
            Gone1=gone(Db),Gone2=gone(NameDb),
            From!{Ref,{Gone1,Gone2,whereis(code_server)=:=undefined,
                       persistent_term:get(code_server)}},
            stopped();
        {'EXIT',_,_}->owner_loop(Server,Db,NameDb)
    end.

stopped()->receive _->stopped() end.
gone(Table)->try ets:lookup(Table,init) of _->false catch error:badarg->true end.
rpc(Request)->Ref=make_ref(),?MODULE!{self(),Ref,Request},receive {Ref,Value}->Value end.
shutdown()->rpc(shutdown).

worker(Parent,Case)->
    Outcome=try probe(Case) of V->{returned,V} catch C:R:S->{caught,C,R,S} end,
    Parent!{Case,Outcome},ok.

probe(ready)->rpc(ready);
probe(mode)->code_server:get_mode();
probe(empty_path)->code_server:call(get_path);
probe(missing)->code_server:is_loaded(beam_code_server_missing410);
probe(preloaded)->code_server:is_loaded(init);
probe(loaded)->
    {file,Path}=code_server:is_loaded(?MODULE),
    filename:basename(Path);
probe(request_reply)->
    []=code_server:call(get_path),embedded=code_server:get_mode(),ok;
probe(finish_empty)->code_server:call({finish_loading,[],false});
probe(owned_tables)->rpc(tables);
probe(wrong_arity)->apply(code_server,is_loaded,[]).
