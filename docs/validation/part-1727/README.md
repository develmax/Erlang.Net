# Part 1727

Delay the shared frame until a point that dominates all continuations requiring it. Keep short returns and entry tests ahead of that point when the framed region cannot re-enter the region that executes without a frame. The planner inserts a single copy of the original allocation and setup. BEAM labels remain valid branch targets.

All 1,366 sources compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. OTP accepted the 968 differing images. The total absolute Code gap decreased by 3,100 bytes across 391 changed images. It decreased in 389 images. In http_response and ex_webview the candidate Code section was already smaller than OTP and shrank by a further two bytes. The function instruction comparison gained 0 matches and lost 0 after normalizing labels and closure identifiers.

187 functions in 134 modules now start with the same predicate as OTP before the first frame allocation.

The build and all three targeted tests passed. The standalone probe compiles the same source with OTP and the C# compiler, checks the allocation placement in the C# image and compares four results in the OTP VM. The previous compiler image failed the allocation placement assertion. The candidate probe still has whole-image differences.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Entry instruction order](entry-order.txt) · [Probe](shared-frame.txt) · [Remaining reloads](entry-reloads.txt)
