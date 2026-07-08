# Security Policy

## Supported Versions

| Version | Supported          | Notes |
| ------- | ------------------ | ----- |
| 5.1.x   | :white_check_mark: | QTTabBar Rebirth (current) |
| 5.0.x   | :x:                | |
| 4.0.x   | :white_check_mark: | Legacy indiff fork line |
| 1.5.6.x | :x:                | Upstream Quizo 1.5.x; no longer maintained here |
| < 4.0   | :x:                | |

## Reporting a Vulnerability

If you discover a security vulnerability in QTTabBar Rebirth, please report it by opening a private security advisory on GitHub, or by emailing the maintainer directly.

- **Do not** open a public issue for security vulnerabilities.
- Please include a description of the vulnerability, steps to reproduce, and the potential impact.
- You can expect an initial response within 72 hours.
- If the vulnerability is accepted, a fix will be prioritized and released in the next patch version.
- If the vulnerability is declined, you will receive an explanation of why.

## Hardening Options (5.1.x)

QTTabBar Rebirth exposes optional security settings under `Config.security` (persisted with user settings):

| Setting | Default | Recommended | Effect |
| ------- | ------- | ----------- | ------ |
| `BlockUntrustedPlugins` | `false` | `true` on managed PCs | Blocks plugins that are neither under the install directory nor Authenticode-/strong-name-trusted (same publisher key as QTTabBar). |
| `BlockLegacyHookDllPath` | `false` | `true` on managed PCs | Prevents loading native hook DLLs from `%ProgramData%\QTTabBar` when a trusted install copy is missing. |

Defaults remain permissive for backward compatibility. Enable both flags in environments where plugin or DLL hijacking is a concern.

### Deserialization binder (IPC / DeepClone)

IPC between Explorer instances uses `BinaryFormatter` with a restricted deserialization binder:

- **Whitelist**: only explicitly listed `QTTabBarLib` types (e.g. `SerializeDelegate`, `Config` and nested types) are bound by name.
- **Core framework gadget block**: in `mscorlib` / `System` / `System.Core`, known-dangerous `ISerializable` types are rejected — e.g. `System.Security.*` identity types, `System.Security.Claims.*`, and `Exception` derivatives — while collections (`Dictionary`, `List`, …) and delegate infrastructure holders remain allowed so `Config` DeepClone works.
- **Drawing / WinForms**: `System.Drawing` and `System.Windows.Forms` ISerializable types (e.g. `Font`) remain allowed because `Config` stores font settings.
- **External assemblies**: any non-whitelisted serializable type is rejected.

Named-pipe messages are capped at 4 MB.

### Plugin signature trust

When signature validation is used (trusted-directory fallback):

- **Strong name**: only assemblies signed with the same `PublicKeyToken` as the main QTTabBar assembly are trusted.
- **Authenticode**: only signatures whose certificate thumbprint matches the main assembly's Authenticode certificate are trusted (offline revocation check). If the main assembly is not Authenticode-signed, the Authenticode path is unavailable and directory/strong-name rules apply.
