-module(contract_logger509).
-export([adding_handler/1, changing_config/3, removing_handler/1, log/2]).
adding_handler(Config) -> {ok, Config}.
changing_config(_, _, Config) -> {ok, Config}.
removing_handler(_) -> ok.
log(Event, #{config := #{owner := Owner, token := Token}}) ->
    Owner ! {log509, Token, self(), Event},
    ok.
