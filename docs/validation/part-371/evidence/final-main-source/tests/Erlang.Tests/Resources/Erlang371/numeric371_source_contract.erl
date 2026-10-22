-module(numeric371_source_contract).
-export([run/0]).
run() ->
    "1.250000e+00" = float_to_list(1.25,[{scientific,-1}]),
    <<"1.250000e+00">> = float_to_binary(1.25,[{scientific,-2147483648}]),
    "1.250000e+00" = float_to_list(1.25,[{scientific,4294967295}]),
    "1.25e+00" = float_to_list(1.25,[{scientific,4294967298}]),
    "1.25" = float_to_list(1.25,[{decimals,4294967298}]),
    {error,badarg} = reason(fun()->float_to_list(1.25,[{decimals,-1}]) end),
    [probe(list_to_integer,[[]]),
     probe(list_to_integer,["!",10]),
     probe(list_to_integer,["42",1]),
     probe(list_to_integer,[[49|tail]]),
     probe(binary_to_integer,[<<>>]),
     probe(binary_to_integer,[<<"!">>,16]),
     probe(binary_to_integer,[<<"42">>,37]),
     probe(binary_to_integer,[<<1:1>>])].

probe(Function,Arguments) ->
    try erlang:apply(erlang,Function,Arguments) of Value->{unexpected,Value}
    catch Class:Reason:Stack->{Function,Arguments,Class,Reason,Stack} end.

reason(F) -> try F() of V->{unexpected,V} catch C:R->{C,R} end.
