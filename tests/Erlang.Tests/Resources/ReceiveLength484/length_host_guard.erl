-module(length_host_guard).
-export([run/1,receive_length/1,map_key/2,bit_size/2]).
run(L) when length(L)>0->ok;run(_)->fallback.
receive_length(L)->
 self()!L,
 R=receive priority->{priority,receive _->retained after 0->lost end};X when length(X)>0->ok end,
 {R,drain()}.
drain()->receive X->[X|drain()] after 0->[] end.
map_key(L,M)->#{(length(L)):=V}=M,V.
bit_size(L,B)-><<_:(length(L)),_:3>>=B,ok.
