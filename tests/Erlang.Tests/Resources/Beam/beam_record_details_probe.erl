-module(beam_record_details_probe).
-export([get_detail_cols/1, get_details/2, init_page/2]).
-record(item, {name, value}).

get_detail_cols(_) -> {[{node, 1+2}, {port, 1+1}], true}.

get_details(Id, _) ->
    case detail_store:read(Id) of
        {ok, Info, Tail} ->
            Props = detail_store:to_proplist(record_info(fields, item), Info),
            Title = io_lib:format("~s (~s)", [Info#item.name, Id]),
            {ok, {Title, Props, Tail}};
        {error, not_found} ->
            Missing = "Missing",
            {info, Missing}
    end.

init_page(Parent, Info) ->
    Fields = fields(),
    detail_view:start_link(Parent, {Fields, Info, []}).

fields() -> [{name, value}].
