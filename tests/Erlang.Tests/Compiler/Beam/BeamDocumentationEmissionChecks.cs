using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamDocumentationEmissionChecks
{
    internal const string TestName = "compiler/beam/documentation-payload-matches-otp";
    private const string Failure = "BEAM documentation fixture differs from OTP.";
    private const byte CompressedEtfTag = 80;

    internal static Task Run()
    {
        foreach (string name in new[] { "beam_docs_probe", "beam_doc_false" })
        {
            string source = name + ".erl";
            byte[] input = BeamModuleEmissionChecks.Read(source);
            byte[] originalImage = BeamModuleEmissionChecks.Read(name + ".beam");
            byte[] generatedImage = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(input), source);
            if (BeamOtpEtfCompression.HasPinnedCompressor && !generatedImage.AsSpan().SequenceEqual(originalImage))
            {
                throw new InvalidOperationException(Failure + " " + name);
            }
            var expected = Chunks(originalImage);
            var actual = Chunks(generatedImage);
            if (expected.Count != actual.Count)
            {
                throw new InvalidOperationException(Failure);
            }
            for (int index = 0; index < expected.Count; index++)
            {
                var reference = expected[index];
                var output = actual[index];
                bool equal = reference.Id == "Docs"
                    ? RawEtf(reference.Data).AsSpan().SequenceEqual(RawEtf(output.Data))
                    : reference.Data.AsSpan().SequenceEqual(output.Data);
                if (reference.Id != output.Id || !equal)
                {
                    throw new InvalidOperationException(Failure + " " + name + "/" + reference.Id);
                }
            }
        }

        const string equiv = "beam_docs_equiv_forms";
        byte[] expectedEquiv = Chunks(BeamModuleEmissionChecks.Read(equiv + ".beam")).Single(chunk => chunk.Id == "Docs").Data;
        string equivSource = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(equiv + ".erl"));
        byte[] actualEquiv = Chunks(CodeGeneration.CompileBeamModule(equivSource, equiv + ".erl")).Single(chunk => chunk.Id == "Docs").Data;
        if (!RawEtf(expectedEquiv).AsSpan().SequenceEqual(RawEtf(actualEquiv)))
        {
            throw new InvalidOperationException(Failure + " " + equiv);
        }

        CompareIncludedForms();
        CompareExternalDocumentation();

        return Task.CompletedTask;
    }

    private static void CompareIncludedForms()
    {
        const string name = "beam_docs_included_forms";
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-docs-include-");
        try
        {
            string source = Path.Combine(temporary.FullName, name + ".erl");
            File.WriteAllBytes(source, BeamModuleEmissionChecks.Read(name + ".erl"));
            File.WriteAllBytes(Path.Combine(temporary.FullName, name + ".hrl"), BeamModuleEmissionChecks.Read(name + ".hrl"));
            byte[] expected = Chunks(BeamModuleEmissionChecks.Read(name + ".beam")).Single(chunk => chunk.Id == "Docs").Data;
            byte[] actual = Chunks(CodeGeneration.CompileBeamFile(source)).Single(chunk => chunk.Id == "Docs").Data;
            bool equal = BeamOtpEtfCompression.HasPinnedCompressor
                ? expected.AsSpan().SequenceEqual(actual)
                : RawEtf(expected).AsSpan().SequenceEqual(RawEtf(actual));
            if (!equal)
            {
                throw new InvalidOperationException(Failure + " " + name);
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    private static void CompareExternalDocumentation()
    {
        const string name = "beam_external_doc";
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-external-doc-");
        try
        {
            string source = Path.Combine(temporary.FullName, name + ".erl");
            File.WriteAllBytes(source, BeamModuleEmissionChecks.Read(name + ".erl"));
            foreach (string document in new[] { "module.md", "function.md" })
            {
                File.WriteAllBytes(
                    Path.Combine(temporary.FullName, document),
                    BeamModuleEmissionChecks.Read(name + "_" + document)
                );
            }

            byte[] expected = Chunks(BeamModuleEmissionChecks.Read(name + ".beam")).Single(chunk => chunk.Id == "Docs").Data;
            byte[] actual = Chunks(CodeGeneration.CompileBeamFile(source)).Single(chunk => chunk.Id == "Docs").Data;
            if (!RawEtf(expected).AsSpan().SequenceEqual(RawEtf(actual)))
            {
                throw new InvalidOperationException(Failure + " " + name);
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    internal static List<(string Id, byte[] Data)> Chunks(byte[] beam)
    {
        var chunks = new List<(string Id, byte[] Data)>();
        for (int offset = 12; offset < beam.Length;)
        {
            string id = Encoding.ASCII.GetString(beam, offset, 4);
            int size = BinaryPrimitives.ReadInt32BigEndian(beam.AsSpan(offset + 4, 4));
            chunks.Add((id, beam.AsSpan(offset + 8, size).ToArray()));
            offset += 8 + ((size + 3) & ~3);
        }

        return chunks;
    }

    internal static byte[] RawEtf(byte[] packet)
    {
        if (packet[1] != CompressedEtfTag)
        {
            return packet;
        }
        using var source = new MemoryStream(packet, 6, packet.Length - 6);
        using var inflater = new ZLibStream(source, CompressionMode.Decompress);
        using var output = new MemoryStream();
        output.WriteByte(packet[0]);
        inflater.CopyTo(output);

        return output.ToArray();
    }
}
