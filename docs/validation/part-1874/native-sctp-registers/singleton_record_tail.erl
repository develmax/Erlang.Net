-module(singleton_record_tail).
-export([four/4,three/3,two/3,one/3,permuted/3,alias_fallback/3]).
four(A,B,C,D) ->
    case fixture_record_dep:select(A,{C}) of
        {selected,[{item,{data,V,W,X,Y}}]} -> fixture_record_dep:send(A,{data,V,W,X,Y,B,C},D);
        _ -> fixture_record_dep:send(A,{data,0,0,0,0,B,C},D)
    end.
three(A,B,C) ->
    case fixture_record_dep:select(A,3) of
        {selected,[{item,{data,V,W,X}}]} -> fixture_record_dep:send(A,{data,V,W,X,B},C);
        _ -> fixture_record_dep:send(A,{data,0,0,0,B},C)
    end.
two(A,B,C) ->
    case fixture_record_dep:select(A,2) of
        {selected,[{item,{data,V,W}}]} -> fixture_record_dep:send(A,{data,V,W,B},C);
        _ -> fixture_record_dep:send(A,{data,0,0,B},C)
    end.
one(A,B,C) ->
    case fixture_record_dep:select(A,1) of
        {selected,[{item,{data,V}}]} -> fixture_record_dep:send(A,{data,V,B},C);
        _ -> fixture_record_dep:send(A,{data,0,B},C)
    end.
permuted(A,B,C) ->
    case fixture_record_dep:select(A,2) of
        {selected,[{item,{data,V,W}}]} -> fixture_record_dep:send(A,{data,W,V,B},C);
        _ -> fixture_record_dep:send(A,{data,0,0,B},C)
    end.
alias_fallback(A,B,C) ->
    case fixture_record_dep:select(A,2) of
        {selected,[{item,{data,V,W}}]} -> fixture_record_dep:send(A,{data,V,W,B},C);
        Other -> fixture_record_dep:send(A,{fallback,Other,B},C)
    end.
