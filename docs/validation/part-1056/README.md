# Part 1056 preserve live format arguments

The register format emitter handles two common shapes. A call followed by a constant return builds an argument list in X registers using a zero-slot frame. A formatted tail call preserves only the values needed after the append chain, while the intermediate string remains in X registers.

The generated snmpa_error_io.beam and snmpm_user_default.beam match the Windows OTP 29.1.1 images byte for byte. The executed probe matches OTP's decoded instructions for all three functions after source paths are normalized.

The full corpus admitted and generated 1363 of 1366 files. It now has 346 Equal, 1017 Different and 3 SourceRejected. The two SNMP images are the only status changes. ELDAPv3 remains Equal. All 1017 differing images prepared successfully in OTP.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Probe comparison](comparison.txt)
- [Executed probe](after.txt)
- [Permanent release fixtures](fixture.json)
