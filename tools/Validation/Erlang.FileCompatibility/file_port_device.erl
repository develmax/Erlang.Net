%% Validation-only descriptor, never registered as a production file backend.
-module(file_port_device).
-export([close/1,read/2,read_line/1,write/2,pread/2,pread/3,pwrite/2,pwrite/3,
         position/2,truncate/1,sync/1,datasync/1,allocate/3,advise/4,
         read_handle_info/2,copy/3,ipread_s32bu_p32bu/3,sendfile/8]).

close(_) -> ok.
read(Fd, N) when is_integer(N), N >= 0 ->
    Pos = pos(Fd),
    Result = pread(Fd, Pos, N),
    case Result of {ok,Bin} -> put({Fd,pos},Pos+byte_size(Bin)); _ -> ok end,
    Result;
read(_,_) -> {error,badarg}.
read_line(Fd) -> read(Fd,3).
write({file_descriptor,_,{fail,Reason}},_) -> {error,Reason};
write(Fd,Data) ->
    try iolist_to_binary(Data) of
        Bin -> Old = case get({Fd,written}) of undefined -> <<>>; X -> X end,
               put({Fd,written},<<Old/binary,Bin/binary>>),ok
    catch error:Reason -> {error,Reason} end.
pread(_,[]) -> {ok,[]};
pread(Fd,[{At,N}|Rest]) ->
    case pread(Fd,At,N) of
        {ok,Data} -> {ok,Tail} = pread(Fd,Rest), {ok,[Data|Tail]};
        eof -> {ok,Tail} = pread(Fd,Rest), {ok,[eof|Tail]};
        Error -> Error
    end;
pread(_,_) -> {error,badarg}.
pread(Fd,At,N) when is_integer(At),At>=0,is_integer(N),N>=0 ->
    Bin = data(Fd),
    if At >= byte_size(Bin), N /= 0 -> eof;
       true -> {ok,binary:part(Bin,erlang:min(At,byte_size(Bin)),erlang:min(N,erlang:max(0,byte_size(Bin)-At)))}
    end;
pread(_,_,_) -> {error,einval}.
pwrite(_,[]) -> ok;
pwrite(Fd,[{At,Data}|Rest]) -> case pwrite(Fd,At,Data) of ok -> pwrite(Fd,Rest); Error -> Error end;
pwrite(_,_) -> {error,badarg}.
pwrite(Fd,At,Data) when is_integer(At),At>=0 -> write(Fd,Data);
pwrite(_,_,_) -> {error,einval}.
position(Fd,N) when is_integer(N),N>=0 -> put({Fd,pos},N),{ok,N};
position(Fd,bof) -> position(Fd,0);
position(Fd,cur) -> {ok,pos(Fd)};
position(Fd,eof) -> position(Fd,byte_size(data(Fd)));
position(Fd,{bof,N}) -> position(Fd,N);
position(Fd,{cur,N}) when is_integer(N) -> position(Fd,pos(Fd)+N);
position(Fd,{eof,N}) when is_integer(N) -> position(Fd,byte_size(data(Fd))+N);
position(_,_) -> {error,einval}.
truncate(_) -> ok.
sync(_) -> ok.
datasync(_) -> ok.
allocate(_,At,N) when is_integer(At),At>=0,is_integer(N),N>=0 -> ok;
allocate(_,_,_) -> {error,einval}.
advise(_,At,N,Advice) -> {advice,At,N,Advice}.
read_handle_info(_,Options) -> {handle_info,Options}.
copy(_,_,N) -> {driver_copy,N}.
ipread_s32bu_p32bu(_,Pos,Max) -> {driver_ipread,Pos,Max}.
sendfile(_,_,_,_,_,_,_,_) -> {error,enotsup}.
pos(Fd) -> case get({Fd,pos}) of undefined -> 0; N -> N end.
data({file_descriptor,_,indirect}) -> <<3:32,8:32,"xyz">>;
data({file_descriptor,_,zero}) -> <<0:32,77:32>>;
data({file_descriptor,_,short}) -> <<1,2,3>>;
data(_) -> <<"abcdef">>.
