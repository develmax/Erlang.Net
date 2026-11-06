-module(gen_server_port_second).
-export([init/1,handle_call/3,handle_cast/2,register_name/2,unregister_name/1,whereis_name/1,send/2]).
init(S)->{ok,S}.
handle_call(get,_,S)->{reply,S,S}.
handle_cast(_,S)->{noreply,S}.

register_name(N,P)->try register(N,P),yes catch error:badarg->no end.
unregister_name(N)->unregister(N).
whereis_name({throw,V})->throw(V);
whereis_name(N)->whereis(N).
send(N,M)->N!M.
