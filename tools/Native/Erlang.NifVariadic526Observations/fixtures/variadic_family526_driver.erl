-module(variadic_family526_driver).
-export([observe/2]).
observe(Path,probe) ->
    try variadic_family526:load(Path) of
        ok -> {loaded,variadic_family526:run()};
        Error -> {not_loaded,Error}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.
