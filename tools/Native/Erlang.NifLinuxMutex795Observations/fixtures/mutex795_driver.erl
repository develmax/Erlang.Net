-module(mutex795_driver).
-export([observe/2]).
observe(Path,probe) ->
    try mutex795:load(Path) of
        ok -> {loaded,mutex795:test()};
        Error -> {not_loaded,Error}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.
