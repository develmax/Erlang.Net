# Part 1787

Remove zero-slot stack allocation and use the tail-call opcode without a frame when argument setup reaches the tail call without another call or branch. Preserve heap reservation as test_heap.

Follow the frame requirements in pinned OTP 29.1.1 beam_ssa_pre_codegen and the tail-call opcode selection in beam_z. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 344 changed images. Gained 412 and lost 9 normalized instruction matches. The sum of absolute Code size differences changed by -3004 bytes.

All 11 focused tests passed. Each compiler passed seven fixture assertions. The consume/2 and unpack/2 fixture instructions match OTP after normalization. In asn1ct_func:call_gen/3, allocate_heap with zero slots becomes test_heap and the resulting instructions match OTP. The broader compiler block had no completed report at publication.

Use the remaining corpus instruction differences to prioritize modes of frame allocation and register reuse.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
