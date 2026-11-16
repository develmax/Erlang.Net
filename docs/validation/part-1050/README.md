# Part 1050 literal list suffixes

The register list emitter folds the trailing constant elements into one literal and allocates heap space only for the remaining cons cells. This follows the literal put_list simplification in OTP 29.1.1 beam_ssa_type.erl.

All three executed probe functions match the decoded OTP instructions after source paths are normalized. The comparison across differing modules paired 78 straight-line list tail bodies by function name and arity. It found 21 new matches, 52 retained matches and five remaining differences.

The full corpus admitted and generated 1363 of 1366 files. The 342 Equal, 1021 Different and 3 SourceRejected statuses were unchanged for every file. ELDAPv3 remains Equal. All 1021 differing files prepared successfully in OTP.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [List body comparison](list-bodies.txt)
- [Loader results](loader.txt)
- [Executed probe](probe-after.txt)
- [Probe instruction comparison](probe-comparison.txt)
