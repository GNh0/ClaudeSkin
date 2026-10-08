# ClaudeSkin

**One Windows manager for animated Claude Web and Desktop themes.**

This is a **0.1.0 preview**. Both adapters are implemented; actual Claude UI compatibility and captures remain pending. This is an independent project, not an Anthropic product.

[한국어](README.ko.md) · [Validation status](docs/VALIDATION.md) · [Media attribution](docs/ASSET-NOTES.md) · [GNho Labs](https://gnholabs.com) · [Live preview](https://gnholabs.com/claudeskin/)

![Real Windows manager — poster preview, not an official Claude capture](docs/media/manager-preview.png)

## Themes

Shorekeeper (GIF), Firefly (MP4), and Jinhsi (MP4), migrated from CoSkin with still PNG posters. Reduced-motion preferences and the motion switch use the still poster. Background videos are muted, looped and paused in hidden tabs.

## Windows

Download the preview ZIP from [Releases](https://github.com/GNh0/ClaudeSkin/releases), extract it to a new folder and run `ClaudeSkin.exe`. Keep `themes/`, `shared/` and `extension/` beside the executable. The manager requires neither administrator privileges nor an API key.

### Web companion

Load the unpacked `extension/` directory using developer mode in Chrome or Edge. Select a theme in the manager, enable Web, save, and reload `https://claude.ai`. The extension checks for changes every five seconds. Closing the manager causes it to remove the theme when it detects the disconnection.

The extension requests `claude.ai` and loopback host access plus alarms. The read-only manager endpoint binds only `127.0.0.1:9164`. It rejects ordinary webpage origins, non-loopback Host headers and write methods. Only selected media/settings are served; conversation content is not collected or sent. There is no login automation, model API usage or telemetry. Browser local-network policies and Claude's DOM/CSP may affect compatibility; actual browser installation is not yet verified.

### Desktop — experimental

Close Claude Desktop normally, select its official `Claude.exe`, choose a free port and use **Open in debug mode** in the manager. Once the Claude chat screen loads, use **Connect and apply**. **Restore** removes injected nodes/styles. Restart Desktop normally after use to close the debugging port.

The adapter checks loopback port ownership against the selected executable and targets only HTTPS pages whose exact host is `claude.ai`. It neither patches packaged app files nor kills processes. Builds that reject remote debugging or use another app URL will not connect. Other local programs can access a debug port while it is open. **Actual Desktop application and screenshot have not yet been verified.**

## Build and check

```powershell
dotnet build -c Release
.\bin\Release\net9.0-windows\ClaudeSkin.exe --self-test
npm ci
npm test
npm run demo:prepare
```

See [Korean instructions](README.ko.md) for exact UI steps and publishing arguments. Settings live in `%LOCALAPPDATA%\ClaudeSkin\settings.json`. Both adapters share `shared/skin-engine.js`, `shared/skin.css` and `themes/catalog.json`. Keep the extension's packaged engine identical; tests check this.

The site demo applies the real engine to a clearly labelled sample conversation UI. It is not an official Claude screenshot. MIT covers the code only; see media attribution for the separate artwork boundaries.
