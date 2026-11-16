# Part 1358 — Construct tuple results inside register cases

A planned case branch can return a tuple containing an ordinary call, written inline or bound to a fresh variable first. It uses the existing call argument scheduler and tuple builder, keeping the result in X0. A branch can use the result more than once.

Stack slots used only for the call are released before the call. A live value can move to a surviving slot before a local call when that permits trimming. Values used in the returned tuple remain live. A maps:find/2 branch that projects the value from an ok tuple uses a tuple test and reads the field directly into the call argument.

The fixture compares four functions with OTP after label normalization and checks exact fallback behavior, stack trimming, preservation of a value across the call, repeated operands, and a projected field. Six prior compiler probes also passed.

The source audit finds computed tuple results under tuple case patterns in 129 modules of the prior differing corpus, including 98 without an unguarded variable arm. A field used only after the call appears in 36 modules, with nested tuple fields in eight. These source counts overlap.

The final Windows OTP 29.1.1 run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading.

Eight images changed and their Code size gaps narrowed by 122 bytes in total. Four functions gained normalized instruction matches with none lost. Four functions reached OTP's peak frame size. The xmerl_xpath:expanded_name/3 case still needs nested tuple matching and delayed field extraction. Full runtime tests and readiness regeneration were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Deferred field example](deferred-case-example.txt)
- [Focused check](check-computed-case-tuple.ps1)
- [Focused output](probe.txt)
- [Literal case regression](literal-case-regression.txt)
- [Two-call case regression](two-call-case-regression.txt)
- [Call tuple sequence regression](call-tuple-sequence-regression.txt)
- [Entry tuple binding regression](entry-tuple-binding-regression.txt)
- [Call sequence regression](entry-sequence-regression.txt)
- [Boolean case regression](boolean-case-regression.txt)
- [Deferred field audit](deferred-case-fields.txt)
- [Audit script](rank-deferred-case-fields.eval)
