# Part900 Deprecation metadata in Docs

The Docs writer applies deprecations from function, type and callback attributes. An explicit Docs deprecation takes precedence over the attribute. Specific name and arity rules take precedence over wildcard rules. The writer no longer rejects every module that has unrelated attributes.

The deterministic fixture covers function and type deprecations, a wildcard rule and an explicit override. The pinned release formats function references as name/arity and uses underscore placeholders for type and callback parameters.

A second deterministic probe with a deprecated type of arity two and a deprecated callback of arity one also matched the complete OTP image byte for byte.

All three focused compiler checks passed. [Focused report](focused.json).

The final 1,366-image corpus reported 137 Equal, 772 Different, 105 SourceRejected and 352 Unsupported. Twenty modules moved from Unsupported to Different. Existing exact matches were preserved. [Corpus report](otp-corpus.json).
