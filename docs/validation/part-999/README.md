# Part 999 wx tuple forwarding and option dispatch

Reuse forwarding parameter analysis for aliased tuple arguments. Test the tuple shape and fields before forwarding with default options, and share the object guards with the fallback branch. Emit grouped option switch targets in reverse postorder by tag, including groups that return the original tuple. The deterministic fixtures exercise both cases.

The complete Windows OTP 29.1.1 corpus has 289 Equal, 1074 Different and 3 SourceRejected. WxBufferedDC, wxHtmlWindow, wxSashLayoutWindow, wxSashWindow, wxSpinButton, wxSplitterWindow and wxStaticLine are new exact images. ELDAPv3 and all prior matches remain Equal. [Corpus report](otp-corpus.json). [Three focused checks passed](focused.json).

The tagged-tuple test difference before allocation dropped from 23 to 22 modules in the 235-image survey. [Opcode survey](wx-opcodes.txt). [Changed wx images](wx-changes.txt).
