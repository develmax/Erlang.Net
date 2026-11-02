-module(init_ack582).
-export([load/1,ack/0]).
load(Path)->erlang:load_nif(Path,0).
ack()->erlang:nif_error(not_loaded).
