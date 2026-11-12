# Part 914 wx option mapping and replies

The object command emitter preserves additional live arguments across lists:map/2 and trims the remaining frame before queue_cmd when the wrapper also receives a reply. The Unicode conversion path uses the same reply handling. The option guard is resolved through the guard argument map so a receiver record does not shift the register check.

wxAuiPaneInfo demonstrates both the option mapper with a reply and the Unicode conversion with a reply. [Pinned disassembly](reference-disassembly.txt).

The previous full report identified 16 unsupported modules at is_bitstring/1, 12 at is_reference/1 and 11 at is_map_key/2 in guard expressions. These are first failures and some modules may reveal further blockers after guard emission is implemented.

The 37 instructions in wxCheckListBox:check/3 match after normalizing label IDs, fun table IDs and source filenames. This checks the live integer argument across the mapper. The complete wxCheckListBox module still differs. [Function comparison](additional-live-argument.txt).

All three selected compiler tests passed, including the seven new complete release-image regressions. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 corpus contains 1,366 files. The final run found 221 Equal, 734 Different, 105 SourceRejected and 306 Unsupported. Seven modules moved from Different to Equal, with no other status changes. [Corpus report](otp-corpus.json).
