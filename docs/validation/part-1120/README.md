# Part 1120 — keep input registers live through an arithmetic case

A terminal case with a direct arithmetic selector, a literal test and a wildcard branch can run without a frame. The selector writes to a scratch register. The branches reuse the original arguments. A successful integer remainder or division proves its variable operands are integers in both branches. Nested arithmetic preserves any entry register still read by an enclosing operation.

The OTP 29.1.1 probe matched the full instruction list for the remainder case. Execution covered both branches, preservation of X0 during nested arithmetic, and badarith.

- [Probe source](beam_guarded_bif_tail_probe.erl)
- [Execution assertions](run.eval)
- [OTP and generated disassembly](disassembly.txt)

The release remainder case in fQuotient/2 matches OTP modulo labels and no longer allocates four slots. The remaining differences in that function are in the preceding division branches. The full corpus generated 1,363 images, with 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The sole changed image, xmerl_xsd_type, has a smaller Code length gap.

- [Release comparison](fquotient.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
