-module(raw_onload_visibility_fixture).
-export([body/0, capture/0]).

body() -> old_body.
capture() -> fun body/0.
