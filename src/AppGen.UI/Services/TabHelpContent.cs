namespace AppGen.UI.Services;

public static class TabHelpContent
{
    public static string GetTitle(string tabId) => tabId switch
    {
        "project" => "How to use Project",
        "documentation" => "How to use Documentation",
        "web" => "How to use Web",
        "mobile" => "How to use Mobile",
        _ => "Help"
    };

    public static string GetBodyHtml(string tabId) => tabId switch
    {
        "project" => """
            <div class="help-body">
            <section class="help-section"><h3>What happens here</h3><ul><li>Define your application once: name, description, entities, and which layers to generate. AppGen writes a shared manifest and optional Documentation, Web, and Mobile outputs.</li></ul></section>
            <section class="help-section"><h3>What you can configure</h3><ul><li>App name and output folder</li><li>Tagline and description (used in READMEs and Documentation vision)</li><li>Layer toggles: Documentation, Web, Mobile</li><li>Web: enable JWT API authentication</li><li>Mobile: offline SQLite cache, theme preset, and <strong>Capabilities</strong> (camera, GPS, notifications, share, etc.)</li><li>Entities and properties (shared across all layers)</li></ul></section>
            <section class="help-section"><h3>Typical workflow</h3><ul><li>1. Fill in Application and About this app.</li><li>2. Add entities and properties.</li><li>3. Enable the layers you need.</li><li>4. Click Generate all (or Save manifest to persist without regenerating code).</li></ul></section>
            <section class="help-section"><h3>Common issues</h3><ul><li>Save manifest is disabled until you add at least one entity.</li><li>Each layer outputs to its own folder: {AppName}, {AppName} Doc, {AppName} Web, {AppName} Mobile.</li><li>Use Load project to reload appgen.json from the hub folder.</li></ul></section>
            </div>
            """,
        "documentation" => """
            <div class="help-body">
            <section class="help-section"><h3>What happens here</h3><ul><li>Generates a static documentation portal under portal/ — vision, roadmap, entities, and shareable HTML.</li></ul></section>
            <section class="help-section"><h3>What you can configure</h3><ul><li>Portal title, tagline, and home quote</li><li>Sections and content blocks</li><li>Entity sketches (names and descriptions for the portal)</li><li>Password gate and search</li></ul></section>
            <section class="help-section"><h3>Typical workflow</h3><ul><li>1. Configure site settings and sections (or use Project → Generate all).</li><li>2. Click Generate portal.</li><li>3. Preview portal/index.html with Live Server.</li><li>4. Edit portal/data/*.json locally, then Import from portal folder.</li></ul></section>
            <section class="help-section"><h3>Workbook import</h3><ul><li>Import the spec workbook on the <strong>Project</strong> tab — sections are written to the hub <code>appgen.json</code> (folder name matches <strong>ApplicationName</strong> on the Application sheet).</li><li>Open Documentation after import, or click <strong>Load from manifest</strong>. Do not use <strong>Import from portal folder</strong> unless you edited generated portal JSON files.</li></ul></section>
            <section class="help-section"><h3>Common issues</h3><ul><li>Blank page when opening file:// — use Live Server or another HTTP server.</li><li>Default password is the lowercased app name.</li><li>Stale sections usually mean Documentation read an old <code>{AppName} Doc</code> manifest — reload from the hub manifest instead.</li></ul></section>
            </div>
            """,
        "web" => """
            <div class="help-body">
            <section class="help-section"><h3>What happens here</h3><ul><li>Scaffolds the .NET solution: API, application layers, persistence, SQL scripts, and optional MVC admin UI.</li></ul></section>
            <section class="help-section"><h3>What you can configure</h3><ul><li>Database provider and connection strings</li><li>Entities and properties (prefer the Project tab for shared editing)</li><li>MVC Web checkbox for CRUD pages</li><li>JWT authentication (Project tab) — protects API endpoints and enables login tests</li><li>Generated xUnit tests use EF InMemory when ASPNETCORE_ENVIRONMENT=Testing — run dotnet test without a live database</li></ul></section>
            <section class="help-section"><h3>Typical workflow</h3><ul><li>1. Define entities on Project (or here).</li><li>2. Set database and connection strings.</li><li>3. Click Generate.</li><li>4. cd to output folder, dotnet build, dotnet test, then run API (and MVC if enabled).</li></ul></section>
            <section class="help-section"><h3>Common issues</h3><ul><li>Connection failed — check appsettings and Database:ActiveConnection.</li><li>Port in use — change launchSettings.json URLs.</li><li>Mobile needs this API running before Flutter can load data.</li></ul></section>
            </div>
            """,
        "mobile" => """
            <div class="help-body">
            <section class="help-section"><h3>What happens here</h3><ul><li>Generates a Flutter mobile client in the <code>{AppName} Mobile/</code> folder with API client, routes, and CRUD screens per entity.</li></ul></section>
            <section class="help-section"><h3>Capabilities</h3><ul><li>Each capability shows read-only <strong>Chrome</strong> / <strong>Android</strong> ticks on the Project tab.</li><li>Native plugins (camera, NFC, Bluetooth, ML Kit, etc.) need an Android emulator — Chrome cannot compile them.</li><li>Clipboard, share, place search, and WiFi are web-friendly.</li></ul></section>
            <section class="help-section"><h3>Typical workflow</h3><ul><li>1. Define entities on Project and enable Mobile (or generate here).</li><li>2. Start the Web API first.</li><li>3. Generate mobile.</li><li>4. Open the <code>{AppName} Mobile</code> folder, then <code>flutter pub get</code> and <code>flutter run</code>.</li></ul></section>
            <section class="help-section"><h3>Common issues</h3><ul><li>Connection refused — start the Web API; confirm apiBaseUrl matches Swagger.</li><li>Android emulator — use http://10.0.2.2:&lt;port&gt; instead of localhost.</li><li>Empty list — verify the API returns data and CORS is configured.</li><li>HTTPS errors — use HTTP in dev or trust the development certificate.</li></ul></section>
            </div>
            """,
        _ => "<p>No help content is available for this tab.</p>"
    };
}
