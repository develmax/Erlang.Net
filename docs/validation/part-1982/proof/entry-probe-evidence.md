# Retired entry-call fixture retake

Part1982 focused lane 1 failed compiler/beam/reuses-retired-entry-slot-before-following-call at line 76. The exact frozen Erlang.Compiler.dll has SHA-256 D9C5C9F9505D4E24C3F711F559587ED00BE8610E13B79CD86B6B57FEEF673422. reflect.ps1 reads the fixture Source field from the exact built Erlang.Tests.dll and emits the diagnostic image in memory. actual.terms contains its disassembly.

returning/3 and checked/3 both enter the new production route. Both allocate two slots and trim to one. The returning branch exchanges the result and restore argument. The checked branch retains exact true matching, failure at the original line and the raw badmatch payload. The original probing/1 fixture selected a nil/plain fallback entry route and emitted two slots. The frozen reader and direct newly built region emitter both accept its ordinary statements in isolation. This fixture did not enter the guarded ordinary branch route that the proposal wires.

The fixture patch changes probing/1 to a tuple-adapter branch and a direct map-guard branch. It keeps the original body, values and frame/opcode assertions. Compiled with D9C5, guarded.terms shows allocate 1, nofail node/0 produces X2, Y0 is read into X1 before X0 replaces it, and the original probe result returns via Y0. The diagnostic image SHA-256 is 5C841A64EA60703F62E4ECE76F08B6A15F07B8C0308A29B6EFD5678B5CCB519F.

Windows OTP-29.1.1 loaded the guarded diagnostic image. All fixture value observations passed, including absence of restoration on the false check, error {badmatch,false} at source line 16, ordinary result retention, exact checked restore, and the probe. This native diagnostic did not invoke the test harness. Private Git index apply, application, reverse, staged whitespace and replay bytes passed. No SDK build, shared source writes, suite reruns or remote writes occurred.

fixture.patch SHA-256 3C945E22D9CC826B153BEEBCF30568333A5D5FED4414AB1142BE5B661E461A4A
