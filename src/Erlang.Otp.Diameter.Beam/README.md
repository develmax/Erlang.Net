# Original OTP diameter library

Pinned OTP-29.1.1 diameter 2.7.2: all 47 original BEAM images, original source/header/dictionary inputs and application resources. Each of the 115 packaged files matches the admitted official Windows x64 release; provenance distinguishes pinned Git matches and generated release inputs. Upstream notices and the existing OTP package license remain intact.

Reference Erlang.Otp.Diameter.Beam through ProjectReference or PackageReference. OtpDiameter.Modules exposes the catalog; ReadImage, ReadApplication and ReadApplicationUpgrade return independent hash-checked bytes. OtpDiameter.Beam(module, dependencies) creates an explicit BEAM selection; dependencies/native providers remain explicit.

OtpDiameter.SupportFiles whitelists original relative support paths. ReadSupportFile returns independent verified data, including original launcher source. Reading never executes a launcher, starts a service, installs code:priv_dir mappings or falls back to a handwritten implementation. Original scripts/MIB data retain their exact bytes and package paths.

Complete packaging and finite helper/codec comparisons do not certify live protocol applications, peer communication, native cryptography, SNMP agents/managers, compilation of every dictionary/MIB or full module compatibility.
