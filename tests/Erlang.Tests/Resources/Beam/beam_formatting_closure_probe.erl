-module(beam_formatting_closure_probe).
-export([formatter/3, decoder/2, hex/2]).

formatter(Format, Bin, Colors) ->
    fun() ->
        try io_lib:format(Format, [Bin]) of
            Text -> show(lists:flatten(Text), Colors)
        catch
            error:badarg ->
                Warning = "Cannot format with " ++ Format,
                detail_view:warning(Warning, Colors)
        end
    end.

decoder(Bin, Colors) ->
    fun() ->
        try binary_to_term(Bin) of
            Term -> show(io_lib:format("~tp", [Term]), Colors)
        catch
            error:badarg ->
                Warning = "Cannot decode",
                detail_view:warning(Warning, Colors)
        end
    end.

hex(Bin, Colors) ->
    fun() ->
        Text = "<<" ++ digits(Bin, 25) ++ ">>",
        show(io_lib:format("~s", [Text]), Colors)
    end.

digits(Bin, Limit) -> detail_view:digits(Bin, Limit).

show(Text, Colors) -> detail_view:show(Text, Colors).
