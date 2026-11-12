-module(beam_guarded_reply_probe).
-export([callbacks/2]).
callbacks(Socket, Controller) ->
    {fun(Ctrl, Length, Timeout) when Ctrl =:= Controller ->
         case beam_reply_transport:recv(Socket, Length, Timeout) of
             {ok, Bin} when is_binary(Bin) ->
                 {ok, binary_to_list(Bin)};
             Other ->
                 Other
         end
     end,
     fun(Ctrl) when Ctrl =:= Controller ->
         beam_reply_transport:setopts(Socket, [beam_reply_transport:option()])
     end}.
