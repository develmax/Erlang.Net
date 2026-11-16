# Part 979 nested wx option tuples

The option handler validates a nested tuple shape and returns the original option. Tags with the same nested arity share a branch. Plain options return directly. Repeated bindings continue through the general matcher and invalid options retain the badoption error. The reference instruction sequence comes from wxAuiNotebook's create/4 option closure in OTP 29.1.1. No dependency was added.

The deterministic mixed nested and plain options fixture matches OTP byte for byte. All three focused compiler checks passed. [Focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 225 Equal, 1138 Different and 3 SourceRejected. WxPanel is the new exact match and ELDAPv3 remains Equal. [Corpus](otp-corpus.json).

The generated wx subset has 59 fewer excess frames. The remaining excess frames affect 125 functions in 59 modules. Guarded disjunction forwarding accounts for several leading remaining patterns. [Frame counts](frame-counts.txt), [remaining frameless patterns](frameless.txt) and [opcode comparison](wx-opcodes.txt).
