%% Scripted group leader for ETS terminal functions; records the real I/O protocol.
-module(ets_port_device).
-export([start/1,snapshot/1,loop/2]).
start(Inputs) -> spawn(?MODULE,loop,[Inputs,[]]).
snapshot(Device) -> Device!{snapshot,self()},receive {snapshot,Output}->Output end.
loop(Inputs,Output) ->
    receive
        {io_request,From,Tag,Request} ->
            {Reply,Next,Text}=request(Request,Inputs),
            From!{io_reply,Tag,Reply},
            loop(Next,[Output,Text]);
        {snapshot,From} ->
            From!{snapshot,unicode:characters_to_list(Output)},loop(Inputs,Output);
        stop -> ok
    end.
request({put_chars,_,Chars},Inputs) -> {ok,Inputs,Chars};
request({put_chars,_,M,F,A},Inputs) -> {ok,Inputs,apply(M,F,A)};
request({get_line,_,Prompt},[H|T]) -> {H,T,format_prompt(Prompt)};
request({get_line,_,Prompt},[]) -> {eof,[],format_prompt(Prompt)};
request({getopts},Inputs) -> {[{encoding,unicode}],Inputs,[]};
request(getopts,Inputs) -> {[{encoding,unicode}],Inputs,[]};
request({setopts,_},Inputs) -> {ok,Inputs,[]}.
format_prompt(P) when is_atom(P) -> atom_to_list(P);
format_prompt(P) -> P.
