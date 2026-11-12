# Part 1402 — Validate and convert paired colour arguments

The wx call emitter checks both colour tuples in registers before allocating the frame. It converts adjacent colour arguments in source order, reuses the first colour's stack slot and trims the frame before the second conversion. The final register moves account for the position of the second colour and trailing operands.

The deterministic OTP fixture matches byte for byte. The oracle uses an Erlang queue stub to compare the converted values, argument order and return values for leading, trailing and middle colour pairs. It also checks malformed tuples, an invalid tuple field and a missing environment. All three selected compiler tests passed with the corpus compiler assembly.

All 1,366 release images generated. The strict comparison has 376 Equal, 990 Different, zero SourceRejected and zero Unsupported. wxListItemAttr is newly equal and ELDAPv3 remains equal. OTP prepared all 990 differing images for loading.

Three release images changed. Five functions gained normalized instruction matches with none lost. Code gaps narrowed by 959 bytes across wxCalendarCtrl, wxDC and wxListItemAttr. wxGraphicsContext retained its complete match.

wxCalendarCtrl still has differences in two options callbacks and their callers, and in the command that repacks a nested date tuple. The same option callback shape occurs in wxDatePickerCtrl. wxDC still differs in the colour pair followed by options mapping, among other functions.

- [Complete corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Release targets](release-targets.txt), [target functions](remaining-target-functions.txt) and [instructions](operands.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-wx-colour-pair.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Remaining calendar instructions](calendar-remaining.txt) and [date tuple sources](calendar-tuple-sources.txt)
