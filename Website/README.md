# FrogRunner Download Site (frogrunner.com)

Pure static site. No build step, no frameworks.

## Local test
```bash
cd Website
python -m http.server 8080
```
Then open http://localhost:8080 or http://<your-pc-ip>:8080 on your phone (same Wi-Fi/hotspot).

## Publish
- GitHub Pages: push this folder, enable Pages.
- Netlify: drag-and-drop this folder at app.netlify.com/drop.
- Cloudflare Pages: connect repo or upload; output dir = this folder.

## Builds
Copy `FrogRunner-Android.apk` and `FrogRunner-Windows.zip` into `downloads/` before deploying.
