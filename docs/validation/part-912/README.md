# Part912 Wx option normalization

The emitter creates the option mapper in the call argument register with a zero-slot frame, swaps the list argument into place, and sends the mapped result to queue_cmd. The lifted mapper supports pass-through tagged options and a single Unicode-converting option. Its fallback constructs the badoption reason and tail-calls error/1.

The reference instruction layouts come from the pinned wxFindReplaceData, wxGridCellNumberEditor and wxTextDataObject images.

Six wx images matched Code and FunT after the emitter change. All six matched byte for byte with the shared generated wx build profile. The next common difference is object methods that normalize an optional list and expose a default-argument forwarding clause, including wxCloseEvent, wxEvent, wxClipboard, wxCaret and wxAuiManagerEvent.

All three focused compiler checks passed with the six expanded release regressions. [Focused report](focused.json).

The full OTP 29.1.1 Windows corpus contains 1,366 modules. Results were 209 Equal, 746 Different, 105 SourceRejected and 306 Unsupported. Six modules moved from Different to Equal. [Corpus report](otp-corpus.json).
