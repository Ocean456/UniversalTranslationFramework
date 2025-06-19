# Universal Translation Framework

A runtime string translation framework for RimWorld mods.

## Overview

Universal Translation Framework provides runtime string replacement for RimWorld mods that lack native translation support through IL code transpilation.

## When to Use This Framework

### Official Translation System (Recommended)

Most modern RimWorld mods support the official translation system. Create translation files in:

```
ModName/
├── Languages/
│   └── ChineseSimplified/
│       └── Keyed/
│           └── Keys.xml
```

### Use Cases for This Framework

- Legacy mods with hardcoded strings
- Mods without translation infrastructure

## Installation

Download `UniversalTranslationFramework.dll` and place in:
```
RimWorld/Mods/UniversalTranslationFramework/Assemblies/
```

## Usage

### File Structure

Create translation files in the target mod's `Patches` directory. Filename must contain "StringTranslation":

```
TargetMod/
├── Patches/
│   └── StringTranslation_ModName.xml
```

### Translation Patch Format

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>TargetClassName</targetType>
    <targetMethod>TargetMethodName</targetMethod>
    <replacements>
      <li>
        <find>Original text</find>
        <replace>Translated text</replace>
      </li>
    </replacements>
  </Operation>
</Patch>
```

## Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>KillFeed.Settings</targetType>
    <targetMethod>DoWindowContents</targetMethod>
    <replacements>
      <li>
        <find>Display wild animal's death?</find>
        <replace>显示野生动物死亡？</replace>
      </li>
    </replacements>
  </Operation>
</Patch>
```

## Requirements

- Filename must contain "StringTranslation"
- XML files must be in `Patches` directory
- Text matching is case-sensitive and exact
- Game restart required for changes

## Technical Implementation

- **Harmony Integration**: IL code transpilation for runtime string replacement
- **Automatic Discovery**: Scans mod directories for translation patches
- **Parallel Processing**: Multi-threaded patch loading when supported
- **Caching**: Type and assembly caching for performance optimization

## Troubleshooting

**Translation not working:**
- Verify filename contains "StringTranslation"
- Check exact text matching
- Restart game

**Finding target classes/methods:**
- Use decompilation tools (dnSpy recommended)
- Check mod source code if available

## Related Resources

- [RimWorld Official Website](https://rimworldgame.com/)
- [Harmony Patching Library](https://github.com/pardeike/Harmony)
- [dnSpy Decompiler](https://github.com/dnSpyEx/dnSpy)
