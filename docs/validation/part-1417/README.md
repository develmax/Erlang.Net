# Part 1417 — Defer tuple fields to a later call

The saved-call planner keeps a checked tuple in one slot when at least two fields have a single later consumer with prepared operands. It reads those fields just before the call. It can reuse a released argument slot and trim the frame. The previous field-by-field schedule remains available for calls that need argument construction.

The deterministic fixture matches OTP byte for byte. It covers relocation and trimming, tagged and known shapes, mixed immediate and deferred fields, and the original badmatch value. The previous compiler differs on all four functions exercising the new behavior. Three focused tests pass with the final corpus compiler assembly.

All 1,366 Windows OTP 29.1.1 release images were generated, with zero source rejections. There are 380 Equal and 986 Different. ELDAPv3 is Equal from C#-generated Erlang, and OTP prepares all 986 differing files for loading. Four files changed. Three function frames reach the OTP size, and a fourth moves closer. ttb:format/2 gains normalized instruction equality. The combined Code size gap falls by five bytes. edoc_macros grows by five bytes despite the closer frame. An intermediate regression in ttb:tracer/2 was corrected, and the final instructions for that function equal the previous build.

The byte-level section audit finds a Code difference in all 986 remaining files. xmerl_xpath_lib is the closest by number of differing sections and has one normalized function difference. The next priority is the general case lowering that produces dynamic tail calls in that function. The ASN.1 append schedule remains another input for the call planner. Full runtime verification, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [status changes](status-changes.txt), [section audit](section-audit.txt) and [audit script](section-audit.escript)
- [Frames](frame-delta.txt), [instructions](normalized-functions.txt), [loader and Code size](loader-and-code.txt) and [tracer regression check](tracer-final-check.txt)
- [Oracle](check-deferred-tuple.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
- [Remaining schedules](remaining-schedules.md)
