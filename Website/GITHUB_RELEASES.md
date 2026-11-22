# Publishing FrogRunner via GitHub Releases

This guide walks you through publishing test builds so players can download them from `frogrunner.com`.

## 1. Create a GitHub Repository
1. Go to https://github.com/new
2. Name it `FrogRunner` (or any name you like)
3. Set visibility to **Public** (required for free GitHub Releases downloads)
4. Click **Create repository**

## 2. Draft a New Release
1. Go to your repo → **Releases** → **Draft a new release**
2. Click **Choose a tag** → type a new tag, e.g. `v0.1.0-test` → **Create new tag on publish**
3. Fill in:
   - **Release title:** `FrogRunner v0.1.0 Test Build`
   - **Description:** paste your patch notes / known bugs
4. **Attach binaries:**
   - Drag & drop `FrogRunner-Android.apk` (from `Builds/` or `Website/downloads/`)
   - Drag & drop `FrogRunner-Windows.zip`
5. Click **Publish release**

## 3. Copy Direct Download URLs
After publishing, each asset has a direct download link. Right-click the asset name → **Copy link address**.

The URL format is:
```
https://github.com/<your-username>/<repo>/releases/download/<tag>/FrogRunner-Android.apk
https://github.com/<your-username>/<repo>/releases/download/<tag>/FrogRunner-Windows.zip
```

## 4. Paste URLs into Website Config
Open `Website/script.js` and edit the `RELEASE_CONFIG` block at the top:

```javascript
const RELEASE_CONFIG = {
  releaseTag: "v0.1.0-test",                          // ← your tag
  releaseNotes: "First test build — ...",              // ← your notes
  androidApkUrl: "https://github.com/.../FrogRunner-Android.apk",  // ← paste here
  windowsZipUrl: "https://github.com/.../FrogRunner-Windows.zip",  // ← paste here
  configured: true                                    // ← set to true!
};
```

## 5. Deploy the Website
- **Netlify:** drag & drop `Website/` folder at app.netlify.com/drop
- **GitHub Pages:** push `Website/` to a repo, enable Pages
- **Cloudflare Pages:** connect repo or upload

Then point `frogrunner.com` DNS to your host.

## Optional: GitHub Actions Auto-Release
Create `.github/workflows/release.yml` in your repo:

```yaml
name: Build & Release
on:
  push:
    tags:
      - 'v*'
jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Create Release
        uses: softprops/action-gh-release@v2
        with:
          files: |
            Builds/FrogRunner-Android.apk
            Builds/FrogRunner-Windows.zip
          body: "FrogRunner test build"
```

Note: This requires you to build the APK/ZIP locally and commit them, or set up Unity build automation (advanced).

## Sideload Install Notes (for players)
When installing the APK on Android:
1. Download the `.apk` file.
2. If prompted, allow "Install unknown apps" for your browser/file manager.
3. Open the APK → tap **Install**.
4. If you see "Not verified" warning: tap **"I acknowledge that this app may pose risks"** to enable the Install button.
5. Tap **Open** to launch FrogRunner.
