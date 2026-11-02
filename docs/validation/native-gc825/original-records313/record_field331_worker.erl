-module(record_field331_worker).
-export([worker/1]).
worker(Parent) ->
    Parent ! {finished331,record_field331_observer:run()},
    ok.
