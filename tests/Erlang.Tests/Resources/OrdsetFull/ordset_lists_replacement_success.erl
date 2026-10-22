-module(lists).
-export([foldl/3,filter/2,map/2,filtermap/2,usort/1]).
foldl(F,A,S) -> {fold_replaced,F,A,S}.
filter(F,S) -> {filter_replaced,F,S}.
map(F,S) -> {map_replaced,F,S}.
filtermap(F,S) -> {filtermap_replaced,F,S}.
usort(S) -> {sort_replaced,S}.
