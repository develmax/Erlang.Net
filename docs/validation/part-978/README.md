# Part 978 guarded frameless forwarding

Extend the existing local tail forwarder from a single record guard to conjunctions of record and unary type tests. Check the full guard before emitting instructions. Keep the original X registers available on failure and use call_only on success, with an optional empty list default argument. The release disassembly of wxAuiManager:addPane/2 shows this layout. No dependency was added.

The deterministic guarded forwarder fixture matches OTP byte for byte. All three focused compiler checks passed. [Focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 224 Equal, 1139 Different and 3 SourceRejected. WxListView is the new exact match and ELDAPv3 remains Equal. [Corpus](otp-corpus.json).

In 235 generated wx images, the count of functions that allocate a frame only in our output fell from 333 to 184. The number of affected modules fell from 102 to 73. The tagged tuple versus allocate first opcode divergence fell from 107 to 77 modules. The most frequent remaining frameless pattern is an option handler with nested tuple tests and dispatch. [Frame counts](frame-counts.txt), [remaining frameless patterns](frameless.txt) and [opcode comparison](wx-opcodes.txt).
