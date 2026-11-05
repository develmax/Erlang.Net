-module(unicode_growth_cases).
-export([write/1, run/2]).

write(Directory) ->
    Cases = [{N,E} || N <- [2000,10000,20000,100000],
                      E <- [utf16,{utf16,little},utf32,latin1]],
    Rows = [{N,E,run(N,E)} || {N,E} <- Cases],
    ok = file:write_file(filename:join(Directory,"growth.etf"),term_to_binary(Rows)).

run(N, Encoding) ->
    Input = lists:duplicate(N,65),
    Binary = unicode:characters_to_binary(Input,unicode,Encoding),
    Size = byte_size(Binary),
    Before = binary:referenced_byte_size(Binary),
    Extended = <<Binary/binary,1>>,
    {Size,Before,binary:referenced_byte_size(Extended),
     binary:part(Binary,0,4),binary:part(Binary,Size-4,4),binary:last(Extended)}.
