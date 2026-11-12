# Part616 LDAP protected calls and sized results

The C# BEAM backend emits the protected call shapes used by the generated LDAP encode and decode functions. It installs a handler, preserves live arguments, ends the try region on success, builds the exception stack on error and exit, and propagates other exception classes. The decode path checks the returned pair and raises badmatch inside the protected region. The sized result flow keeps the schema and converted value across local calls, trims the frame and returns the binary with its byte count.

The deterministic probe uses the three generated LDAP function bodies with fixture dependencies for controlled return values and exceptions. The complete C# output matches the OTP reference after removing Docs for the fixture profile. In original OTP, the generated image passed successful encode and decode, error and exit with stacks, badmatch handling, throw propagation, binary size and invalid byte_size input.

The isolated audit now emits 115 of 130 source function bodies. Thirteen functions fail the clause check and two fail during body emission. Whole-module ELDAPv3 emission remains open. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
