-module(beam_case_register_selector_probe).
-export([run/3, tail/3, continue_after/3, arity_fallback/3, projection/3, guard_route/2, guard_entry/2, guard_failure/2, nested_tagged/2, fresh_nested/2, argument_case/3, guarded_case/2, guarded_reject/1, update_one/2, update_many/3, update_known/1, read_checked/1, record_case/1, read_shadow/2, record_select/1, record_args/1, record_saved/1, record_after_branch/2, plain_case/2, drop_fields/1, retain_fields/2, retain_export/1, final_field/2, checked_fields/2, map_after_call/2, map_caught/2, tuple_fields/1, tuple_field_bound/1, map_linear/1, map_nested/1, map_checked_parent/2, map_duplicate/1, filter_known/1, filter_negated/1, filter_general/1, filter_general_not/1, filter_mapped/1, dispatch_binary/1, dispatch_computed/1, dispatch_throw/1]).
-record(parity_record, {first,second}).
-export([grouped_arity/2, grouped_arity_tail/2, grouped_arity_class/2, grouped_arity_default/2]).
-export([dispatch_entry_tail/3]).
-export([literal_case_guard/1, literal_case_partial/1, literal_function_guard/1, literal_function_error/1]).
-export([atom_guard_table/1, atom_guard_if/1, literal_case_outer/1]).
-export([record_dispatch/2, record_dispatch_tail/2]).

run(Input, Expected, State) ->
    Prepared = prepare(State),
    case select(Input) of
        {entry, Expected, Value} -> {match, Prepared, Value};
        Error = {error, _} -> Error;
        {entry, fallback, Value} -> {fallback, Prepared, Value}
    end.

prepare(State) -> {state, State}.
select(Input) -> Input.

tail(Input, Expected, State) ->
    Prepared = prepare(State),
    case select(Input) of
        {entry, Expected, Value} -> pass(Value, Prepared, Expected);
        _ -> no
    end.

pass(Value, {state, State}, Key) -> {Value,State,Key}.

continue_after(Input, Expected, State) ->
    Prepared = prepare(State),
    Value = case select(Input) of
        {entry, Expected, Field} -> pass(Field, Prepared, Expected);
        _ -> no
    end,
    {continued,Value}.

arity_fallback(Input, Expected, State) ->
    case select(Input) of
        {entry, Expected} -> {match,State};
        {entry, Expected, Value} -> {match,State,Value};
        {other_tag, _} -> tagged;
        {entry, fallback} -> fallback;
        off -> off;
        Original -> {other,Original}
    end.

projection(Input, Expected, State) ->
    case select(Input) of
        Whole = {ignored, Expected, _Unused} -> {retained,Whole};
        {copy, Expected, Value, _Unused} -> {value,State,Value};
        {call, Expected, _Unused, _Extra, Value} -> project_call(Value, State, Expected);
        {repeat, Same, Same} -> {matched,State};
        Original -> {other,Original}
    end.

project_call(Value, State, Expected) -> {Value,State,Expected}.

guard_route(Input, Other)
  when (is_list(Input) andalso is_tuple(Other)) orelse is_binary(Input) ->
    accepted;
guard_route(_, _) ->
    rejected.

guard_entry(Input, Other) when is_list(Input) orelse is_binary(Other) ->
    {Input,Other}.

guard_failure(Input, Other) when element(1, Input) orelse is_binary(Other) ->
    accepted;
guard_failure(_, _) ->
    rejected.

nested_tagged(Input, Expected) ->
    case Input of
        {outer, {inner, Expected, Value}} -> {ok,Value};
        Original -> {other,Original}
    end.

fresh_nested(Input, Expected) ->
    case select(Input) of
        {outer, {inner, {leaf, Expected}, Same}, Same} -> {ok,Same};
        Original -> {other,Original}
    end.

argument_case(First, Input, Last) ->
    put(beam_argument_order, []),
    Packed = pack(record_argument(First),
                  case record_argument(Input) of
                      {tagged, _, Value} -> {ok,Value};
                      Original -> {other,Original}
                  end,
                  record_argument(Last)),
    {Packed,get(beam_argument_order)}.

record_argument(Value) ->
    put(beam_argument_order, [Value|get(beam_argument_order)]),
    Value.

pack(First, Middle, Last) -> {First,Middle,Last}.

guarded_case(Input, Limit) ->
    case select(Input) of
        {guarded, Value} when is_integer(Value), Value > Limit -> {large,Value};
        {guarded, Value} when is_atom(Value) orelse is_binary(Value) -> {named,Value};
        Items when is_list(Items) -> {list,Items};
        Original -> {other,Original}
    end.

guarded_reject(Input) ->
    case select(Input) of
        {guarded, Value} when is_integer(Value) -> Value
    end.

update_one(Original, Value) ->
    {Original,Original#parity_record{first=Value}}.

update_many(Original, First, Second) ->
    {Original,Original#parity_record{first=First,second=Second}}.

update_known(Value) ->
    (#parity_record{first=Value,second=same})#parity_record{first=Value,second=same}.

read_checked(Original={parity_record,_,_}) ->
    {Original#parity_record.first,Original#parity_record{second=updated}}.

record_case(Input) ->
    case select(Input) of
        Record={parity_record,_,_} -> Record#parity_record.first;
        Record -> Record#parity_record.first
    end.

read_shadow(_Record={parity_record,_,_}, Items) ->
    [_Record#parity_record.first || _Record <- Items].

record_select(Record) ->
    case Record#parity_record.first of
        old -> Record#parity_record.second;
        Other -> {Other,Record#parity_record.second}
    end.

record_args(Record) ->
    pack(Record#parity_record.first, Record#parity_record.second, Record#parity_record.first).

record_saved(Record) ->
    case Record of
        {parity_record,_,_} -> Record#parity_record.first;
        Other -> {other,Other}
    end.

record_after_branch(Record, Flag) ->
    case Flag of
        true -> Record#parity_record.first;
        false -> skip
    end,
    Record#parity_record.second.

plain_case(Input, Expected) ->
    case select(Input) of
        {Expected, {First,Second}} -> {nested,First,Second};
        {tagged, Value} -> {tagged,Value};
        {First,Second,Third} -> {plain,First,Second,Third};
        Original -> {other,Original}
    end.

drop_fields(Input) ->
    case select(Input) of
        {_Unused1,_Unused2,_Unused3} -> triple;
        Original -> {other,Original}
    end.

retain_fields(Input, Key) ->
    case select(Input) of
        {same, Repeated, Repeated, _Unused} -> equal;
        {bound, Key, _Unused} -> bound;
        {guard, Value, _Unused} when is_integer(Value) -> integer_value;
        {closure, Captured, _Unused} -> fun() -> Captured end;
        Original -> {other,Original}
    end.

retain_export(Input) ->
    case select(Input) of
        {picked, Value, _Unused} -> ok;
        {another, Value, _Unused} -> ok
    end,
    {exported,Value}.

final_field(Other, Record) ->
    pack(Other, Record#parity_record.second, marker).

checked_fields(Left, Right) ->
    pack(Left#parity_record.first, marker, Right#parity_record.second).

map_after_call(Input, Tag) ->
    Items = prepare_list(Input),
    [map_entry(Item, Tag) || Item <- Items].

map_caught(Input, Tag) ->
    Items = prepare_list(Input),
    [try map_entry(Item, Tag)
     catch error:invalid_item -> {recovered,Tag}
     end || Item <- Items].

prepare_list(Input) -> Input.

map_entry(invalid, _) -> erlang:error(invalid_item);
map_entry(Item, Tag) -> {Tag,Item}.

tuple_fields(Items) ->
    [Value || {field,Value} <- Items].

tuple_field_bound(Items) ->
    Values = [Value || {_,Value} <- Items],
    {mapped,Values}.

map_linear(Items) ->
    [begin
         Pair = mapper_pair(Item, suffix),
         mapper_finish(Pair)
     end || Item <- Items].

map_nested(Items) ->
    [mapper_finish(mapper_pair(Item, suffix)) || Item <- Items].

mapper_pair(invalid, _) -> erlang:error(invalid_item);
mapper_pair(Item, Suffix) -> {Item,Suffix}.

mapper_finish(Value) -> {mapped,Value}.

map_checked_parent(Items, Fixed) ->
    [begin
         Fixed = mapper_pair(Item, suffix),
         mapper_finish(Fixed)
     end || Item <- Items].

map_duplicate(Items) ->
    [mapper_three(prefix, Item, Item) || Item <- Items].

mapper_three(Prefix, Left, Right) -> {Prefix,Left,Right}.

filter_known(Items) -> [Item || Item <- Items, filter_predicate(Item)].
filter_negated(Items) -> [Item || Item <- Items, not filter_predicate(Item)].
filter_general(Items) -> [Item || Item <- Items, filter_value(Item)].
filter_general_not(Items) -> [Item || Item <- Items, not filter_value(Item)].
filter_mapped(Items) -> [mapper_finish(Item) || Item <- Items, filter_predicate(Item)].

filter_predicate(key) -> true;
filter_predicate(_) -> false.

filter_value(invalid) -> erlang:error(invalid_item);
filter_value(Item) -> Item.

dispatch_binary(alpha) -> <<"one">>;
dispatch_binary(gamma) -> <<"two">>;
dispatch_binary(beta) -> <<"one">>;
dispatch_binary(Input) -> <<"prefix:", (iolist_to_binary(Input))/binary>>.

dispatch_computed(alpha) -> one;
dispatch_computed(beta) -> two;
dispatch_computed(Input) -> mapper_finish(Input).

dispatch_throw(alpha) -> one;
dispatch_throw(beta) -> two;
dispatch_throw(Input) -> throw({error,{bad,Input}}).

grouped_arity(alpha, 1) -> true;
grouped_arity(beta, 1) -> true;
grouped_arity(beta, 2) -> true;
grouped_arity(gamma, 1) -> true;
grouped_arity(delta, 3) -> true;
grouped_arity(Name, Arity) when is_atom(Name), is_integer(Arity) -> false.

grouped_arity_tail(alpha, 1) -> true;
grouped_arity_tail(alpha, 2) -> true;
grouped_arity_tail(beta, 3) -> true;
grouped_arity_tail(Name, Arity) -> grouped_arity(Name, Arity).

grouped_arity_class(alpha, 2) -> first;
grouped_arity_class(beta, 1) -> second;
grouped_arity_class(gamma, 1) -> first;
grouped_arity_class(gamma, 2) -> first;
grouped_arity_class(delta, 2) -> second;
grouped_arity_class(epsilon, 1) -> first.

grouped_arity_default(alpha, 1) -> true;
grouped_arity_default(beta, 2) -> true;
grouped_arity_default(gamma, 1) -> true;
grouped_arity_default(gamma, 2) -> true;
grouped_arity_default(_, _) -> false.

dispatch_entry_tail(Head, gamma, Value) -> entry_tail_result(Head, 2, Value);
dispatch_entry_tail(Head, beta, Value) -> entry_tail_result(Head, 1, Value);
dispatch_entry_tail(Head, alpha, Value) -> entry_tail_result(Head, 1, Value);
dispatch_entry_tail(_, Key, Value) -> entry_tail_fallback(Key, Value).

entry_tail_result(Head, Arity, Value) -> {Head,Arity,Value}.
entry_tail_fallback(Key, Value) -> {missing,Key,Value}.

literal_case_guard(Key) ->
    case Key of
        gamma -> 2;
        alpha -> 1;
        beta -> 1;
        Value when is_integer(Value) -> Value
    end.

literal_case_partial(Key) ->
    case Key of
        alpha -> {one,1};
        beta -> {two,2}
    end.

literal_function_guard(alpha) -> true;
literal_function_guard(beta) -> true;
literal_function_guard(Key) when is_atom(Key) -> false.

literal_function_error(alpha) -> {one,1};
literal_function_error(beta) -> {two,2};
literal_function_error(_) -> erlang:error(badarg).

atom_guard_table(default) -> 0;
atom_guard_table(alpha) -> 1;
atom_guard_table(Key) when Key == default; Key =:= beta; gamma == Key -> 2;
atom_guard_table(Key) when Key == alpha orelse Key =:= delta -> 3;
atom_guard_table(_) -> 0.

atom_guard_if(Key) ->
    if
        Key =:= alpha; Key == beta -> first;
        Key =:= gamma orelse Key == delta -> second;
        true -> other
    end.

literal_case_outer(Key) ->
    case Key of
        alpha -> 1;
        beta -> 2;
        _ when is_integer(Key) -> Key
    end.

record_dispatch(beta, #parity_record{second=Second}) -> Second;
record_dispatch(alpha, #parity_record{first=First}) -> First;
record_dispatch(gamma, #parity_record{first=First}) -> First.

record_dispatch_tail(Key = alpha, Record) ->
    case Record#parity_record.first of
        "-1" -> undefined;
        _ -> record_dispatch_tail(atom_to_list(Key),
                                  Record#parity_record.first)
    end;
record_dispatch_tail(Key = beta, Record) ->
    record_dispatch_tail(atom_to_list(Key),
                         Record#parity_record.second);
record_dispatch_tail(_, undefined) -> undefined;
record_dispatch_tail(Key, Value) -> {Key,Value}.
