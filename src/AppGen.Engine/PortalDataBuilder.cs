using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class PortalDataBuilder
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task BuildAsync(string portalRoot, PortalFeatures features, CancellationToken ct = default)
    {
        var dataDir = Path.Combine(portalRoot, "data");
        var jsDir = Path.Combine(portalRoot, "js");
        Directory.CreateDirectory(jsDir);

        var settingsPath = Path.Combine(dataDir, "portal-settings.json");
        var navPath = Path.Combine(dataDir, "nav.json");
        if (!File.Exists(settingsPath) || !File.Exists(navPath))
            throw new InvalidOperationException("portal/data/portal-settings.json and nav.json are required.");

        var settingsJson = (await File.ReadAllTextAsync(settingsPath, ct)).Trim();
        var navJson = (await File.ReadAllTextAsync(navPath, ct)).Trim();

        var sections = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var searchEntries = new List<SearchIndexEntry>();

        foreach (var file in Directory.GetFiles(dataDir, "*.json"))
        {
            var name = Path.GetFileName(file);
            if (name is "portal-settings.json" or "nav.json")
                continue;

            var text = await File.ReadAllTextAsync(file, ct);
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("id", out var idProp))
                continue;

            var id = idProp.GetString();
            if (string.IsNullOrWhiteSpace(id))
                continue;

            sections[id] = doc.RootElement.Clone();
            if (features.Search)
                searchEntries.AddRange(BuildSearchEntries(doc.RootElement, id));
        }

        var sectionsJs = BuildSectionsJsObject(sections);
        var portalDataJs =
            "window.DELTACORE_PORTAL = {\n" +
            $"  settings: {settingsJson},\n" +
            $"  nav: {navJson},\n" +
            $"  sections: {sectionsJs}\n" +
            "};\n";

        await File.WriteAllTextAsync(Path.Combine(jsDir, "portal-data.js"), portalDataJs, ct);

        if (features.Search)
        {
            var searchJson = JsonSerializer.Serialize(searchEntries, CompactOptions);
            await File.WriteAllTextAsync(
                Path.Combine(jsDir, "search-index.js"),
                $"window.DELTACORE_SEARCH_INDEX = {searchJson};\n",
                ct);
        }
    }

    private static IEnumerable<SearchIndexEntry> BuildSearchEntries(JsonElement doc, string sectionId)
    {
        var title = doc.TryGetProperty("title", out var t) ? t.GetString() ?? sectionId : sectionId;
        var status = doc.TryGetProperty("status", out var s) ? s.GetString() ?? "planned" : "planned";
        var tags = ReadStringArray(doc, "tags");

        yield return new SearchIndexEntry
        {
            Id = sectionId,
            Title = title,
            Section = title,
            Url = $"sections/{sectionId}.html",
            Text = BuildSearchText(doc),
            Tags = tags,
            Status = status
        };

        if (!doc.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var block in blocks.EnumerateArray())
        {
            var blockId = block.TryGetProperty("id", out var bid) ? bid.GetString() : null;
            var entryId = string.IsNullOrWhiteSpace(blockId) ? $"{sectionId}-block" : $"{sectionId}-{blockId}";
            var heading = block.TryGetProperty("heading", out var h) ? h.GetString() : null;
            yield return new SearchIndexEntry
            {
                Id = entryId,
                Title = heading ?? title,
                Section = title,
                Url = $"sections/{sectionId}.html#{blockId}",
                Text = BuildBlockSearchText(block),
                Tags = tags,
                Status = status
            };
        }
    }

    private static string BuildSearchText(JsonElement doc)
    {
        var parts = new List<string>();
        if (doc.TryGetProperty("title", out var t) && t.GetString() is { } title) parts.Add(title);
        if (doc.TryGetProperty("summary", out var s) && s.GetString() is { } summary) parts.Add(summary);
        parts.AddRange(ReadStringArray(doc, "searchKeywords"));
        parts.AddRange(ReadStringArray(doc, "tags"));
        if (doc.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in blocks.EnumerateArray())
                parts.Add(BuildBlockSearchText(block));
        }
        return string.Join(' ', parts);
    }

    private static string BuildBlockSearchText(JsonElement block)
    {
        var parts = new List<string>();
        if (block.TryGetProperty("heading", out var h) && h.GetString() is { } heading) parts.Add(heading);
        if (block.TryGetProperty("content", out var c) && c.GetString() is { } content) parts.Add(content);
        parts.AddRange(ReadStringArray(block, "bullets"));
        return string.Join(' ', parts);
    }

    private static List<string> ReadStringArray(JsonElement parent, string name)
    {
        var result = new List<string>();
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return result;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.GetString() is { } value)
                result.Add(value);
        }
        return result;
    }

    private static string BuildSectionsJsObject(Dictionary<string, JsonElement> sections)
    {
        var parts = sections
            .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => $"\"{kvp.Key}\":{JsonSerializer.Serialize(kvp.Value, CompactOptions)}");
        return "{" + string.Join(',', parts) + "}";
    }

    private sealed class SearchIndexEntry
    {
        public required string Id { get; init; }
        public required string Title { get; init; }
        public required string Section { get; init; }
        public required string Url { get; init; }
        public required string Text { get; init; }
        public List<string> Tags { get; init; } = [];
        public string Status { get; init; } = "planned";
    }
}

internal static class PortalSectionShell
{
    public static string Render(string sectionId, string title, PortalFeatures features)
    {
        var authBlock = features.PasswordGate
            ? "  <style>html:not(.auth-ok) body > :not(#auth-gate) { display: none; }</style>\n  <script src=\"../js/auth.js\"></script>\n"
            : string.Empty;
        var searchScripts = features.Search
            ? "  <script src=\"../js/vendor/fuse.min.js\"></script>\n  <script src=\"../js/search-index.js\"></script>\n  <script src=\"../js/search.js\"></script>\n"
            : string.Empty;

        var encodedTitle = System.Net.WebUtility.HtmlEncode(title);
        return "<!DOCTYPE html>\n" +
               "<html lang=\"en\">\n" +
               "<head>\n" +
               "  <meta charset=\"UTF-8\">\n" +
               "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n" +
               $"  <title>{encodedTitle}</title>\n" +
               "  <link rel=\"stylesheet\" href=\"../css/portal.css\">\n" +
               authBlock +
               "</head>\n" +
               $"<body data-nav-scope=\"section\" data-section-id=\"{sectionId}\">\n" +
               "  <div id=\"portal-toolbar\"></div>\n" +
               "  <div class=\"page\">\n" +
               "    <aside class=\"sidebar\" data-portal-sidebar></aside>\n" +
               "    <main class=\"main\" id=\"section-content\">\n" +
               "      <p class=\"loading\">Loading…</p>\n" +
               "    </main>\n" +
               "  </div>\n" +
               "  <footer class=\"site-footer no-print\">Generated by AppGen</footer>\n" +
               searchScripts +
               "  <script src=\"../js/portal-data.js\"></script>\n" +
               "  <script src=\"../js/portal.js\"></script>\n" +
               "</body>\n" +
               "</html>\n";
    }
}
