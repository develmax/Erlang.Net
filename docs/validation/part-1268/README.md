# Part 1268 — Select receive clauses by tuple arity

The scan of the 1,007 differing images identified 319 functions in 149 modules with receive and tuple arity selection in the OTP function, but only receive in the generated function. The scan compares operations at function granularity. Some functions contain multiple independent matches, so this is a candidate cohort.

When all top-level receive clauses match tuples and more than one arity occurs, emit one tuple shape test and a shared arity selection. Preserve clause order within each arity and resume scanning the mailbox after the last failed clause in the group.

The focused probe ran both the OTP and C# images. It verified mailbox order across different tuple arities, an already bound variable, guard fallthrough, a timeout and preservation of unmatched messages.

In gl:rec/1, the selection table now lists arities 2 and 3 in release order, with the three-element branch emitted first. The remaining mismatches include the seven-slot frame (one slot in OTP), repeated field extraction and call lowering. The number of fully matching functions in gl remains 1,066 of 1,068.

The standalone gl Code section decreased from 56,862 to 56,857 bytes, compared with 56,777 in OTP. AtU8, ImpT and Type now match. ExpT and LocT now differ because the labels in 1,057 export entries and seven local entries are each one higher. The additional join label in rec/1 remains after the wait loop.

The full corpus generated all 1,366 images. The totals remain 359 Equal, 1,007 Different and zero SourceRejected. ELDAPv3 remains Equal. The OTP loader accepted all 1,007 differing pairs. There were 105 changed images. The absolute Code size difference decreased in 51, increased in 51 and was unchanged in three, for an aggregate decrease of 885 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [gl table label deltas](gl-label-deltas.txt)
- [gl section comparison](gl-chunks.txt)
- [gl instruction comparison](gl-receive.txt)
- [asn1_db selection count](asn1-db-dispatch.txt)
- [Corpus scan](receive-arity.txt)
- [Scan command](rank-receive-arity.eval)
- [Focused probe](probe.txt)
- [Probe command](check-receive-arity.ps1)
