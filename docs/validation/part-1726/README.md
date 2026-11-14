# Part 1726

Move an entry frame below eligible predicates when only one continuation needs it. Run the predicates on the incoming X registers, preserve arguments on function clause failures and remove frame cleanup on directly returning alternatives. Forward copied arguments on paths that now allocate after an entry predicate, including dynamic apply. The initial experiment duplicated setup on branches and reduced the exact count from 398 to 395. It changed dbg_idb, ELDAPv3 and snmp_pdus_basic. The final pass preserves the shared allocation in that case.

All 1,366 release files were generated. 398 images matched exactly, including ELDAPv3 built from ASN.1 by the C# generator. OTP accepted all 968 differing images. The total absolute Code size gap decreased by 9,326 bytes across 471 changed images. The gap decreased in 402 images and increased in 17. After normalizing labels and closure identifiers, 4 functions gained instruction parity and 0 lost it.

402 functions in 241 modules now start with the same predicate as OTP before the first frame allocation.

The build had zero warnings and the three targeted tests passed. The deterministic fixture compares a complete module whose list and map clauses test their arguments before dynamic calls. Its fallback clauses return without a frame.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Entry instruction order](entry-order.txt)
