-module(snprintf791_driver).
-export([observe/2]).
observe(Path,probe) ->
    try snprintf791_oracle:load(Path) of
        ok -> {loaded,snprintf791_oracle:run()};
        Error -> {not_loaded,Error}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.
