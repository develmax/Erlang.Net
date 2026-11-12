# Part617 LDAP signed integer instructions

The C# BEAM backend emits the three signed-integer helpers in the generated LDAP module. It lowers the recursive signed width calculation, a signed match whose size is derived from bit_size/1, and signed binary construction after an integer guard. The BEAM type table now emits optional integer bounds, and the binary match preserves the source location for a failed match.

The deterministic probe contains the three generated Erlang function bodies. Its generated BEAM matches the OTP reference after removing Docs for the fixture profile. An original OTP VM checked widths and round trips for 12 integers, including a 181-bit positive value, and decoding of nine bitstrings including empty, non-byte-aligned, and 200-bit inputs. Invalid inputs retained their badarg or function_clause behavior.

The isolated audit emits 118 of 130 source function bodies. Twelve fail clause shape validation. Whole-module ELDAPv3 emission remains open. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
