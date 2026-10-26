-module(oracle_proc_format).
-export([run/0]).
run()->{raw,proc_lib:format([[],other])}.
