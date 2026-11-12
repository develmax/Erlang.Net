-module(proper_list_return_probe).
-export([start/2, start_tail/2, public/2, callback/0, finish/1]).
start(List, Result) -> private_return(lists:reverse(List),Result).
start_tail(List, Result) -> private_tail(lists:reverse(List),Result).
private_return([_|T], Result) -> private_return(T,Result);
private_return([], Result) -> Result.
private_tail([_|T], Result) -> private_tail(T,Result);
private_tail([], Result) -> finish(Result).
public([_|T], Result) -> public(T,Result);
public([], Result) -> Result.
callback() -> fun indirect/2.
indirect([_|T], Result) -> indirect(T,Result);
indirect([], Result) -> finish(Result).
finish(Value) -> {finished,Value}.
