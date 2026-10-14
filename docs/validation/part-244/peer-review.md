# Independent bounded receive source review

Reviewed on 2026-10-14 against the revision3 frozen receive candidate on published242. This is source review; actual full validation is recorded separately.

The decoded matcher clones the pre-receive X-register state for each message and has a finite validated graph. Matching can only read/move values, test integer/exact equality and terminate at remove_message or loop_rec_end. It cannot send, call BIFs or mutate the mailbox. The interpreter delegates selection to the existing atomic ProcessContext.ReceiveAsync and commits the selected register state only after consumption. Unmatched messages therefore remain admitted to the actual mailbox. Timeout resumes after the decoded timeout marker. Send and self/0 use the actual runtime context and registry.

Zero-slot allocation records function ownership, continuation depth and saved continuation. The control-flow guard rejects unbalanced allocation/deallocation and inconsistent depth at merged paths before body effects. Nonzero/Y-register frames remain unsupported. This review does not establish general non-tail-call or physical BEAM frame compatibility.

The genuine receive fixture is unchanged and compared to its existing independently captured native ETF. Three modified-bytecode variants are explicitly modeled lifecycle checks, not new original OTP observations. The timeout operand support is restricted to valid static integer/infinity values; dynamic and invalid timeout semantics remain unsupported, and physical save-pointer/reduction parity is not claimed.

No blocking source defect found within this declared subset. Module publication, generated information, on_load, native exception frames, tracing invocation parity, physical heaps and general loading remain outside this review. Inherited source-resource LF/CRLF provenance must be recorded separately from exact binary BEAM/ETF bytes.
