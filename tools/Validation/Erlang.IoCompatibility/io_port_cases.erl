-module(io_port_cases).
-export([write/1, probe/1, observe/2, server/3]).

write(Directory) ->
    _ = erl_features:keywords(),
    Features = [{Key,persistent_term:get({erl_features,Key})} || Key <-
                   [init_done,enabled_features,keywords,feature_specs]],
    ok = file:write_file(filename:join(Directory,"features.etf"),term_to_binary(Features)),
    Cases = cases(),
    Rows = [{C,capture(C)} || C <- Cases],
    ok = file:write_file(filename:join(Directory,"cases.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),
                         term_to_binary(io:module_info(exports)--[{module_info,0},{module_info,1}])),
    ok.

capture(Case) ->
    {Pid,Ref} = spawn_monitor(?MODULE,observe,[Case,self()]),
    receive
        {observed,Result} -> demonitor(Ref,[flush]), Result;
        {'DOWN',Ref,process,Pid,Reason} -> erlang:error({observer_died,Case,Reason})
    after 30000 -> erlang:error({observer_timeout,Case})
    end.

observe(Case,Parent) ->
    Result = probe(Case),
    Parent ! {observed,Result}.

probe({scenario,Name}) ->
    scenario(Name);
probe({upstream,Name}) ->
    apply(io_SUITE,Name,[[]]);
probe({F,Args,Reply}) ->
    Parent = self(),
    Server = spawn(?MODULE,server,[Parent,Reply,[]]),
    Old = group_leader(),
    OldUser = whereis(user),
    Password = F =:= get_password andalso Args =:= [],
    case Password of
        true -> case OldUser of undefined -> ok; _ -> unregister(user) end, register(user,Server);
        false -> ok
    end,
    true = group_leader(Server,self()),
    ActualArgs = [replace(A,Server) || A <- Args],
    Result = try apply(io,F,ActualArgs) of
                 Value -> {ok,Value}
             catch Class:Reason:Stack -> {exception,Class,Reason,Stack}
             after group_leader(Old,self()),
                   case Password of
                       true -> unregister(user), case OldUser of undefined -> ok; _ -> register(user,OldUser) end;
                       false -> ok
                   end
             end,
    Server ! {finish,Parent},
    receive {finished,Server,Requests} -> {normalize(Result,Server),Requests} end.

replace('$device',Server) -> Server;
replace('$self',_) -> self();
replace(X,_) -> X.

normalize(X,Server) when X =:= Server -> '$device';
normalize(X,_) when X =:= self() -> '$self';
normalize(X,_) when is_reference(X) -> '$reference';
normalize(X,Server) when is_tuple(X) -> list_to_tuple([normalize(I,Server) || I <- tuple_to_list(X)]);
normalize([H|T],Server) -> [normalize(H,Server)|normalize(T,Server)];
normalize(X,_) -> X.

server(Parent,Reply,Requests) ->
    receive
        {io_request,From,Ref,Request} ->
            From ! {io_reply,Ref,Reply},
            server(Parent,Reply,[Request|Requests]);
        {finish,Parent} -> Parent ! {finished,self(),lists:reverse(Requests)}
    end.

scenario(dead_device) ->
    {Pid,Ref} = spawn_monitor(fun() -> ok end),
    receive {'DOWN',Ref,process,Pid,normal} -> ok end,
    A = io:request(Pid,nl),
    B = try io:put_chars(Pid,"abc") catch error:R -> R end,
    {A,B,process_info(self(),monitors)};
scenario(dies_during_request) ->
    Pid = spawn(fun() ->
        receive {io_request,From,_,_} ->
            From ! unrelated,
            From ! {'EXIT',self(),marker},
            exit(normal)
        end
    end),
    Reply = io:request(Pid,nl),
    {Reply,process_info(self(),messages),process_info(self(),monitors)};
scenario(reply_then_die) ->
    Pid = spawn(fun() ->
        receive {io_request,From,Ref,_} ->
            From ! unrelated,
            From ! {io_reply,Ref,success}
        end
    end),
    Reply = io:request(Pid,nl),
    {Reply,process_info(self(),messages),process_info(self(),monitors)};
scenario(selective_receive) ->
    self() ! noise,
    self() ! {io_reply,wrong_ref,stale},
    Server = spawn(fun() -> receive {io_request,From,Ref,_} -> From ! {io_reply,Ref,ok} end end),
    ok = io:request(Server,hello),
    process_info(self(),messages);
scenario(callback_output) ->
    Server = spawn(fun callback_server/0),
    A = io:format(Server,"~p ~ts",[{a,42},[16#410,16#1f600]]),
    B = io:fwrite(Server,"~.16B",[255]),
    C = io:write(Server,[a,{b,1}]),
    D = io:nl(Server),
    Server ! stop,
    {A,B,C,D};
scenario(callback_input) ->
    Server = spawn(fun callback_server/0),
    A = io:fread(Server,"42 hello\n","~d ~s"),
    B = io:read(Server,"{hello,42}. "),
    C = io:parse_erl_exprs(Server,"foo(42). "),
    D = io:scan_erl_form(Server,"-module(foo). "),
    Server ! stop,
    {A,B,C,D};
scenario(format_dependency) ->
    try io_lib_format:fwrite("~p ~ts",[{a,42},[16#410,16#1f600]])
    catch C:R:S -> {dependency_error,C,R,S} end.

callback_server() ->
    receive
        {io_request,From,Ref,Request} ->
            Reply = try case Request of
                {put_chars,Encoding,M,F,A} ->
                    {output,unicode:characters_to_binary(apply(M,F,A),Encoding)};
                {put_chars,Encoding,Chars} ->
                    {output,unicode:characters_to_binary(Chars,Encoding)};
                {get_until,_,Input,M,F,A} ->
                    case apply(M,F,[[],Input|A]) of
                        {done,Result,_Rest} -> Result;
                        Other -> {unexpected,Other}
                    end
            end catch C:R:S -> {callback_error,C,R,S} end,
            From ! {io_reply,Ref,Reply},
            callback_server();
        stop -> ok
    end.

cases() ->
    Output = [{put_chars,["abc"]},{put_chars,['$device',[16#410,16#1f600]]},
              {put_chars,['$device',[<<"hello">>,[10]]]},
              {put_chars,['$device',[16#d800]]}, {put_chars,['$device',[1|2]]},
              {nl,[]},{nl,['$device']},{write,[{a,1}]},{write,['$device',#{x=>1}]},
              {format,["literal"]},{format,["~p",[42]]},{format,['$device',"~ts",["abc"]]},
              {fwrite,["literal"]},{fwrite,["~p",[42]]},{fwrite,['$device',"~p",[42]]}],
    Inputs = [{get_chars,[">",0]},{get_chars,['$device',">",4]},
              {get_line,[">"]},{get_line,['$device',">"]},
              {get_password,[]},{get_password,['$device']},
              {getopts,[]},{getopts,['$device']},{setopts,[[binary]]},
              {setopts,['$device',[{encoding,unicode}]]},
              {fread,[">","~d"]},{fread,['$device',">","~ts"]},
              {columns,[]},{columns,['$device']},{rows,[]},{rows,['$device']},
              {request,[arbitrary]},{request,['$device',{get_line,latin1,">"}]},
              {requests,[[]]},{requests,['$device',[{write,42},nl,{get_chars,unicode,[],1}]]},
              {requests,['$device',[{requests,[nl,{requests,[{format,"~p",[1]}]}]},getopts]]},
              {request,['$device',{fwrite,"~p",[1]}]},
              {request,['$device',{put_chars,latin1,[233]}]}],
    Scan = [{F,A} || F <- [scan_erl_exprs,scan_erl_form,parse_erl_exprs,parse_erl_form],
                     A <- [[">"],['$device',">"],['$device',">",{2,3}],['$device',">",1,[text]]]],
    Reads = [{read,A} || A <- [[">"],['$device',">"],['$device',">",1],['$device',">",{2,3},[]]]],
    Replies = [ok,eof,{eof,7},{error,badarg},{error,estale,8},<<233>>,17,
               {ok,[{integer,1,42},{dot,1}],2},
               {ok,[{atom,1,abc},{'(',1},{')',1},{dot,1}],2}],
    [{F,A,R} || {F,A} <- Output++Inputs++Scan++Reads, R <- Replies] ++
    [{F,['$self'|tl(A)],ok} || {F,A} <- Output++Inputs, A =/= [], hd(A) =:= '$device'] ++
    [{F,[io_port_missing|tl(A)],ok} || {F,A} <- Output++Inputs, A =/= [], hd(A) =:= '$device'] ++
    [{F,['$device'|tl(A)],{error,R}} || {F,A} <- Output, A =/= [], hd(A) =:= '$device',
                                   R <- [terminated,calling_self,{no_translation,unicode,latin1},strange]] ++
    [{printable_range,[],ok}] ++
    [{get_chars,['$device',[],N],ok} || N <- [-1,1.0,a,[],1 bsl 100]] ++
    [{request,['$device',R],ok} || R <- [{put_chars,unknown,[1]},
        {put_chars,unicode,<<1:1>>}, {get_chars,latin1,[],a},
        {get_password,latin1},{requests,[a]}, {write,a,extra}, {format,a}]] ++
    [{requests,['$device',Rs],ok} || Rs <- [a,[a|b],[{requests,a}],
        [{requests,[]},a], [a,{requests,[]},b], [a,{requests,[b|c]}]]] ++
    [{read,['$device',[]],{ok,bad_tokens,2}},
     {parse_erl_exprs,['$device',[]],{ok,bad_tokens,2}},
     {parse_erl_form,['$device',[]],{ok,bad_tokens,2}}] ++
    [{scenario,N} || N <- [dead_device,dies_during_request,reply_then_die,
                           selective_receive,callback_output,callback_input,format_dependency]] ++
    [{F,[42|tl(A)],ok} || {F,A} <- Output++Inputs++Scan++Reads,
                          A =/= [], hd(A) =:= '$device'] ++
    [{get_chars,[[],-1],ok},{requests,['$device',[a,b,c|d]],ok}] ++
    [{upstream,N} || N <- [calling_self,otp_5403,io_lib_fread_literal]].
