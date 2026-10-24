# Original OTP compiler library

Pinned OTP-29.1.1 compiler 10.0.6: all 59 original BEAM images, Erlang sources, headers and application/upgrade resources. Sources and images are verified against the admitted official Windows x64 release; provenance identifies generated sources separately. Upstream notices and OTP-LICENSE.txt are retained.

Reference Erlang.Otp.Compiler.Beam as a ProjectReference or PackageReference. OtpCompiler.Modules enumerates available names; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpCompiler.Beam(module, dependencies) creates an explicit original-image startup selection. Declare dependencies and native bindings separately. No original VM executable or silent C# fallback is shipped.

Inventory completeness does not imply execution or compatibility of every API, compiler self-hosting or automatic application startup. See docs/OTP_LIBRARY_PACKAGING.md for actual validation scope.
