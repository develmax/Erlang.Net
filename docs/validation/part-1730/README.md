# Part 1730

Reuse Y registers in straight-line code when the original stack slots have disjoint lifetimes. Preserve a live value when an initialization clears another slot assigned to the same register. Use the new size in allocation, deallocation and framed tail instructions. OTP beam_ssa_pre_codegen provides the linear-scan and reversed Y numbering references.

All 1,366 sources compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. OTP accepted the 968 differing images. The total absolute Code gap decreased by 923 bytes across 430 changed images. It decreased in 194 images and increased in 2. The function instruction comparison gained one match and lost zero after normalizing labels and closure identifiers.

1133 functions in 414 modules use fewer Y slots. Total allocated slots decrease by 1654 across those functions. The first allocation now matches OTP in 101 functions in 78 modules.

The build and six focused tests passed. Three fixtures check frame sizes and compare six results in the OTP VM. The callback fixture explicitly runs garbage collection while the compiled caller has live values in the frame. The fixtures still have whole-image differences.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame sizes](frame-sizes.txt) · [Probes](linear-probes.txt) · [Call result spills](tail-result-spills.txt)
