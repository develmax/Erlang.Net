# Original OTP common_test library

Pinned OTP-29.1.1 common_test 1.31.2: all47 application-descriptor images and2 additional installed images. Preserve all110 original release files, nested source/header inputs, application resources and available support data. Provenance distinguishes pinned Git matches and generated release inputs; upstream notices and third-party support notices remain intact, with the existing OTP package license.

Reference Erlang.Otp.CommonTest.Beam from an ordinary .NET project. OtpCommonTest.Modules exposes every packaged image (including optional property-test extensions when present); ReadImage/ReadApplication/ReadApplicationUpgrade return independent hash-checked bytes. OtpCommonTest.Beam(module,dependencies) selects original execution on our .NET runtime explicitly. Dependencies/native providers remain explicit; no backend fallback or automatic application startup is introduced.

SupportFiles/ReadSupportFile whitelist original hash-checked support data. Reading assets does not execute scripts or install code:priv_dir paths. Common Test optional proper_ext images retain their paths and are also selectable by their module names; external property-testing frameworks are not bundled.

Finite configuration/group and warning/Core-helper observations do not certify the complete Common Test runner, external property frameworks, full Dialyzer analysis/PLT lifecycle, distributed nodes, graphical tools or complete application boot. No original VM executable or producer native library is shipped.
