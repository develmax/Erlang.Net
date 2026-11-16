-module(list_element_case_probe).
-export([lookup/2, read/2]).

lookup([], _) ->
    {missing, key};
lookup([Item|Rest], Key) ->
    case element(2, Item) of
        Key ->
            case element(1, Item) of
                payload -> {element(1, Item), Key, element(3, Item)};
                _ -> {element(1, Item), Key}
            end;
        _ -> lookup(Rest, Key)
    end.

read([], _) ->
    missing;
read([Item|Rest], Key) ->
    case element(2, Item) of
        Key -> element(2, Item);
        _ -> read(Rest, Key)
    end.
