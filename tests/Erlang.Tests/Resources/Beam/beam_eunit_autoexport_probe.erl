-module(beam_eunit_autoexport_probe).
-compile({parse_transform,eunit_autoexport}).
-moduledoc false.

alpha_test() -> ok.
beta_test_() -> [].
gamma_exported_() -> ok.
eunit_wrapper_(Value) -> Value.
