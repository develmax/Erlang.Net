# Part 1732

Forward results to ordinary call arguments when the target X register is available during argument setup and the old stack slot is dead on all reachable continuations. Keep the spill in functions containing exception handlers. In the dynamic tail apply form, place an independent function-name assignment before the final X0 load to match OTP's operand order.

All 1,366 sources compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. OTP accepted the 968 differing images. The total absolute Code gap decreased by 7,241 bytes across 422 changed images. It decreased in 419 images and increased in 3. The function instruction comparison gained 3 matches and lost 0 after normalizing labels and closure identifiers.

168 functions in 109 modules use fewer Y slots. Total allocated slots decrease by 178 across those functions. The first allocation now matches OTP in 53 functions in 43 modules. The monitored ordinary-call spill pattern drops from 697 to 87 functions.

Six focused tests passed. The dynamic-tail fixture matches the entire OTP image and remains in the whole-module regression suite. The ordinary-call callback probe verifies direct X0-to-X1 forwarding and three outcomes and effects against OTP. The callback probe still has whole-image differences.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame sizes](frame-sizes.txt) · [Probe](ordinary-probe.txt) · [Ordinary call spills](ordinary-call-spills.txt) · [Argument move order](argument-move-order.txt)
