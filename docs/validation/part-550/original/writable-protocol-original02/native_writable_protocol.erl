-module(native_writable_protocol).
-export([run/0, append/2, grow/1, error_stack/1]).
run() ->
    A = append(<<>>,7),
    B = append(A,8),
    C = append(A,99),
    Initial = {A,B,C,binary:referenced_byte_size(B),binary:referenced_byte_size(C)},
    D = grow(B),
    Grown = binary:referenced_byte_size(D),
    true = erlang:garbage_collect(),
    Active = binary:referenced_byte_size(D),
    true = erlang:garbage_collect(),
    Inactive = binary:referenced_byte_size(D),
    Failed = error_stack(D),
    AfterFailure = binary:referenced_byte_size(D),
    self() ! {native_writable,D},
    Received = receive {native_writable,X} -> X end,
    Shared = binary:referenced_byte_size(D),
    Next = append(D,9),
    {Initial,Grown,Active,Inactive,Failed,AfterFailure,Received,Shared,
        Next,binary:referenced_byte_size(Next)}.
append(B,N) -> <<B/binary,N>>.
grow(B) -> <<B/binary,0:2048>>.
error_stack(B) ->
    try <<B/binary,broken:8>>
    catch Class:Reason:Stack -> {Class,Reason,Stack}
    end.
