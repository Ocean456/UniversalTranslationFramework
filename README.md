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

#### Basic Translation
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

#### Format String Translation (NEW)
For strings with placeholders like `{0}`, `{1}`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>TargetClassName</targetType>
    <targetMethod>TargetMethodName</targetMethod>
    <replacements>
      <li isFormatString="true">
        <find>A Mechanoid Raid will arrive in {0} hour{1}! Be prepared!</find>
        <replace>机械族袭击将于{0}小时{1}后到来!做好准备!</replace>
      </li>
    </replacements>
  </Operation>
</Patch>
```

#### Regex Pattern Translation (NEW)
For advanced pattern matching:

```xml
<li isRegex="true" pattern="Test\d+">
  <find>Test\d+</find>
  <replace>测试\1</replace>
</li>
```

## Example

### Basic Translation
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

### Format String Translation
```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>AlertsReadoutUtility</targetType>
    <targetMethod>AlertsReadoutOnGUI</targetMethod>
    <replacements>
      <!-- Auto-detected format string -->
      <li>
        <find>Raid will arrive in {0} hours</find>
        <replace>袭击将在{0}小时后到来</replace>
      </li>
      <!-- Explicitly marked format string -->
      <li isFormatString="true">
        <find>You have {0} colonist{1} and {2} prisoner{3}</find>
        <replace>你有{0}名殖民者{1}和{2}名囚犯{3}</replace>
      </li>
    </replacements>
  </Operation>
</Patch>
```

## Requirements

- Filename must contain "StringTranslation"
- XML files must be in `Patches` directory
- Text matching is case-sensitive and exact for basic translations
- Format strings with placeholders `{0}`, `{1}`, etc. are automatically detected
- Placeholder compatibility is validated between source and target
- Game restart required for changes

## Features

### Basic Translation
- **Exact string matching**: Replace hardcoded strings with translations
- **Case-sensitive matching**: Ensures precise replacements

### Format String Support (NEW)
- **Automatic detection**: Detects `{0}`, `{1}` style placeholders
- **Pattern matching**: Matches runtime format strings with placeholders
- **Placeholder preservation**: Ensures all placeholders from source exist in target
- **Compatibility validation**: Warns about mismatched placeholders

### Advanced Features
- **Regex patterns**: Support for complex pattern matching
- **Context support**: Add translation notes and context
- **Quality scoring**: Track translation quality (0-100)
- **Performance optimization**: Cached translations and pattern matching

## Technical Implementation

- **Harmony Integration**: IL code transpilation for runtime string replacement
- **Automatic Discovery**: Scans mod directories for translation patches
- **Parallel Processing**: Multi-threaded patch loading when supported
- **Caching**: Type and assembly caching for performance optimization

## Troubleshooting

**Translation not working:**
- Verify filename contains "StringTranslation"
- Check exact text matching for basic translations
- For format strings, ensure placeholder compatibility
- Restart game

**Format string issues:**
- Check console for placeholder mismatch warnings
- Ensure target translation has all placeholders from source
- Use `isFormatString="true"` for explicit format string marking
- Verify runtime string matches the expected pattern

**Finding target classes/methods:**
- Use decompilation tools (dnSpy recommended)
- Check mod source code if available
- Look for string.Format() calls for format string opportunities

## Related Resources

- [RimWorld Official Website](https://rimworldgame.com/)
- [Harmony Patching Library](https://github.com/pardeike/Harmony)
- [dnSpy Decompiler](https://github.com/dnSpyEx/dnSpy)
