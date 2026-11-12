# Part 1151 — sort stack loads during tail argument setup

The shared argument emitter sorts consecutive Y-to-X loads by source slot. It retains the X0 boundary and does not move loads across slot kills, register transfers or constants.

The corpus audit simulated sorting on final disassembly. It found 40 potential instruction matches and one potential loss in wxDC after frame trimming. The emitter change is narrower. It changed 90 images and 1,183 functions. Forty functions gained an instruction match after label normalization, including the asn1ct test_value closure. No matched function regressed.

The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. All three focused compiler tests passed.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader results](loader.txt)
- [Disassembly audit](move-audit.txt)
- [asn1ct instruction comparison](asn1ct-test-value.txt)
- [Next frame allocation example](frame-example.txt)
