# Astar Vanguard

**English** · [简体中文](README.zh-CN.md)

AI tactical support mod for **SPT 4.0.13 / EFT 40087**.

The goal is to turn AI teammates into a persistent squad that can be recruited, managed, and reused across raids instead of treating every raid as a fresh set of temporary bots. Development currently focuses on the Vanguard Command Center, persistent operators, and the tactical support systems around them.

## Current features

- Adds the **Vanguard Command Center** with its own management interface.
- Provides **37 persistent operators**, each with an individual callsign, role, and combat profile.
- Supports permanent recruitment and preserves the identity of recruited operators.
- Supports **1–4 operator deployments** managed directly from the Command Center.
- Gives operators individual aim, vision, hearing, reaction, aggression, damage, enemy-memory, and cover characteristics.
- Continues to maintain tactical AI commands, interactions, and Fika integration.

## Supported environment

Development and verification currently target:

- **SPT 4.0.13**
- **EFT Build 40087**
- **BigBrain**
- **Fika Host / Solo Host**

Other SPT / EFT versions are not assumed to be compatible. Fika work currently focuses on Host / Solo Host behavior; full remote-client behavior will continue to be verified as development progresses.

## Installation and usage

Installable builds will be published through GitHub Releases. Once a release package is available, copy it into the matching SPT installation using the directory structure included in the archive. Do not use GitHub's automatically generated `Source code` archives as mod packages.

The main entry point is **Vanguard Command** in the trader interface. The Command Center is used to inspect the roster, permanently recruit operators, and manage the current deployment squad.

The client requires BigBrain. Fika integration also requires a compatible Fika installation. Required UI runtime components will be included with official release packages, so players will not need to obtain development sources separately.

## Source

Source code is maintained in this repository. The current codebase explicitly targets SPT 4.0.13 / EFT 40087; local dependencies matching that environment are required for source builds.

```powershell
dotnet build .\Astar.Vanguard.slnx -c Debug
dotnet build .\Astar.Vanguard.slnx -c Release
```

## Open source and origin

Development originally started from the CC BY-NC-SA 4.0 source of **MiyakoCarryService**. Since then, the project has been substantially reworked and expanded across branding, UI, persistent operators, behavior logic, Fika integration, and its overall development direction.

Astar Vanguard is distributed under **CC BY-NC-SA 4.0**. Rights in original code and assets remain with their respective authors and contributors. New original code, refactoring, documentation, product identity, and systems are distributed as part of the derivative project under the same license.

Upstream attribution, the fixed derivative baseline, and third-party notices are documented in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

License: **[English](LICENSE)** · [简体中文](LICENSE.zh-CN.md)
