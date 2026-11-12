# Part610 managed LDAP ASN.1 source generation and runtime_tools release parity

The corpus tool generates ELDAPv3.erl and ELDAPv3.hrl directly from ELDAPv3.asn1 in C#. The parser and writer cover the BER grammar used by the LDAP schema, including implicit tags, named and inline sequences, collections, recursive choices, enumerations, COMPONENTS OF, optionals and Boolean defaults. ASN.1 constraints are parsed as syntax and are not used for validation by this generator. The generated Erlang delegates low-level BER TLV I/O to asn1rt_nif at runtime. Source generation uses neither that module nor an Erlang executable.

The standalone command is:

```powershell
dotnet run --project tools/Validation/Erlang.BeamCorpus -- --generate-asn1 <schema.asn1> <output-directory>
```

This implements an alternative schema-driven Erlang emitter. It does not reproduce the original OTP ASN.1 compiler's source layout, internal helper graph or module metadata. The generated module reaches the BEAM emission profile check, but the C# compiler does not emit its BEAM yet. Functional BER comparison and release image comparison are separate.

The BEAM backend now emits a static external call with mixed constant and register arguments followed by a tagged result case. The success branch constructs a new tuple using the matched payload and a constant. The fallback returns the original call result. The runtime_tools release profile uses the same build options as the other verified runtime_tools application modules. Its generated 1,908-byte image matches the reference, and the regression test retains the inventory-verified .erl and .beam pair.

Both focused builds and eight selected tests passed. [Focused report](focused.json).

Standalone generation passed with an empty PATH and a copied schema as input. The original OTP test VM compiled the generated Erlang for an independent codec comparison. Eleven encode/decode examples, direct BER tag handling and all 23 record definitions matched the release. Examples cover bind and search messages, recursive filters, controls, sequence extensions, record aliases, octet strings and signed BER integer boundaries. A separate OTP VM loaded the C#-generated runtime_tools BEAM and exercised successful start, repeated-start error passthrough and stop.

| Standalone artifact | SHA-256 |
| --- | --- |
| ELDAPv3.asn1 | 0797323839ECB59B95D6A56603186F86F8C2A4E820444A82317D76A72575FF00 |
| ELDAPv3.erl | C88D882FDA840BAF24EB6FA80A93EF3348F3A369312F870118BF152E38C83535 |
| ELDAPv3.hrl | 73F57EA10D806EF83FDAFB58304194B3AE1E20940AC9928DC4682A8A54125371 |

The final 1,366-image corpus run exited 1 with 15 exact matches, 488 source rejections and 863 modules outside the current BEAM emission profile. The ELDAPv3 source is generated locally and classified Unsupported for BEAM emission. No module has a missing source. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
