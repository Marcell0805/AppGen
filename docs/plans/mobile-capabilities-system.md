# Plan: Mobile Capabilities System for AppGen

> **Status:** Implemented (schema v8)  
> **Last updated:** 2026-06-23

## 1. Goal

Introduce a **Capabilities** model in `appgen.json` so developers describe what a mobile app should do (camera, GPS, notifications, etc.) instead of manually adding Flutter packages, platform permissions, and service boilerplate. AppGen's Flutter generator translates enabled capabilities into `pubspec.yaml` dependencies, `lib/core/services/*` classes, Riverpod registration, and Android/iOS permission/config patches.

The design must be **framework-agnostic in the manifest** so future generators (MAUI, React Native) can consume the same capability IDs; only the Flutter emitter ships in the first slice.

## 2. Context (what shipped)

- **Schema v8** — `targets.mobile.capabilities.enabled` as `string[]` on [`MobileTargetSpec`](../../src/AppGen.Core/Models/ApplicationTargets.cs)
- **Catalog** — `AppGen.Core/Capabilities/MobileCapabilityCatalog.cs` with pilot implementations (camera, GPS, biometrics, local notifications, share, clipboard, vibration, maps, etc.) and stubs for future IDs
- **Resolver** — `MobileCapabilityResolver` with dependency closure; legacy mapping from `offline.enabled` → `offlineCache` and Web JWT → `secureStorage` / `jwtAuth`
- **Emitter** — `FlutterCapabilityEmitter` — pubspec merge, `lib/core/services/*` templates, `capabilities_provider.dart`
- **Platform patcher** — `FlutterPlatformConfigPatcher` merges Android/iOS permission snippets after `FlutterPlatformScaffolder`
- **UI** — capability checkboxes on Project tab; summary on Mobile tab
- **Tests** — `MobileCapabilitiesTests`, `AuthAndOfflineTests`, `MobileGenerationServiceTests`

## 3. Proposed approach

Add **schema v8** with `targets.mobile.capabilities` — a list of enabled capability IDs grouped by category in the UI but stored as a flat `string[]` (or `Dictionary<string,bool>`) for simplicity and forward compatibility.

Introduce a **capability catalog** in `AppGen.Core` (`MobileCapabilityCatalog`) defining each capability's metadata: category, display name, Flutter pub packages, Android manifest permissions/uses-feature, iOS Info.plist keys, optional `dependsOn` IDs, and template paths. The engine's new `FlutterCapabilityEmitter` resolves enabled capabilities (including transitive dependencies), merges pubspec dependencies, emits only the matching Scriban service templates into `lib/core/services/`, and produces a `capabilities_provider.dart` that registers Riverpod providers for enabled services.

**Platform permissions** are applied **after** `FlutterPlatformScaffolder.ScaffoldAsync` via a new `FlutterPlatformConfigPatcher` that merges XML/plist fragments from `Templates/Mobile/flutter/platform/` into the generated `android/app/src/main/AndroidManifest.xml` and `ios/Runner/Info.plist` (only when those folders exist). If Flutter SDK is missing and platforms were not scaffolded, emit a `mobile/flutter/CAPABILITIES.md` checklist as fallback (same pattern as existing scaffold skip message).

**Thin-slice v1** implements catalog entries + full Flutter emission for a pilot set: `internet` (documented baseline — `dio` already present), `camera`, `gps`, `biometrics`, `notifications` (local only), `share`, `clipboard`, `vibration`. Remaining proposal categories (Bluetooth, NFC, OCR, Maps, etc.) are registered in the catalog as **metadata-only / coming soon** in UI or hidden until templates exist.

**Migrate existing toggles** without breaking v7 manifests: `targets.mobile.offline.enabled` maps to capability `offlineCache`; `targets.web.auth.enabled` continues to drive JWT/login templates but also auto-enables capabilities `secureStorage` and `jwtAuth` in the resolver (dual-read during transition). Long-term, Project UI can replace separate offline/auth checkboxes with capabilities, but v1 keeps them for backward compatibility.

### Alternatives considered

- **Per-capability YAML files on disk** — flexible for non-devs, but repo already uses embedded Scriban + C# models; YAML adds a second loader. Rejected for v1; catalog stays in C# with optional JSON export later.
- **Generate only Dart, skip platform files** — simpler but fails the proposal's core value (manual manifest/plist edits). Rejected; patcher is required for pilot capabilities.
- **Implement all 25+ capabilities in one pass** — high risk, untestable surface. Rejected; phased catalog with pilot templates.

## 4. Affected files

| File | Change type | Notes |
|------|-------------|-------|
| `AppGen.Core/Models/ApplicationTargets.cs` | edit | Add `MobileCapabilitiesSpec` on `MobileTargetSpec` |
| `AppGen.Core/Models/SolutionSpec.cs` | edit | Bump `CurrentSchemaVersion` to **8** |
| `AppGen.Core/Capabilities/MobileCapabilityId.cs` | new | String constants / enum for capability IDs |
| `AppGen.Core/Capabilities/MobileCapabilityDefinition.cs` | new | Catalog record type |
| `AppGen.Core/Capabilities/MobileCapabilityCatalog.cs` | new | All capability metadata (pilot + stubs) |
| `AppGen.Engine/MobileCapabilityResolver.cs` | new | Enabled set + dependency closure + legacy mapping |
| `AppGen.Engine/FlutterCapabilityEmitter.cs` | new | Pubspec merge, service emission, provider registry |
| `AppGen.Engine/FlutterPlatformConfigPatcher.cs` | new | Android/iOS permission merges post-scaffold |
| `AppGen.Engine/FlutterGenerator.cs` | edit | Call emitter; pass `capabilities` into Scriban models; preserve on write-back |
| `AppGen.Engine/MobileApplicationGenerator.cs` | edit | Invoke patcher after scaffold |
| `AppGen.Engine/TargetFlags.cs` | edit | `HasCapability(spec, id)` helper |
| `AppGen.Engine/SpecNormalizer.cs` | edit | v7→v8 migration helpers |
| `AppGen.Engine/ReadmeGenerator.cs` | edit | List enabled capabilities in mobile README |
| `AppGen.Templates/Templates/Mobile/flutter/pubspec.yaml.scriban` | edit | Loop / include merged capability dependencies |
| `AppGen.Templates/Templates/Mobile/flutter/capabilities_provider.dart.scriban` | new | Riverpod exports for enabled services |
| `AppGen.Templates/Templates/Mobile/flutter/services/*.dart.scriban` | new | Pilot service templates (camera, location, etc.) |
| `AppGen.Templates/Templates/Mobile/flutter/platform/android_permissions.xml.scriban` | new | Merge fragments per capability |
| `AppGen.Templates/Templates/Mobile/flutter/platform/ios_info_plist.snippet.scriban` | new | Plist key fragments |
| `AppGen.UI/Models/WizardDraft.cs` | edit | `HashSet<string> MobileCapabilities` or category fields |
| `AppGen.UI/Services/WizardStateService.cs` | edit | Map capabilities ↔ manifest |
| `AppGen.UI/Components/ProjectWorkspace.razor` | edit | Categorized capability checkboxes under Mobile |
| `AppGen.UI/Pages/Mobile.razor` | edit | Read-only summary or subset editor |
| `AppGen.UI/Services/TabHelpContent.cs` | edit | Capabilities help section |
| `AppGen.Tests/MobileCapabilitiesTests.cs` | new | Schema round-trip, resolver, generation asserts |
| `docs/EVOLUTION_ROADMAP.md` | edit | Phase 5 / capabilities status |

## 5. Implementation steps — completed

All v1 steps from the original plan are done. See git history and `MobileCapabilitiesTests` for coverage.

**Not in v1 (deferred):**

- FCM / `pushNotifications` capability
- Per-capability demo screens/routes beyond service stubs
- MAUI / React Native emitters
- Incremental capability-only regen (see EVOLUTION_ROADMAP long-term sync phase)

## 6. Risks & mitigations

- **Risk:** `flutter create` not run (no Flutter SDK) → no `android/` / `ios/` folders to patch  
  **Mitigation:** Emit `CAPABILITIES.md` with manual steps; tests assert Dart layer only when scaffold skipped

- **Risk:** Duplicate or conflicting `AndroidManifest` permissions when regenerating  
  **Mitigation:** Patcher uses namespaced XML comments `<!-- AppGen:capability:camera -->` for idempotent merge/remove

- **Risk:** Catalog sprawl (25+ capabilities) before templates exist  
  **Mitigation:** `IsImplemented` flag; UI disables unimplemented entries with tooltip

- **Risk:** Breaking v7 projects  
  **Mitigation:** `SpecNormalizer` default empty capabilities; legacy offline/auth toggles keep working via resolver mapping

- **Risk:** Firebase / push notifications complexity  
  **Mitigation:** v1 `notifications` = `flutter_local_notifications` only (**confirmed**); FCM deferred to v2 capability `pushNotifications`

## 7. Testing strategy

- Unit tests: `MobileCapabilityResolver` dependency closure, legacy mapping, unknown ID ignored
- Unit tests: `FlutterPlatformConfigPatcher` XML/plist merge on fixture files in `AppGen.Tests/TestData/`
- Integration tests: `MobileCapabilitiesTests` generate temp project with `camera` + `gps` enabled → assert `camera_service.dart`, `location_service.dart`, `pubspec.yaml` contains `image_picker` / `geolocator`, patched manifest contains `CAMERA` / `ACCESS_FINE_LOCATION`
- Regression: existing `AuthAndOfflineTests`, `MobileUiShellTests` still pass
- Manual: enable capabilities in Project UI → Generate mobile → `flutter pub get` → run on Android emulator with permission prompts

## 8. Resolved questions

- **Internet:** implicit via `dio` (catalog entry for documentation)
- **Notifications:** local-only (`flutter_local_notifications`) in v1; FCM deferred
- **Capability UI:** editable on Project tab; Mobile tab shows summary
- **JWT auth:** auto-enables secure storage + login templates via resolver (not a separate locked checkbox)
- **iOS:** plist patching implemented alongside Android when platforms are scaffolded

## 9. Out of scope

- .NET MAUI and React Native capability emitters (manifest-only preparation)
- Combined feature flows (Camera + OCR screens, Maps UI, auth + biometric login screen beyond existing JWT login)
- Firebase setup, `google-services.json`, APNs certificates
- Bluetooth, NFC, WiFi, geofencing, ML Kit / barcode / face detection templates (catalog stub only)
- Per-capability sample screens/routes in v1 (services + permissions only)
- Incremental capability sync on regen (Phase 5 sync work)
- App Store / Play Store deployment configuration
