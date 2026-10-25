using System.Security.Cryptography;
using System.Text.Json;
using static CatalogValidationNames;

internal static class CatalogInventory
{
    internal static CatalogImage[] Read(string repository)
    {
        var images = new List<CatalogImage>();
        string[] applications = Directory.GetDirectories(
            Path.Combine(
                repository,
                Source,
                Otp,
                Original
            ),
            Libraries,
            SearchOption.AllDirectories
        );
        if (applications.Length != ApplicationsCount)
            throw new InvalidDataException(Failure);
        foreach (string directory in applications.Order(StringComparer.Ordinal))
        {
            using JsonDocument catalog = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, Catalog)));
            JsonElement value = catalog.RootElement;
            var entries = value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray().Select(row => (row.GetProperty(Name).GetString()!, row.GetProperty(Sha256).GetString()!))
                : value.GetProperty(Images).EnumerateObject().Select(row => (row.Name, row.Value.GetString()!));
            foreach (var (module, hash) in entries)
            {
                string path = Directory.GetFiles(Path.Combine(directory, Otp), module + Beam, SearchOption.AllDirectories).Single();
                if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) != hash)
                    throw new InvalidDataException(Failure);
                images.Add(new(module, path, hash));
            }
        }
        int total = ImagesCount + SupplementaryImages.Length;
        if (images.Count != total || images.Select(image => image.Module).Distinct(StringComparer.Ordinal).Count() != total
            || SupplementaryImages.Any(name => !images.Any(image => image.Module == name)))
            throw new InvalidDataException(Failure);

        return images.Where(image => !SupplementaryImages.Contains(image.Module, StringComparer.Ordinal)).ToArray();
    }
}
