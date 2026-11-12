-module(beam_expanded_reply_probe).
-export([get_details/2]).
-record(item, {id, name}).

get_details(Id, _) ->
    case detail_store:read(Id) of
        {ok, Info, Warning} ->
            Table = ets:new(expanded, [set, public]),
            Props0 = detail_store:to_proplist(record_info(fields, item), Info),
            Props = [{expand_table, Table} | Props0],
            Title = io_lib:format("~s (~s)", [Info#item.name, Id]),
            {ok, {Title, Props, Warning}};
        {error, {other_node, Node}} ->
            Message = "Open the remote node?",
            Open = fun() -> detail_view:open(Node, node) end,
            {yes_no, Message, Open};
        {error, not_found} ->
            Message = "Missing",
            {info, Message}
    end.
