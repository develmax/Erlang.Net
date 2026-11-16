# Part910 Wx string conversion wrappers

The object queue path recognizes a final chardata argument and its conversion through unicode:characters_to_binary/1. It emits the list-or-binary guard, trims the consumed stack slot before conversion, obtains wx_env in a separate register and forwards the converted argument to the tail queue call. The reference layout was checked for wxControl:setLabel/2 and wxTextCtrl:replace/4.

The deterministic probe with arities two and four matched OTP byte for byte. In the targeted wx scan, wxControl matched with the new emitter. The existing generated wx profile closed the five remaining metadata-only candidates.

All three focused compiler tests passed with six new release fixtures. [Focused report](focused.json).

The full Windows OTP 29.1.1 corpus reported 201 Equal, 754 Different, 105 SourceRejected and 306 Unsupported. All six status changes were Different to Equal. [Corpus report](otp-corpus.json).
