-module(repeated_argument_probe).
-export([classify/2]).
classify(X, X) -> equal;
classify(_, _) -> different.
