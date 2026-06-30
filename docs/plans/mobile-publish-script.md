# Plan: Generated Mobile Publish Script

> **Status:** Implemented (2026-06-23)  
> **Scope:** Generic AppGen — every generated `{AppName} Mobile` project.

## Goal

Ship a runnable `scripts/publish-mobile.ps1` with each Flutter mobile output so developers can build a release APK, copy artifacts to `dist/`, and write `mobile-version.json` in one command — mirroring Portal’s `scripts/build-portal.ps1`.

## What was built

| Item | Location |
|------|----------|
| Publish settings on manifest | `targets.mobile.publish.baseUrl`, `targets.mobile.publish.apkFileName` in [`ApplicationTargets.cs`](../../src/AppGen.Core/Models/ApplicationTargets.cs) |
| Script template | [`Templates/Mobile/flutter/scripts/publish-mobile.ps1.scriban`](../../src/AppGen.Templates/Templates/Mobile/flutter/scripts/publish-mobile.ps1.scriban) |
| Generation | [`FlutterGenerator.cs`](../../src/AppGen.Engine/FlutterGenerator.cs) emits `scripts/publish-mobile.ps1` |
| Mobile README | [`ReadmeGenerator.WriteMobileAsync`](../../src/AppGen.Engine/ReadmeGenerator.cs) — **Publish release APK** section |
| Tests | [`MobileGenerationServiceTests.GenerateAsync_emits_publish_mobile_script_with_defaults`](../../src/AppGen.Tests/MobileGenerationServiceTests.cs) |

## Usage

```powershell
cd "output\MyApp Mobile"
.\scripts\publish-mobile.ps1
.\scripts\publish-mobile.ps1 -PagesBaseUrl "https://example.github.io/myapp" -ReleaseNotes "Bug fixes"
.\scripts\publish-mobile.ps1 -SkipBuild   # copy only, APK already built
```

Optional `appgen.json`:

```json
"mobile": {
  "publish": {
    "baseUrl": "https://example.github.io/myapp",
    "apkFileName": "myapp.apk"
  }
}
```

Artifacts land in `{AppName} Mobile/dist/` (`*.apk` + `mobile-version.json`). Upload `dist/` to static hosting when ready.

## Script behaviour

1. `flutter pub get` + `flutter build apk --release` (unless `-SkipBuild`)
2. Copy `build/app/outputs/flutter-apk/app-release.apk` → `dist/{apkFileName}`
3. Parse `pubspec.yaml` version (`1.0.0+1`)
4. Write `dist/mobile-version.json` with `version`, `build`, `apkUrl`, `releaseNotes`
5. Warn if APK appears debug-signed (`jarsigner` check, best-effort)

## Relationship to Huntress Cookbook

The **huntress-cookbook** repo has a domain-specific `scripts/publish-mobile.ps1` that copies to `downloads/` and runs `export-mobile-seed.ps1`. AppGen’s generated script is **generic** (no seed export, default output `dist/`).

## Out of scope (follow-ups)

- AppGen UI **Publish** button invoking the script
- In-app update checker in generated Flutter templates (Huntress mobile has custom `UpdateService`)
- Doc-layer `downloads/` co-location (override via `-DownloadsDir` if needed later)
