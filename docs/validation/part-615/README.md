# Part615 LDAP local bindings and tuple tag extraction

The emitter handles local call results passed to a tail call with preserved arguments and pair bindings followed by list and tuple construction. It saves live values in frame slots. A callee without a proven return shape is checked before extracting pair members, with badmatch on failure.

The tagged tuple emitter reuses arity selection for wire_tag/1. It distinguishes recursive references, field extraction, a constant result and the element/2 fallback.

The deterministic fixture uses the actual decode_with_tags/3, decode_field/3 and wire_tag/1 bodies. The C# BEAM matches the original compiler output after removing the reference Docs chunk for the fixture profile. An original OTP VM loaded the generated image and checked successful local binding, tag dispatch and the badmatch and badarg paths.

The per-function audit now emits isolated bodies for 112 of the 130 source functions. Thirteen fail the clause check and five fail during body emission. This audit supplies local call targets and verifies instruction emission in isolation. Whole-module ELDAPv3 emission remains unsupported. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The complete OTP corpus run checked 1,366 images and recorded 18 exact matches, 488 source rejections and 860 modules outside the current emission profile. ELDAPv3 remains in the last group. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
