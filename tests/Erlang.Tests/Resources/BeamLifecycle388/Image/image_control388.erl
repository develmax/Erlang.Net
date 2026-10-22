-module(image_control388).
-export([load/1, soft_purge/0]).

load(Bytes) -> erlang:load_module(beam_image388, Bytes).
soft_purge() -> code:soft_purge(beam_image388).
