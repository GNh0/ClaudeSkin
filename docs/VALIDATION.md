# Validation boundaries — 0.1.0 preview

Implemented:

- Windows .NET 9 WinForms manager, selected theme persistence and read-only IPv4 loopback service.
- Chromium MV3 companion extension restricted to `https://claude.ai/*`; it fetches only the manager's selected theme and does not send conversation content to the manager.
- Shared GIF/MP4 background engine, still poster fallback, reduced motion, hidden-tab pause and complete injected-node removal.
- Experimental Claude Desktop adapter with selected-executable/loopback-port ownership checks, strict `claude.ai` target allowlist, CSS/DOM injection and restore. No packaged-app files are changed.

Verified locally on Windows, SDK 9.0.308:

- Release build and Windows self-test: real media headers, persisted settings, Desktop URL restrictions, owning PID of loopback listener, local API, origin rejection and write rejection.
- Node DOM tests: theme swapping, original chat preservation, restore, reduced motion, poster fallback, external media URL rejection and unchanged-theme polling.

Not yet verified:

- Actual `claude.ai` extension installation and visual compatibility.
- Actual Claude Desktop 2.26454.2 debug-port support, UI compatibility and screenshot.
- Browser-specific local-network permission prompts and future Claude DOM changes.

The computer-use runtime failed to initialize on the validation host (`Windows sandbox setup refresh`), so actual Claude UI checks and captures remain pending. A sample UI preview does not establish compatibility. This is a preview release, not a stability claim.
