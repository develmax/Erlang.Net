-module(observe_mod_actions).
-export([run/2]).
-include("httpd.hrl").
run(Image, Output) ->
    {ok, Beam} = file:read_file(Image),
    {module, mod_actions} = code:load_binary(mod_actions, "mod_actions witness", Beam),
    Rows=[#mod{data=[{status,{404,[],not_found}}]},
          #mod{data=[{status,{200,[],ok}},{response,done}]},
          #mod{data=[{response,done}]}, #mod{data=[{response,[]}]},
          #mod{data=[{status,wrong}]}, wrong],
    Outcomes=[try mod_actions:do(Info) of Value->{ok,Value}
              catch Class:Reason:Stack ->
                  {Class,Reason,[{F,A,proplists:get_value(line,Site)}||{mod_actions,F,A,Site}<-Stack]}
              end || Info<-Rows],
    ok=file:write_file(Output,term_to_binary(Outcomes)).

