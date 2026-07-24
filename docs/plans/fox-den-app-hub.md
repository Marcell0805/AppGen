# Fox's Den app distribution hub

AppGen Flutter apps can publish APKs and **`mobile-version.json`** to [The Fox's Den Doc](https://github.com/Marcell0805/the-foxs-den-doc) (GitHub Pages) instead of a per-app static site.

## appgen.json

```json
"targets": {
  "mobile": {
    "publish": {
      "baseUrl": "https://marcell0805.github.io/the-foxs-den-doc",
      "appId": "active-huntress",
      "apkFileName": "active-huntress.apk",
      "portalRepoPath": "D:\\repos\\The_Fox_s_Den Doc"
    }
  }
}
```

When **`baseUrl`** is set, generated mobile projects include:

- `assets/mobile_config.json` with `updateCheckUrl`
- `UpdateService` + launch-time update dialog
- `scripts/publish-mobile.ps1` delegates to `{portalRepoPath}/portal/scripts/publish-app-mobile.ps1` when that file exists

## Maintainer flow

1. Add the app to `portal/data/apps-manifest.json` in The Fox's Den Doc repo.
2. From the mobile project: `.\scripts\publish-mobile.ps1 -ReleaseNotes "…"`
3. In Fox's Den Doc: `portal\scripts\build-portal.ps1`, commit, push.

Bump **`version: x.y.z+N`** in `pubspec.yaml` before each publish (`N` = Android build number).
