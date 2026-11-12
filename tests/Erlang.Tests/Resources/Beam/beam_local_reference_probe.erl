-module(beam_local_reference_probe).
-export([direct/0, unused/1, nested/1]).
direct() -> fun target/1.
unused(_) -> fun target/1.
nested(X) -> {X, fun target/1}.
target(X) -> X.
