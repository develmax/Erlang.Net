# Full uncached Windows release comparison

The source, compiler, SDK and original ERTS inputs are pinned in the identity metadata. Four isolated processes compiled all 1,366 Windows OTP 29.1.1 inputs without compiler-result caching.

| Equal | Different | Unsupported | SourceRejected | Elapsed seconds |
| ---: | ---: | ---: | ---: | ---: |
| 412 | 954 | 0 | 0 | 1,088.39 |

All 1,366 raw statuses and effective build descriptions retain their previous classifications. All 749 changed images pass the recorded Windows ERTS lane. 746 prepare normally. Crypto, zstd and pubkey_os_cacerts load separately, and all nine native observations agree across three sources. Strong validation finds zero uninitialized register updates on the affected 20-module record cohort.

Eight Line, five Dbgi, three AtU8, three ImpT and two Type sections gain raw equality. No previously exact chunk is lost. Eighty-three normalized functions gain equality, and none loses it. One hundred seven frame-distance comparisons improve. Two increase in ct_util:open_url/3 and cerl:update_map_literal/2. The Cerl shape is present in the preceding compiler and its independent six native observations agree.

The measured previous full run took 1,055.41 seconds. The balanced partition plan reduced its predicted longest group by 2.22%. The new input compiler and concurrent host work differ, and these two times give no isolated speedup measurement.

[Raw reports](full-uncached/otp-corpus.json) · [Raw chunk delta](full-uncached/section-delta.json) · [Native preparation](full-uncached/loader-summary.terms) · [Native on-load calls](full-native-on-load/receipt.json) · [Pinned toolchain](toolchain-identity.json) · [Effective build profiles](profile-equivalence.json) · [Scoped tests](../scoped/README.md).
