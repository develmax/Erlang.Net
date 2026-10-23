namespace Erlang;

internal static class ErlDriverOwnTermCodec455
{
    internal static Term Decode(byte[] bytes, ErlDriverOwnTermBinding455[] bindings,
        Func<ErlDriverOwnTermBinding455, Term> resolve)
    {
        int previous = -1;
        foreach (var binding in bindings)
        {
            if (binding.Occurrence <= previous || binding.Handle == 0)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            previous = binding.Occurrence;
        }
        int occurrence = 0, selected = 0;

        Term Resource(Term wire, bool actor)
        {
            int current = occurrence++;
            if (selected == bindings.Length || bindings[selected].Occurrence != current) return wire;
            var binding = bindings[selected++];
            if (binding.Actor != actor) throw new InvalidDataException(DriverParentWireNames422.Invalid);
            Term actual = resolve(binding);
            bool same = (wire, actual) switch
            {
                (Pid first, Pid second) => first.Id == second.Id,
                (PortTerm first, PortTerm second) => first.Id == second.Id,
                _ => false
            };
            if (!same) throw new InvalidDataException(DriverParentWireNames422.Invalid);

            return actual;
        }
        // Factories run before map construction: an own current value must not
        // collapse into a separately supplied foreign value with old spelling.
        EtfDecodedTerm decoded = ExternalTermFormat.DecodeOne(
            bytes,
            new EtfDecodeOptions
            {
                PidFactory = value => (Pid)Resource(value, true),
                PortFactory = value => (PortTerm)Resource(value, false)
            }
        );
        if (selected != bindings.Length || decoded.Used != bytes.Length)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return decoded.Term;
    }
}
