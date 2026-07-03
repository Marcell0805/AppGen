using AppGen.Core.Models;

namespace AppGen.Core.Themes;

public static class MobileThemeCatalog
{
    private static readonly IReadOnlyList<MobileThemeDefinition> All =
    [
        Def("appgen", "AppGen default", "Balanced business theme for general applications.",
            sidebar: "0xFF0F172A", sidebarAccent: "0xFF1E293B", accent: "0xFF1D4ED8", accentMuted: "0xFF3B82F6",
            background: "0xFFF8FAFC", surface: "0xFFFFFFFF", border: "0xFFE2E8F0",
            text: "0xFF0F172A", textMuted: "0xFF64748B", highlight: "0xFF60A5FA"),

        Def("portal", "Match documentation portal", "Uses documentation portal brand colors when available.",
            sidebar: "0xFF1B3A5C", sidebarAccent: "0xFF152E47", accent: "0xFF2E75B6", accentMuted: "0xFF5A9BD5",
            background: "0xFFE8F1F8", surface: "0xFFFFFFFF", border: "0xFFD4E3F0",
            text: "0xFF1B3A5C", textMuted: "0xFF5A6F82", highlight: "0xFFF28C28",
            hideWhenDocumentationDisabled: true),

        Def("cookbook", "Cookbook", "Huntress woodland palette — forest green, warm gold, and cream.",
            sidebar: "0xFF1A3D2E", sidebarAccent: "0xFF0F2A1F", accent: "0xFFC9A227", accentMuted: "0xFFE8D5A3",
            background: "0xFFF5F0E8", surface: "0xFFFFFFFF", border: "0xFFE8DFD0",
            text: "0xFF5C4A3A", textMuted: "0xFF8A9A8E", highlight: "0xFFC45C5C",
            onAccent: "0xFF1A3D2E", onSidebar: "0xFFFFFFFF", sidebarSelectedBackground: "0x2EC9A227",
            headingFont: "cormorantGaramond", bodyFont: "cormorantGaramond", buttonFont: "inter",
            cornerRadius: 12),

        Def("corporate", "Corporate", "Conservative enterprise palette for ERP, CRM, and HR apps.",
            sidebar: "0xFF1E293B", sidebarAccent: "0xFF0F172A", accent: "0xFF2563EB", accentMuted: "0xFF3B82F6",
            background: "0xFFF1F5F9", surface: "0xFFFFFFFF", border: "0xFFCBD5E1",
            text: "0xFF0F172A", textMuted: "0xFF64748B", highlight: "0xFF0EA5E9",
            cornerRadius: 6),

        Def("glass", "Modern Glass", "Soft gradients and translucent surfaces with a modern feel.",
            sidebar: "0xFF1E3A5F", sidebarAccent: "0xFF152A45", accent: "0xFF4F46E5", accentMuted: "0xFF818CF8",
            background: "0xFFEEF2FF", surface: "0xE6FFFFFF", border: "0x33FFFFFF",
            text: "0xFF1E293B", textMuted: "0xFF64748B", highlight: "0xFF8B5CF6",
            cornerRadius: 16),

        Def("material3", "Material 3", "M3-inspired colors on the AppGen drawer shell.",
            sidebar: "0xFF1C1B1F", sidebarAccent: "0xFF141316", accent: "0xFF6750A4", accentMuted: "0xFF9575CD",
            background: "0xFFFFFBFE", surface: "0xFFFFFBFE", border: "0xFFCAC4D0",
            text: "0xFF1C1B1F", textMuted: "0xFF49454F", highlight: "0xFF7D5260",
            cornerRadius: 12, minButtonHeight: 48),

        Def("ios", "Apple / iOS", "Minimal whitespace with restrained iOS-like typography.",
            sidebar: "0xFFF2F2F7", sidebarAccent: "0xFFE5E5EA", accent: "0xFF0056D6", accentMuted: "0xFF5AC8FA",
            background: "0xFFF2F2F7", surface: "0xFFFFFFFF", border: "0xFFC6C6C8",
            text: "0xFF000000", textMuted: "0xFF8E8E93", highlight: "0xFF5856D6",
            onSidebar: "0xFF000000", sidebarSelectedBackground: "0x1A007AFF",
            headingFont: "inter", cornerRadius: 10),

        Def("minimal", "Minimal", "Content-first monochrome design with thin lines.",
            sidebar: "0xFF111111", sidebarAccent: "0xFF000000", accent: "0xFF111111", accentMuted: "0xFF444444",
            background: "0xFFFFFFFF", surface: "0xFFFFFFFF", border: "0xFFE5E5E5",
            text: "0xFF111111", textMuted: "0xFF666666", highlight: "0xFF333333",
            cornerRadius: 4),

        Def("dark-pro", "Dark Pro", "Premium dark interface for developer and productivity apps.",
            sidebar: "0xFF0F1117", sidebarAccent: "0xFF080A0F", accent: "0xFF2563EB", accentMuted: "0xFF60A5FA",
            background: "0xFF0F1117", surface: "0xFF1A1D26", border: "0xFF2A2F3A",
            text: "0xFFF8FAFC", textMuted: "0xFF94A3B8", highlight: "0xFF38BDF8",
            isDark: true),

        Def("dashboard", "Dashboard", "KPI tiles and data-forward layout for operational apps.",
            sidebar: "0xFF0F172A", sidebarAccent: "0xFF020617", accent: "0xFF0F766E", accentMuted: "0xFF14B8A6",
            background: "0xFFF0FDFA", surface: "0xFFFFFFFF", border: "0xFFCCFBF1",
            text: "0xFF134E4A", textMuted: "0xFF5F6B6B", highlight: "0xFF0891B2",
            usesDashboardNav: true, cornerRadius: 10),

        Def("nature", "Nature", "Organic earth tones for wellness, food, and travel.",
            sidebar: "0xFF2D4A3E", sidebarAccent: "0xFF1F3329", accent: "0xFF3F5F47", accentMuted: "0xFF6B8F71",
            background: "0xFFF5F1E8", surface: "0xFFFFFFFF", border: "0xFFD8E2D0",
            text: "0xFF2D4A3E", textMuted: "0xFF6B7F6E", highlight: "0xFFB8860B",
            headingFont: "cormorantGaramond", onAccent: "0xFFFFFFFF"),

        Def("luxury", "Luxury", "Black, gold, and ivory high-end presentation.",
            sidebar: "0xFF0A0A0A", sidebarAccent: "0xFF000000", accent: "0xFFC9A227", accentMuted: "0xFFE0BE5A",
            background: "0xFFFAF7F0", surface: "0xFFFFFFFF", border: "0xFFE8DFD0",
            text: "0xFF1A1A1A", textMuted: "0xFF6B5E4E", highlight: "0xFFC9A227",
            headingFont: "playfairDisplay", onAccent: "0xFF1A1A1A", sidebarSelectedBackground: "0x33C9A227"),

        Def("medical", "Medical", "Accessible healthcare palette with clear status colors.",
            sidebar: "0xFF0C4A6E", sidebarAccent: "0xFF082F49", accent: "0xFF0369A1", accentMuted: "0xFF38BDF8",
            background: "0xFFF0F9FF", surface: "0xFFFFFFFF", border: "0xFFBAE6FD",
            text: "0xFF0C4A6E", textMuted: "0xFF475569", highlight: "0xFF16A34A",
            minButtonHeight: 48, cornerRadius: 8),

        Def("education", "Education", "Friendly learning palette with soft blues and purples.",
            sidebar: "0xFF312E81", sidebarAccent: "0xFF1E1B4B", accent: "0xFF4F46E5", accentMuted: "0xFFA5B4FC",
            background: "0xFFEEF2FF", surface: "0xFFFFFFFF", border: "0xFFC7D2FE",
            text: "0xFF312E81", textMuted: "0xFF6366F1", highlight: "0xFFF59E0B",
            cornerRadius: 12),

        Def("gaming", "Gaming", "Bold dark UI with neon accents.",
            sidebar: "0xFF0B0B12", sidebarAccent: "0xFF050508", accent: "0xFF22D3EE", accentMuted: "0xFF67E8F9",
            background: "0xFF0B0B12", surface: "0xFF151522", border: "0xFF2E2E45",
            text: "0xFFF8FAFC", textMuted: "0xFF94A3B8", highlight: "0xFFE879F9",
            isDark: true, cornerRadius: 6),

        Def("terminal", "Terminal", "Developer console aesthetic with monospace accents.",
            sidebar: "0xFF000000", sidebarAccent: "0xFF0A0A0A", accent: "0xFF22C55E", accentMuted: "0xFF4ADE80",
            background: "0xFF000000", surface: "0xFF0A0A0A", border: "0xFF1F2937",
            text: "0xFF22C55E", textMuted: "0xFF86EFAC", highlight: "0xFF4ADE80",
            headingFont: "jetBrainsMono", bodyFont: "jetBrainsMono", buttonFont: "inter",
            isDark: true, cornerRadius: 2, onAccent: "0xFF000000"),
    ];

    private static readonly Dictionary<string, MobileThemeDefinition> ById =
        All.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<MobileThemeDefinition> GetAll() => All;

    public static IReadOnlyList<MobileThemeDefinition> GetSelectable(bool documentationEnabled) =>
        All.Where(d => !d.HideWhenDocumentationDisabled || documentationEnabled).ToList();

    public static MobileThemeDefinition Get(string? presetId) =>
        ById.TryGetValue(NormalizePreset(presetId), out var def) ? def : ById["appgen"];

    public static string NormalizePreset(string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return "appgen";

        var id = presetId.Trim().ToLowerInvariant();
        return ById.ContainsKey(id) ? id : "appgen";
    }

    public static PortalThemeSettings ToPortalThemeSettings(string? presetId)
    {
        var definition = Get(NormalizePreset(presetId));
        return new PortalThemeSettings
        {
            PrimaryColor = ThemeColorHelper.DartToCssHex(definition.Sidebar),
            AccentColor = ThemeColorHelper.DartToCssHex(definition.Accent),
            HighlightColor = ThemeColorHelper.DartToCssHex(definition.Highlight),
            BackgroundColor = ThemeColorHelper.DartToCssHex(definition.Background)
        };
    }

    public static PortalSettings WithAppTheme(PortalSettings settings, string? presetId) =>
        new()
        {
            PortalName = settings.PortalName,
            Tagline = settings.Tagline,
            Version = settings.Version,
            HomeQuote = settings.HomeQuote,
            MaintainerDocsUrl = settings.MaintainerDocsUrl,
            ProductRepoUrl = settings.ProductRepoUrl,
            Auth = settings.Auth,
            Theme = ToPortalThemeSettings(presetId)
        };

    public static string[] PresetIds => All.Select(d => d.Id).ToArray();

    private static MobileThemeDefinition Def(
        string id,
        string displayName,
        string description,
        string sidebar,
        string sidebarAccent,
        string accent,
        string accentMuted,
        string background,
        string surface,
        string border,
        string text,
        string textMuted,
        string highlight,
        string success = "0xFF16A34A",
        string error = "0xFFDC2626",
        string? onAccent = null,
        string? onSidebar = null,
        string? sidebarSelectedBackground = null,
        string headingFont = "inter",
        string bodyFont = "inter",
        string buttonFont = "inter",
        double cornerRadius = 8,
        bool isDark = false,
        double minButtonHeight = 44,
        bool usesDashboardNav = false,
        bool hideWhenDocumentationDisabled = false) =>
        new(
            id,
            displayName,
            description,
            sidebar,
            sidebarAccent,
            accent,
            accentMuted,
            background,
            surface,
            border,
            text,
            textMuted,
            highlight,
            success,
            error,
            onAccent,
            onSidebar,
            sidebarSelectedBackground,
            headingFont,
            bodyFont,
            buttonFont,
            cornerRadius,
            isDark,
            minButtonHeight,
            usesDashboardNav,
            hideWhenDocumentationDisabled);
}
