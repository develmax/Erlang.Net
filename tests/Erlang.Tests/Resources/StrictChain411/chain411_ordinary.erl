-module(chain411_ordinary).
-export([ready/0, require_b/0]).
ready() -> ready.
require_b() -> chain411_b:run().
