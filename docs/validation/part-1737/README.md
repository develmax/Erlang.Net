# Part 1737

Keep the return value in X0 when an immediate heap test precedes tuple or cons construction or a record update that consumes the saved value. Raise the initial test's live count from zero to one. Follow all uses until the saved slot is overwritten, cleared or released. Keep a save where a later instruction needs it after X0 is clobbered. The initial heap test remains at the same location with its original heap allocation size.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 565 images and reduces the total absolute Code gap by 1706 bytes. Normalized instructions gain 8 function matches and lose 0. No status changes.

2312 heap tests retain X0 for construction in 1802 functions across 524 modules with the same before and after sites. 914 also use X0 in the corresponding OTP call and construction sequence. 461 functions in 251 modules use fewer Y slots. Total allocated slots decrease by 497 across those functions. The first allocation now matches OTP in 81 functions in 68 modules.

The build and all six targeted tests passed. The [new fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_heap_call_result.erl) now uses one slot and keeps one X register live at both tuple heap tests, as in OTP. The six OTP VM checks exercise both branches and the fallback with atomic and heap-allocated inputs. The full fixture images still differ. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Heap results](heap-results.txt) · [Frame sizes](frame-sizes.txt) · [Probe](heap-result-probe.txt) · [Remaining heap spills](remaining-heap-spills.txt) · [Duplicate lines](duplicate-lines.txt)
