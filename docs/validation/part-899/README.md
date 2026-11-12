# Part899 Documentation attached to types in headers

Docs now consumes doc attributes from included files. The entry annotation retains the header file and the doc attribute's location. The source_anno metadata still refers to the type definition. The release abstract-code writer accepts included doc directives and leaves them out of Dbgi, consistent with OTP.

The deterministic fixture covers a documented type with group metadata, a hidden type and an undocumented type from the same header. All three are reachable from an exported function spec.

The complete deterministic fixture matched OTP byte for byte. All three focused compiler tests passed. [Focused report](focused.json).

The complete run found 137 Equal, 752 Different, 105 SourceRejected and 372 Unsupported out of 1,366 images. Twenty-four modules moved from Unsupported to Different, including seventeen SSH and seven Dialyzer modules. Existing exact matches were preserved. [Corpus report](otp-corpus.json).
