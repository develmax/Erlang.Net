# Part 1404 — Schedule colour conversion after wx options

The emitter checks colour tuples before allocating a frame. It maps the options first, then uses swaps to preserve the options and any earlier conversion while the next colour call runs. The forwarding emitter uses the same colour tests and can interleave record and tuple shape checks by entry register.

The deterministic fixture matches OTP byte for byte. The queue stub verifies arguments for a single colour and a pair, including default options and rejection of an invalid option before colour conversion. The fixture also exercises forwarding with a record guard between tuple arguments.

The full run generated all 1,366 images. The strict comparison has 379 Equal, 987 Different, zero SourceRejected and zero Unsupported. wxDC is newly equal. ELDAPv3 and all previous exact matches remain equal. OTP prepared all 987 differing images for loading. All three selected compiler cases passed with the corpus compiler assembly.

Two release images changed. Seven functions gained normalized instruction matches and none lost them. Code gaps narrowed by 919 bytes across wxDC and wxRegion. wxBrush, wxPen and wxTextAttr retained their exact matches.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Release targets](release-targets.txt) and [instruction comparisons](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-wx-option-colour.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Remaining target functions](remaining-target-functions.txt) and [wxRegion instructions](region-remaining.txt)
