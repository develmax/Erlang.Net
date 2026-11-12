-module(schema_probe).
-export([schema/1]).
schema(integer) -> {integer, 2};
schema(string) -> {octets, 4};
schema(alias) -> {octets, 4};
schema(control) -> {sequence, 16, [{criticality, false}]};
schema(Type) -> erlang:error({undefined_type, Type}).
