-module(atom_replacement_probe).
-export([alias/1]).
alias(zulu) -> zed;
alias(alpha) -> first;
alias(Value) -> Value.
