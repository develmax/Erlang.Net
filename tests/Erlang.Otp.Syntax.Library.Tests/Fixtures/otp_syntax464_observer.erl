-module(otp_syntax464_observer).
-file("otp_syntax464_observer.erl", 1).
-export([worker/1, run/0]).

worker(Parent) ->
    Parent ! {finished, run()}.

run() ->
    [observe(atom, fun() -> erl_syntax:concrete(erl_syntax:atom(hello)) end),
     observe(integer, fun() -> erl_syntax:concrete(erl_syntax:integer(-123)) end),
     observe(tuple, fun() -> erl_syntax:concrete(erl_syntax:tuple([erl_syntax:atom(ok), erl_syntax:integer(7)])) end),
     observe(list, fun() -> erl_syntax:concrete(erl_syntax:list([erl_syntax:integer(1), erl_syntax:integer(2)])) end),
     observe(string, fun() -> erl_syntax:concrete(erl_syntax:string("abc")) end),
     observe(variable, fun() -> erl_syntax:variable_name(erl_syntax:variable('X')) end),
     observe(revert, fun() -> erl_syntax:revert(erl_syntax:tuple([erl_syntax:atom(ok), erl_syntax:integer(42)])) end),
     observe(position, fun() -> erl_syntax:get_pos(erl_syntax:set_pos(erl_syntax:atom(a), 17)) end),
     observe(subtrees, fun() -> erl_syntax:subtrees(erl_syntax:tuple([erl_syntax:atom(a)])) end),
     observe(type_bad, fun() -> erl_syntax:type(invalid) end),
     observe(concrete_variable, fun() -> erl_syntax:concrete(erl_syntax:variable('X')) end),
     observe(variable_bad, fun() -> erl_syntax:variable_name(erl_syntax:integer(1)) end),
     observe(pretty_text, fun() -> prettypr:format(prettypr:text("hello")) end),
     observe(pretty_above, fun() -> prettypr:format(prettypr:above(prettypr:text("a"), prettypr:text("b"))) end),
     observe(pretty_empty, fun() -> prettypr:format(prettypr:empty()) end),
     observe(pretty_bad, fun() -> prettypr:format(invalid) end),
     observe(grammar_declaration, fun() -> yeccparser:parse([{var,1,'Nonterminals'},{atom,1,expr},{dot,1}]) end),
     observe(grammar_rule, fun() -> yeccparser:parse([{atom,2,expr},{'->',2},{atom,2,number},{':',2},{var,2,'$1'},{dot,2}]) end),
     observe(grammar_bad_list, fun() -> yeccparser:parse(invalid) end)].

observe(Name, Function) ->
    try Function() of
        Value -> {Name, {returned, Value}}
    catch
        Class:Reason:Stack -> {Name, {caught, Class, Reason, Stack}}
    end.
