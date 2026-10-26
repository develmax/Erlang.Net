# Original OTP edoc library

Pinned OTP-29.1.1 edoc 1.5: all 21 original BEAM images, Erlang sources, headers and application/upgrade resources. Sources and images are verified against the admitted official Windows x64 release; provenance identifies generated sources separately. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.Edoc.Beam as a ProjectReference or PackageReference. OtpEdoc.Modules enumerates available names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpEdoc.Beam(module, dependencies) creates an explicit original-image startup selection. Declare dependencies and native bindings separately. No original VM executable or silent C# fallback is shipped.

Inventory completeness does not imply execution or compatibility of every API, compiler self-hosting or automatic application startup. See docs/OTP_LIBRARY_PACKAGING.md for actual validation scope.

OtpEdoc.SupportFiles enumerates four original priv assets; ReadSupportFile(name) returns hash-checked independent bytes from that whitelist. These include the original stylesheet, DTD, image and edoc_generate escript source. Reading it does not execute a command or deploy files into code:priv_dir. A complete rendering/filesystem pipeline remains separate work.
