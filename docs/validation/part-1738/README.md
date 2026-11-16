# Part 1738

Remove repeated source line instructions in the same basic block when they name the same position index. Start tracking a new position at labels, function metadata, and debug or executable line operations. Apply the rewrite after code layout and adjust the Line instruction count. The reference is pinned OTP beam_z:remove_redundant_lines.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 831 images and decreases the total absolute Code size gap by 75382 bytes. Normalized instructions gain 63 function matches and lose 0. No status changes.

25457 line instructions removed in 831 images. All changed counters match their emitted code and position tables retain their entries. 10 Line chunks gain exact equality with OTP and 2 lose it.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_heap_call_result.erl) now matches the entire OTP Line chunk, with the same five call locations and two fewer line instructions. The full fixture images still differ. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Line tables](line-tables.txt) · [Probe](line-probe.txt) · [Within-block lines](within-block-lines.txt) · [Across-block lines](across-block-lines.txt)
