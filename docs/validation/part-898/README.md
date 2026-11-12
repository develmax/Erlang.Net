# Part898 EUnit autoexport

The C# parser applies the built-in EUnit autoexport transform with OTP's default suffixes and wrapper name. It preserves an existing test/0 and otherwise appends the generated test/0 wrapper. The release metadata writer emits the generated export directly after the module form, removes the transform directive and places the generated function after the original EOF. Docs emits the hidden generated test entry with annotation zero.

The deterministic fixture covers test, generator and exported suffixes and the eunit_wrapper_/1 name. The complete fixture matches the pinned OTP image byte for byte. A separate probe with an existing test/0 and a list of compile options also matched byte for byte.

All three focused compiler cases passed. [Focused report](focused.json).

The remaining header documentation blockers are concentrated in SSH and Dialyzer. Their headers contain doc attributes attached to included types. These need source-file annotations and the same Docs reachability rules as the module's own types.

The final 1,366-image run found 137 Equal, 728 Different, 105 SourceRejected and 396 Unsupported. All ten modules previously stopped by eunit_autoexport passed source admission. Five generated differing images and five reached a later generation limit. [Corpus report](otp-corpus.json).
