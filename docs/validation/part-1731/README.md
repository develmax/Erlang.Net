# Part 1731

Copy a call result straight to the X register that supplies a terminal call argument. Check every intervening move for reads or writes that would conflict with the earlier copy. Preserve source lines and other argument moves. Dead stack-slot analysis removes the saved slot where it is unused.

All 1,366 sources compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. OTP accepted the 968 differing images. The total absolute Code gap decreased by 6,489 bytes across 381 changed images. It decreased in 380 images and increased in 1. The function instruction comparison gained one match and lost zero after normalizing labels and closure identifiers.

297 functions in 178 modules use fewer Y slots. Total allocated slots decrease by 319 across those functions. The first allocation now matches OTP in 43 functions in 36 modules. The scanned tail-result spill group drops from 766 functions to zero.

The build and six targeted tests passed. The OTP probe checks the direct X0-to-X1 copy and the one-slot frame, then compares four outcomes and side effects in the VM. The probe still differs from the OTP image. The Code chunk differs only by the order of the independent function-name assignment and the final X0 move. Attr also differs.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame sizes](frame-sizes.txt) · [Probe](tail-probe.txt) · [Tail call spills](tail-result-spills.txt) · [Ordinary call spills](ordinary-call-spills.txt)
