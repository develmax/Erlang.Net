# Part 1448 — Direct record field destinations for call operands

The compiler emits the final computed record-field operand into its call argument register. The shared record emitter preserves the record shape check and the failing value. In mod_head:do/1, the extraction for proplists:get_value/2 writes x1 directly and removes the move from x0. The function still allocates three frame slots and OTP allocates one.

The fixture verifies direct extraction to x1, the successful result, and the badrecord reason in both compiled images. The focused .NET run passed three cases. The compiler DLLs used by the corpus and the test runner have the same hash.

- [Fixture instructions and execution](probe.txt), [focused tests](focused.json), [compiler hashes](compiler-hash.txt), and [release instructions](release-fields.txt)

The Windows OTP 29.1.1 comparison generated all 1,366 images with zero source rejections. It reports 382 Equal, including ELDAPv3 generated from ASN.1 by the C# generator, and 984 Different. Both loaders accept all 984 differing images. The shared rule changes 275 images and 1,036 functions, and the aggregate absolute Code size gap falls by 5,664 bytes. The field-destination scan falls from 3,689 functions in 708 images to 2,923 functions in 676 images.

A byte comparison of each chunk, including its padding, finds at least four differing chunks in every remaining image. Code differs in all 984. The chunk counts describe where the differences appear. A changed code label can change the export and local function tables, and changes to the essential chunks can change the default module version in Attr.

- [Corpus](otp-corpus.json), [delta](summary.txt), [remaining field destinations](direct-field-destinations-after.txt), [chunk comparison](chunks-after.txt), and [previous chunk comparison](chunks-before.txt)
