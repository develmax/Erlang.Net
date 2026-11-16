# Part904 List head extraction in tuple results

The emitter uses input registers directly for distinct list-head projections in a tuple result. It checks lists in expression order, places the heap check before the final extraction, and emits the failure labels in reverse order with the original match location.

The OTP fixture exercises two independent inlined head extractions and a preserved caller argument. Separate OTP probes established the same instruction order for one, two and three projections.

The two complete deterministic images matched byte for byte. All three focused compiler checks passed. [Focused report](focused.json).

The full corpus reported 137 Equal, 818 Different, 105 SourceRejected and 306 Unsupported. Status counts stayed unchanged. The next metadata candidates are ssh_channel, ssh_server_key_api and ssh_client_key_api. [Corpus report](otp-corpus.json).
