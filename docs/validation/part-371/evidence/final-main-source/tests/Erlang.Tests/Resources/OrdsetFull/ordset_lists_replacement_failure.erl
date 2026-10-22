-module(lists).
-export([foldl/3,filter/2,map/2,filtermap/2]).
foldl(_,_,_) -> erlang:error({replacement_failure,fold}).
filter(_,_) -> erlang:error({replacement_failure,filter}).
map(_,_) -> erlang:error({replacement_failure,map}).
filtermap(_,_) -> erlang:error({replacement_failure,filtermap}).
