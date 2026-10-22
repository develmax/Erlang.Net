-module(beam_project_driver).
-export([run/1]).
run(Values) ->
    {beam_fixture_constant:run(),
     beam_fixture_add:add(17,25),
     beam_project_host:sum(Values)}.
