# SOCOM Expansion Core

A lightweight shared trader integration library for optional SOCOM Armory weapon expansions on SPT 4.1.6.

This library registers offers supplied by separately installed expansion modules, checks duplicate IDs, and merges them into the SOCOM trader without replacing its existing inventory. It contains no weapon or equipment content.

## Requirements

- [SOCOM Armory — Unofficial SPT 4.1.6 Port](https://sp-mod.com/mod/3375/socom-armory-unofficial-spt-416-port)

The parent mod is responsible for its own dependencies.

## For developers

Expansion modules reference `SocomExpansionCore.dll` and register their offer resources with `SOCOMExpansionCore.SocomTraderOffers.Register(Assembly.GetExecutingAssembly())` at the correct load stage. Registration occurs before the core applies the collected offers. The expansion module remains responsible for its own custom item creation.

## Building

Requires .NET 10 and SPT 4.1.6 server reference assemblies. The source project expects those references in `../../references/` relative to the project file. Obtain the reference DLLs from your own SPT installation and build environment. Third-party DLLs are not included in this source repository.

## License

MIT (see LICENSE). Third-party mods and SPT reference assemblies are not included.

## Dependency identity

The supported SOCOM 4.1.6 port declares internal mod GUID `com.mtmilitia.socom` and version `2.0.3`. The core uses that internal identity for dependency checking. The Forge listing identifier is not the mod GUID.
