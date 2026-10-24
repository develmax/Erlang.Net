# Original OTP asn1 library

Pinned OTP-29.1.1 asn1 5.5.2: all 22 original BEAM images, Erlang sources, headers and application/upgrade resources. Sources and images are verified against the admitted official Windows x64 release; provenance identifies generated sources separately. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.Asn1.Beam as a ProjectReference or PackageReference. OtpAsn1.Modules enumerates available names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpAsn1.Beam(module, dependencies) creates an explicit original-image startup selection. Declare dependencies and native bindings separately. No original VM executable or silent C# fallback is shipped.

Inventory completeness does not imply execution or compatibility of every API, compiler self-hosting or automatic application startup. See docs/OTP_LIBRARY_PACKAGING.md for actual validation scope.

The application descriptor names one module (asn1rt_nif), while the official ebin directory supplies22 images. This library retains all22. Of25 source/header files,23 match pinned Git and two are generated release sources (asn1ct_eval_ext.erl and asn1ct_rtt.erl); provenance preserves this distinction. The original native ASN.1 NIF binary is not included. Current execution checks cover EXTERNAL transformations and compiler-table helpers, not ASN.1 compilation or generated codecs.
