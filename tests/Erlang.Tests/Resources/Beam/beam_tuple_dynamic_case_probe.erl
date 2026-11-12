-module(beam_tuple_dynamic_case_probe).
-export([run/2]).
-record(value, {kind, term}).

run({number, N}, _Context) ->
    #value{kind=number, term=N};
run({literal, Text}, _Context) ->
    #value{kind=string, term=Text};
run({call, Name, Arg}, Context) ->
    case persistent_term:get(Name) of
        {true, Alias} -> erlang:Alias(Context, Arg);
        true -> erlang:Name(Context, Arg);
        false -> exit({unknown_name, Name})
    end;
run(Other, _Context) ->
    exit({unknown_expression, {unsupported, Other}}).
