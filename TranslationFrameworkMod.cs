using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using HarmonyLib;
using Verse;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// Main entry point for the Universal Translation Framework - RimWorld Mod
    /// Compatible version without System.Collections.Immutable dependency
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TranslationFrameworkMod
    {
        public const string FRAMEWORK_HARMONY_ID = "Ocean.Universal.Translation.Framework";
        
        private static readonly Lazy<List<TranslationPatch>> _patches = 
            new Lazy<List<TranslationPatch>>(DiscoverAndLoadPatchesInternal);
        
        private static readonly ConcurrentDictionary<string, Type> _typeCache = 
            new ConcurrentDictionary<string, Type>();
        
        private static readonly ConcurrentDictionary<string, Assembly> _assemblyCache = 
            new ConcurrentDictionary<string, Assembly>();
        
        private static readonly object _initLock = new object();
        private static volatile bool _initialized = false;
        private static Harmony _harmony;

        static TranslationFrameworkMod()
        {
            // 使用LongEventHandler进行异步初始化，避免阻塞游戏启动
            LongEventHandler.QueueLongEvent(() => InitializeFramework(), "Loading Universal Translation Framework", false, null, showExtraUIInfo: false, forceHideUI: false);
        }

        private static void InitializeFramework()
        {
            if (_initialized) return;
            
            lock (_initLock)
            {
                if (_initialized) return;
                
                try
                {
                    Log.Message("[UTF] Universal Translation Framework is starting...");
                    
                    _harmony = new Harmony(FRAMEWORK_HARMONY_ID);
                    
                    // 触发延迟加载
                    var patches = _patches.Value;
                    ApplyAllPatches(patches);
                    
                    Log.Message($"[UTF] Universal Translation Framework started successfully! Loaded {patches.Count} translation patches.");
                    _initialized = true;
                }
                catch (Exception ex)
                {
                    Log.Error($"[UTF] Failed to start Universal Translation Framework: {ex}");
                }
            }
        }

        /// <summary>
        /// Automatically discover and load translation patches from all mods (optimized version)
        /// </summary>
        private static List<TranslationPatch> DiscoverAndLoadPatchesInternal()
        {
            var discoveredPatches = new List<TranslationPatch>();
            var lockObject = new object();

            try
            {
                // 检查是否支持并行处理
                if (Environment.ProcessorCount > 1)
                {
                    var parallelOptions = new ParallelOptions 
                    { 
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
                    };

                    // 并行处理mod扫描
                    Parallel.ForEach(LoadedModManager.RunningMods, parallelOptions, mod =>
                    {
                        var modPatches = ScanModForPatches(mod);
                        lock (lockObject)
                        {
                            discoveredPatches.AddRange(modPatches);
                        }
                    });
                }
                else
                {
                    // 单线程处理（兼容性回退）
                    foreach (var mod in LoadedModManager.RunningMods)
                    {
                        var modPatches = ScanModForPatches(mod);
                        discoveredPatches.AddRange(modPatches);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF] Error during parallel processing, falling back to sequential: {ex.Message}");
                
                // 回退到单线程处理
                discoveredPatches.Clear();
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    try
                    {
                        var modPatches = ScanModForPatches(mod);
                        discoveredPatches.AddRange(modPatches);
                    }
                    catch (Exception modEx)
                    {
                        Log.Error($"[UTF] Error scanning mod {mod.Name}: {modEx.Message}");
                    }
                }
            }

            return discoveredPatches;
        }

        private static List<TranslationPatch> ScanModForPatches(ModContentPack mod)
        {
            var patches = new List<TranslationPatch>();
            var patchesDir = Path.Combine(mod.RootDir, "Patches");
            
            if (!Directory.Exists(patchesDir))
                return patches;

            try
            {
                // 使用EnumerateFiles避免一次性加载所有文件到内存
                var translationFiles = Directory.EnumerateFiles(patchesDir, "*StringTranslation*.xml", 
                    SearchOption.AllDirectories);

                foreach (var filePath in translationFiles)
                {
                    try
                    {
                        var filePatches = LoadTranslationPatchesFromFileOptimized(filePath, mod);
                        patches.AddRange(filePatches);
                        
                        Log.Message($"[UTF] Loaded {filePatches.Count} translation patches from {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[UTF] Failed to load translation patch file {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF] Error occurred while scanning mod '{mod.Name}': {ex.Message}");
            }

            return patches;
        }

        /// <summary>
        /// Load translation patches from an XML file (optimized version)
        /// </summary>
        private static List<TranslationPatch> LoadTranslationPatchesFromFileOptimized(string filePath, ModContentPack mod)
        {
            var patches = new List<TranslationPatch>();
            
            try
            {
                // 使用更安全的XML加载方式
                XDocument doc;
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192))
                    using (var reader = XmlReader.Create(fileStream, new XmlReaderSettings
                    {
                        IgnoreWhitespace = true,
                        IgnoreComments = true,
                        IgnoreProcessingInstructions = true,
                        DtdProcessing = DtdProcessing.Ignore
                    }))
                    {
                        doc = XDocument.Load(reader);
                    }
                }
                catch (Exception)
                {
                    // 回退到简单加载方式
                    doc = XDocument.Load(filePath);
                }

                var root = doc.Root;

                if (root?.Name != "Patch")
                {
                    Log.Warning($"[UTF] Invalid patch file format {filePath}: Root element must be <Patch>");
                    return patches;
                }

                // 查找相关的操作
                var operations = root.Elements("Operation")
                    .Where(op => op.Attribute("Class")?.Value == "UniversalTranslationFramework.PatchOperationStringTranslate");

                foreach (var operation in operations)
                {
                    var patch = ParseStringTranslationPatchOptimized(operation, mod, filePath);
                    if (patch != null)
                    {
                        patches.Add(patch);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF] XML parse error {filePath}: {ex.Message}");
            }

            return patches;
        }

        /// <summary>
        /// Parse a single string translation patch operation (optimized version)
        /// </summary>
        private static TranslationPatch ParseStringTranslationPatchOptimized(XElement operation, ModContentPack mod, string sourceFile)
        {
            try
            {
                var targetAssembly = operation.Element("targetAssembly")?.Value;
                var targetType = operation.Element("targetType")?.Value;
                var targetMethod = operation.Element("targetMethod")?.Value;
                
                if (string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(targetMethod))
                {
                    Log.Warning($"[UTF] Skipping invalid patch operation: Missing targetType or targetMethod ({sourceFile})");
                    return null;
                }

                var replacementsElement = operation.Element("replacements");
                if (replacementsElement == null)
                    return null;

                var items = replacementsElement.Elements("li").ToList();
                var replacements = new List<StringTranslation>(items.Count);

                foreach (var item in items)
                {
                    var original = item.Element("find")?.Value;
                    var translated = item.Element("replace")?.Value;

                    if (!string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(translated))
                    {
                        replacements.Add(new StringTranslation
                        {
                            OriginalText = original,
                            TranslatedText = translated
                        });
                    }
                }

                if (replacements.Count > 0)
                {
                    return new TranslationPatch
                    {
                        SourceMod = mod,
                        SourceFile = sourceFile,
                        TargetAssembly = targetAssembly,
                        TargetTypeName = targetType,
                        TargetMethodName = targetMethod,
                        Translations = replacements
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF] Failed to parse translation patch operation ({sourceFile}): {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Apply all discovered translation patches (optimized version)
        /// </summary>
        private static void ApplyAllPatches(List<TranslationPatch> patches)
        {
            var appliedCount = 0;
            var failedCount = 0;

            // 按类型分组，减少重复的类型查找
            var patchesByType = patches.GroupBy(p => p.TargetTypeName).ToList();

            foreach (var typeGroup in patchesByType)
            {
                try
                {
                    var targetType = FindTypeOptimized(typeGroup.Key, typeGroup.First().TargetAssembly);
                    if (targetType == null)
                    {
                        Log.Warning($"[UTF] Could not find target type: {typeGroup.Key}");
                        failedCount += typeGroup.Count();
                        continue;
                    }

                    foreach (var patch in typeGroup)
                    {
                        try
                        {
                            if (ApplyTranslationPatchOptimized(patch, targetType))
                            {
                                appliedCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            Log.Error($"[UTF] Failed to apply translation patch {patch.TargetTypeName}.{patch.TargetMethodName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedCount += typeGroup.Count();
                    Log.Error($"[UTF] Failed to process type group {typeGroup.Key}: {ex.Message}");
                }
            }

            Log.Message($"[UTF] Patch application complete: {appliedCount} succeeded, {failedCount} failed");
        }
        
        /// <summary>
        /// Apply a single translation patch (compatible version)
        /// </summary>
        private static bool ApplyTranslationPatchOptimized(TranslationPatch patch, Type targetType)
        {
            try
            {
                // Find target method
                var targetMethod = AccessTools.Method(targetType, patch.TargetMethodName);
                if (targetMethod == null)
                {
                    Log.Warning($"[UTF] Could not find target method: {patch.TargetTypeName}.{patch.TargetMethodName}");
                    return false;
                }

                // Build translation map using regular Dictionary (compatible approach)
                var translationMap = new Dictionary<string, string>();
                foreach (var translation in patch.Translations)
                {
                    translationMap[translation.OriginalText] = translation.TranslatedText;
                }

                // Cache translation map
                var methodId = $"{targetType.FullName}.{targetMethod.Name}";
                TranslationCache.RegisterTranslations(methodId, translationMap);                // Apply appropriate patch based on method characteristics
                if (IsGetGizmosMethod(targetMethod))
                {
                    // For GetGizmos methods, use BOTH Transpiler and Postfix
                    // Transpiler handles string literals in the method body
                    var transpilerMethod = typeof(UniversalStringTranspiler).GetMethod(nameof(UniversalStringTranspiler.ReplaceStrings));
                    // Postfix handles dynamic Command_Action properties
                    var postfixMethod = typeof(UniversalStringTranspiler).GetMethod(nameof(UniversalStringTranspiler.TranslateGizmoLabels));
                    
                    _harmony.Patch(targetMethod, 
                        transpiler: new HarmonyMethod(transpilerMethod),
                        postfix: new HarmonyMethod(postfixMethod));
                    
                    Log.Message($"[UTF] Complete Gizmo translation patch applied: {patch.TargetTypeName}.{patch.TargetMethodName} (Transpiler + Postfix, {patch.Translations.Count} strings)");
                }
                else
                {
                    // For regular methods, use Transpiler to replace string constants
                    var transpilerMethod = typeof(UniversalStringTranspiler).GetMethod(nameof(UniversalStringTranspiler.ReplaceStrings));
                    _harmony.Patch(targetMethod, transpiler: new HarmonyMethod(transpilerMethod));
                    Log.Message($"[UTF] String translation patch applied: {patch.TargetTypeName}.{patch.TargetMethodName} ({patch.Translations.Count} strings)");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF] Exception in ApplyTranslationPatchOptimized: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Check if the method is a GetGizmos method that returns IEnumerable<Gizmo>
        /// </summary>
        private static bool IsGetGizmosMethod(MethodInfo method)
        {
            return method.Name == "GetGizmos" && 
                   typeof(IEnumerable<Gizmo>).IsAssignableFrom(method.ReturnType);
        }

        /// <summary>
        /// Find type by name, optionally from a specific assembly (optimized version with caching)
        /// </summary>
        private static Type FindTypeOptimized(string typeName, string assemblyName = null)
        {
            // 构建缓存键
            var cacheKey = string.IsNullOrEmpty(assemblyName) ? typeName : $"{assemblyName}:{typeName}";
            
            return _typeCache.GetOrAdd(cacheKey, key =>
            {
                try
                {
                    // 如果指定了程序集，优先从该程序集查找
                    if (!string.IsNullOrEmpty(assemblyName))
                    {
                        var assembly = _assemblyCache.GetOrAdd(assemblyName, name =>
                        {
                            try
                            {
                                // 尝试多种方式加载程序集
                                Assembly asm = null;
                                
                                // 方式1: 直接从文件加载
                                if (File.Exists(name))
                                {
                                    asm = Assembly.LoadFrom(name);
                                }
                                
                                // 方式2: 从已加载的程序集中查找
                                if (asm == null)
                                {
                                    asm = AppDomain.CurrentDomain.GetAssemblies()
                                        .FirstOrDefault(a => a.GetName().Name == Path.GetFileNameWithoutExtension(name));
                                }
                                
                                // 方式3: 尝试Load方法
                                if (asm == null)
                                {
                                    asm = Assembly.Load(name);
                                }
                                
                                return asm;
                            }
                            catch
                            {
                                return null;
                            }
                        });

                        if (assembly != null)
                        {
                            var type = assembly.GetType(typeName);
                            if (type != null)
                                return type;
                        }
                    }

                    // 回退到AccessTools搜索
                    return AccessTools.TypeByName(typeName);
                }
                catch (Exception ex)
                {
                    Log.Error($"[UTF] Error finding type {typeName}: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Get initialization status
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Get loaded patches count
        /// </summary>
        public static int LoadedPatchesCount => _initialized ? _patches.Value.Count : 0;

        /// <summary>
        /// Force initialization (for testing purposes)
        /// </summary>
        public static void ForceInitialize()
        {
            if (!_initialized)
            {
                InitializeFramework();
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public static string GetCacheStatistics()
        {
            return $"Type cache: {_typeCache.Count} entries, Assembly cache: {_assemblyCache.Count} entries";
        }
    }
}