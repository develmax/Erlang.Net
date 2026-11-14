# Part 1632 — Allocate the frame after singleton list dispatch

An atom wrapper can tail-call the same function with a singleton list before allocating a frame. When the singleton branch converts its head and passes the result through nested checked calls, it retains only that result in Y0. The final call loads the result and trims the frame. Each rejection branch returns with the frame size active in that branch. Constant fallback expressions include tuples. Pinned OTP release disassembly and the existing entry call and tail emitters provide the reference. No dependency was added.

The permanent fixture matches OTP byte for byte. The OTP VM verified successful dispatch, both rejected call results, and function_clause for invalid list shapes. Three focused tests passed. [Hashes](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

The Windows corpus accepted and generated all 1,366 sources. 394 matched byte for byte, including ELDAPv3, and 972 differed. Observer was the only changed image and moved from Different to Equal. The Code section went from 476 bytes to the reference 457 bytes. The OTP loader accepted all 972 differing images. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Previous observer instructions](observer-before.txt) · [Loader and size](loader-and-code.txt).

The builds used SDK 10.0.404 through MSBuild. The full test suite and readiness regeneration were outside this run.
