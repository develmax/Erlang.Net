namespace Erlang;

internal static partial class PrimFileComposition410
{
    private static bool Candidate(int point)
    {
        if (point < CandidateMinimum) return false;
        if (point > CandidateMaximum) return point is JapaneseVoicing or JapaneseSemivoicing;

        return (CandidateMap[point / CandidateWordBits - CandidateMapOffset] & (1U << (point % CandidateWordBits))) != 0;
    }

    private static int Translate(List<int> saved, int offset, out int value)
    {
        int table = Root;
        value = 0;
        for (int position = 0; position < saved.Count - offset; position++)
        {
            PrimFileCompositionEntry410? found = null;
            foreach (var entry in Tables[table])
                if (entry.Codepoint == saved[offset + position])
                {
                    found = entry;
                    break;
                }
            if (found is not { } node) return PrimFileEncodingNames410.TranslateNo;
            if (node.Result != 0)
            {
                value = node.Result;

                return position;
            }
            table = node.Children;
        }

        return PrimFileEncodingNames410.TranslateMaybe;
    }

    internal static async ValueTask<Term> Normalize(PrimFileEncodingWork410 work, IReadOnlyList<int> points)
    {
        Term result = Nil.Value;
        var saved = new List<int>();

        void Emit(int point) => result = new Cons(Term.I(point), result);

        void Flush()
        {
            Emit(saved[0]);
            for (int index = 1; index < saved.Count;)
            {
                int count = Candidate(saved[index]) ? Translate(saved, index, out _) : PrimFileEncodingNames410.TranslateNo;
                if (count <= 0) Emit(saved[index++]);
                else
                {
                    _ = Translate(saved, index, out int point);
                    Emit(point);
                    index += count;
                }
            }
            saved.Clear();
        }
        for (int cursor = points.Count - 1; cursor >= 0; cursor--)
        {
            await work.Tick();
            int point = points[cursor];
            if (saved.Count == 0)
            {
                if (Candidate(point)) saved.Add(point);
                else Emit(point);
                continue;
            }
            if (point > PrimFileEncodingNames410.WordMaximum)
            {
                Flush();
                Emit(point);
                continue;
            }
            saved.Add(point);
            int translated = Translate(saved, 0, out int combined);
            if (translated > 0)
            {
                saved.Clear();
                Emit(combined);
            }
            else if (translated == PrimFileEncodingNames410.TranslateNo)
            {
                Emit(saved[0]);
                int index = 1;
                for (; index < saved.Count;)
                {
                    int count = Candidate(saved[index]) ? Translate(saved, index, out _) : PrimFileEncodingNames410.TranslateNo;
                    if (count == 0) Emit(saved[index++]);
                    else if (count > 0)
                    {
                        _ = Translate(saved, index, out int replacement);
                        Emit(replacement);
                        index += count;
                    }
                    else break;
                }
                if (index == saved.Count) saved.Clear();
                else saved.RemoveRange(0, index);
            }
        }
        if (saved.Count != 0) Flush();

        return result;
    }
}
