# Universal Translation Framework

**通用翻译框架** - RimWorld Mod的简单翻译解决方案

## 📋 项目简介

Universal Translation Framework 是一个为RimWorld游戏设计的Mod翻译框架，让你可以轻松翻译任何Mod的界面文本，无需修改原Mod文件。

## 🤔 何时使用这个框架？

### 官方翻译系统 vs Universal Translation Framework

**优先推荐：官方翻译系统** 📚

大部分现代RimWorld Mod都支持官方翻译系统，你只需要在Mod目录下的 `Languages/ChineseSimplified/` 文件夹中添加翻译文件：

```
ModName/
├── Languages/
│   └── ChineseSimplified/
│       └── Keyed/
│           └── Keys.xml
```

**何时需要这个框架？** 🔧

在以下情况下，官方翻译系统无法解决问题，这时就需要Universal Translation Framework：

- ❌ **老旧Mod**：使用硬编码字符串，没有翻译支持
- ❌ **应急翻译**：新Mod还没有翻译接口时的临时解决方案

### 使用建议 💡

1. **首先检查**：Mod是否已有 `Languages/` 文件夹？
   - ✅ 有 → 使用官方翻译系统
   - ❌ 没有 → 使用Universal Translation Framework

2. **实际案例**：
   - `Prison Labor`、`Hospitality` 等现代Mod → 使用官方翻译
   - `KillFeed`、老版本Mod → 使用这个框架

3. **性能考虑**：官方翻译系统性能更好，应优先选择

## ✨ 主要特性

- **🔄 实时翻译**: 游戏运行时动态替换文本
- **📦 自动发现**: 自动扫描并加载翻译补丁
- **✏️ 简单配置**: 只需编写简单的XML文件
- **🎯 精确匹配**: 准确替换指定的文本内容

## 🚀 快速使用

### 第一步：安装框架

1. 下载 `UniversalTranslationFramework.dll`
2. 放入 RimWorld 的 Mods 目录：
   ```
   RimWorld/Mods/UniversalTranslationFramework/Assemblies/
   ```

### 第二步：创建翻译文件

在你想翻译的Mod目录下创建 `Patches` 文件夹，然后创建包含 "StringTranslation" 字样的XML文件。

**文件结构示例：**
```
YourMod/
├── Patches/
│   └── StringTranslation_ModName.xml  # 文件名必须包含 "StringTranslation"
└── 其他文件...
```

### 第三步：编写翻译补丁

复制以下模板到你的XML文件中：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>目标类名</targetType>
    <targetMethod>目标方法名</targetMethod>
    <replacements>
      <li>
        <find>原文文本</find>
        <replace>翻译文本</replace>
      </li>
      <!-- 添加更多翻译 -->
    </replacements>
  </Operation>
</Patch>
```

## 📝 实际使用示例

以下是翻译 KillFeed Mod 的真实示例：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <!-- 翻译设置界面 -->
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>KillFeed.Settings</targetType>
    <targetMethod>DoWindowContents</targetMethod>
    <replacements>
      <li>
        <find>Display wild animal's death?</find>
        <replace>显示野生动物死亡？</replace>
      </li>
      <li>
        <find>Display ally's death?</find>
        <replace>显示盟友死亡？</replace>
      </li>
      <li>
        <find>Width</find>
        <replace>宽度</replace>
      </li>
      <li>
        <find>Height</find>
        <replace>高度</replace>
      </li>
    </replacements>
  </Operation>

  <!-- 翻译游戏消息 -->
  <Operation Class="UniversalTranslationFramework.PatchOperationStringTranslate">
    <targetType>KillFeed.HarmonyPatches</targetType>
    <targetMethod>Patch_Pawn_Kill</targetMethod>
    <replacements>
      <li>
        <find> died from </find>
        <replace> 死于 </replace>
      </li>
    </replacements>
  </Operation>
</Patch>
```

## 🔍 如何找到要翻译的类和方法？

1. **使用ILSpy等反编译工具**查看Mod的DLL文件
2. **查看Mod源码**（如果开源）
3. **使用游戏内调试工具**定位UI元素
4. **参考其他翻译补丁**的写法

## ⚠️ 注意事项

- 文件名必须包含 "StringTranslation" 字样
- XML文件必须放在 `Patches` 目录下
- 原文必须**完全匹配**，包括空格和标点
- 重启游戏后翻译生效

## 🎯 常见问题

**Q: 翻译不生效怎么办？**
- 检查XML文件名是否包含 "StringTranslation"
- 确认原文是否完全匹配（包括空格）
- 重启游戏让翻译生效

**Q: 如何找到要翻译的类和方法？**
- 推荐使用 ILSpy 等反编译工具查看Mod的DLL
- 查看Mod的源码（如果开源）

**Q: 可以翻译多个方法吗？**
- 可以！在同一个XML文件中添加多个 `Operation` 节点

**Q: 支持正则表达式吗？**
- 当前版本专注于精确匹配，正则支持在规划中

**Q: 为什么不直接使用官方翻译系统？**
- 官方翻译系统是首选，但某些情况下不可用：
  - 老旧Mod使用硬编码字符串
  - Mod作者未提供翻译支持

## 🛠️ 技术原理

- **Harmony 补丁系统**: 使用IL代码转换器实现运行时字符串替换
- **智能扫描**: 自动发现并加载翻译补丁，支持并行处理
- **缓存优化**: 使用高效的缓存机制提升性能
- **非侵入式**: 不修改原始Mod文件，保持兼容性

## 🔗 相关链接

- [RimWorld 官方网站](https://rimworldgame.com/)
- [Harmony 补丁库](https://github.com/pardeike/Harmony)
- [ILSpy 反编译工具](https://github.com/icsharpcode/ILSpy)
