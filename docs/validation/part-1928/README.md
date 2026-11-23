# Part 1928 held Windows OTP 29.1.1 comparison

The attempted package preserved 475 Equal and 891 Different, with zero Unsupported and zero SourceRejected in 628.603 seconds. All 264 targeted tests passed and all 475 earlier whole-image matches remained. The linked fixtures matched six ASN.1 modes and 16 wx observations.

The pinned disassembler rejected the new asn1ct image because three local helper targets landed inside function bodies. The managed loader independently rejected the same image. The targeted ASN.1 runs did not exercise those calls. This candidate remains held. Part 1927 is the accepted release baseline.

[Raw release comparison](full-uncached/otp-corpus.json) · [Unmodified disassembler error](full-uncached/normalized.stderr.log) · [Raw call targets](asn1ct-local-entry-conflict/Candidate-raw-entries.log).
