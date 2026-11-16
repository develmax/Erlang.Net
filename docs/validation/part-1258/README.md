# Part 1258 — Remove proven nonempty list entry checks

Omit the first cons shape test in clause-local frame lowering when all known calls to a private function pass a syntactic cons or a cons literal for that argument. Keep the test for exports, captured local functions, native declarations and inputs whose shape is unknown.

The instructions of `asn1ct_gen:traverse_SO_type_structure/5` match Windows OTP 29.1.1 after label normalization. Its guard and fallback still use the correct head, tail and original input. The probe also checks an improper cons on the private path and non-cons arguments on the exported and captured paths.

The full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Only `asn1ct_gen.beam` changed and its Code length moved four bytes closer to OTP. The C# ASN.1 generator supplied `ELDAPv3.erl`, and its BEAM remains Equal. OTP accepted all differing pairs.

- [Probe source](private_cons_probe.erl)
- [Probe command](check-private-cons.ps1)
- [Execution result](probe.txt)
- [Release instruction comparison](traverse-type-structure.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
