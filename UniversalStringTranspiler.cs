using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// Universal string replacement transpiler (optimized version)
    /// This class contains Harmony transpiler methods that replace string constants at runtime.
    /// </summary>
    [HarmonyPatch]
    public static class UniversalStringTranspiler
    {
        /// <summary>
        /// Harmony transpiler method: replaces string constants in the target method (safe version)
        /// </summary>
        /// <param name="instructions">Original IL instruction sequence</param>
        /// <param name="original">Original method being patched</param>
        /// <returns>Modified IL instruction sequence</returns>
        public static IEnumerable<CodeInstruction> ReplaceStrings(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // Construct the method identifier
            var methodId = $"{original.DeclaringType?.FullName}.{original.Name}";

            // Get the translation mapping for this method
            var translations = TranslationCache.GetTranslations(methodId);

            if (translations == null || translations.Count == 0)
            {
                // No translations found; return the original instructions directly
                return instructions;
            }

            try
            {
                var instructionList = instructions.ToList(); // 转换一次，避免多次枚举
                var replacedCount = 0;

                // 安全处理：创建新的指令列表，保持所有标签和引用
                var newInstructions = new List<CodeInstruction>();

                // Iterate over all IL instructions
                for (int i = 0; i < instructionList.Count; i++)
                {
                    var instruction = instructionList[i];

                    // Check if it's a string load instruction (ldstr)
                    if (instruction.opcode == OpCodes.Ldstr &&
                        instruction.operand is string originalString &&
                        translations.TryGetValue(originalString, out var translatedString))
                    {
                        // 创建新的指令，但保持原始指令的所有属性（标签、块等）
                        var newInstruction = new CodeInstruction(OpCodes.Ldstr, translatedString)
                        {
                            labels = instruction.labels != null ? new List<Label>(instruction.labels) : new List<Label>(),
                            blocks = instruction.blocks != null ? new List<ExceptionBlock>(instruction.blocks) : new List<ExceptionBlock>()
                        };

                        newInstructions.Add(newInstruction);
                        replacedCount++;
                    }
                    else
                    {
                        // 保持原始指令不变
                        newInstructions.Add(instruction);
                    }
                }
                if (replacedCount > 0)
                {
                    UTF_Log.Message($"[UTF] Safely replaced {replacedCount} strings in {methodId}");
                }

                return newInstructions;
            }
            catch (Exception ex)
            {
                UTF_Log.Warning($"[UTF] Transpiler failed for {methodId}, falling back to original: {ex.Message}");
                // 如果转译失败，返回原始指令序列
                return instructions;
            }
        }

        /// <summary>
        /// Debug transpiler method: logs all string constants without replacing them.
        /// </summary>
        /// <param name="instructions">Original IL instruction sequence</param>
        /// <param name="original">Original method being patched</param>
        /// <returns>Unmodified IL instruction sequence</returns>
        public static IEnumerable<CodeInstruction> DebugLogStrings(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var methodId = $"{original.DeclaringType?.FullName}.{original.Name}";
            var stringConstants = new List<string>();
            var instructionList = instructions.ToList();
            
            foreach (var instruction in instructionList)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string str)
                {
                    stringConstants.Add(str);
                }
            }
            
            if (stringConstants.Count > 0)
            {
                UTF_Log.Message($"[UTF DEBUG] {methodId} contains {stringConstants.Count} string constants:");
                for (int i = 0; i < stringConstants.Count; i++)
                {
                    UTF_Log.Message($"  [{i}] \"{stringConstants[i]}\"");
                }
            }
            
            return instructionList;
        }

        /// <summary>
        /// Performance monitoring transpiler for development
        /// </summary>
        public static IEnumerable<CodeInstruction> MonitorPerformance(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = ReplaceStrings(instructions, original);
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 10) // UTF_Log if transpilation takes more than 10ms
            {
                UTF_Log.Warning($"[UTF PERF] Slow transpilation for {original.DeclaringType?.FullName}.{original.Name}: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            return result;
        }

        /// <summary>
        /// Ultra-safe transpiler method: for complex methods with many branches and labels
        /// This method preserves all instruction metadata including labels, blocks, and exception handlers
        /// </summary>
        /// <param name="instructions">Original IL instruction sequence</param>
        /// <param name="original">Original method being patched</param>
        /// <returns>Modified IL instruction sequence</returns>
        public static IEnumerable<CodeInstruction> ReplaceStringsSafe(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // Construct the method identifier
            var methodId = $"{original.DeclaringType?.FullName}.{original.Name}";
            
            // Get the translation mapping for this method
            var translations = TranslationCache.GetTranslations(methodId);
            
            if (translations == null || translations.Count == 0)
            {
                // No translations found; return the original instructions directly
                return instructions;
            }

            try
            {
                var instructionList = instructions.ToList();
                var replacedCount = 0;
                bool hasComplexFlow = false;
                
                // 预检查：检测是否有复杂的控制流
                foreach (var instruction in instructionList)
                {
                    if (instruction.labels?.Count > 0 || instruction.blocks?.Count > 0)
                    {
                        hasComplexFlow = true;
                        break;
                    }
                }
                
                if (hasComplexFlow)
                {
                    UTF_Log.Message($"[UTF] Detected complex control flow in {methodId}, using safe mode");
                    
                    // 对于复杂控制流，使用最安全的方式：只替换操作数，不创建新指令
                    for (int i = 0; i < instructionList.Count; i++)
                    {
                        var instruction = instructionList[i];
                        
                        if (instruction.opcode == OpCodes.Ldstr && 
                            instruction.operand is string originalString &&
                            translations.TryGetValue(originalString, out var translatedString))
                        {
                            // 只替换操作数，保持指令对象本身不变
                            instruction.operand = translatedString;
                            replacedCount++;
                        }
                    }
                }
                else
                {
                    // 对于简单控制流，使用标准方式
                    for (int i = 0; i < instructionList.Count; i++)
                    {
                        var instruction = instructionList[i];
                        
                        if (instruction.opcode == OpCodes.Ldstr && 
                            instruction.operand is string originalString &&
                            translations.TryGetValue(originalString, out var translatedString))
                        {
                            // 创建新指令，保持所有元数据
                            var newInstruction = new CodeInstruction(OpCodes.Ldstr, translatedString);
                            
                            // 复制所有元数据
                            if (instruction.labels != null)
                                newInstruction.labels = new List<Label>(instruction.labels);
                            if (instruction.blocks != null)
                                newInstruction.blocks = new List<ExceptionBlock>(instruction.blocks);
                            
                            instructionList[i] = newInstruction;
                            replacedCount++;
                        }
                    }
                }
                
                if (replacedCount > 0)
                {
                    UTF_Log.Message($"[UTF] Safely replaced {replacedCount} strings in {methodId} (complex flow: {hasComplexFlow})");
                }
                
                return instructionList;
            }
            catch (Exception ex)
            {
                UTF_Log.Error($"[UTF] Safe transpiler failed for {methodId}: {ex.Message}");
                UTF_Log.Error($"[UTF] Stack trace: {ex.StackTrace}");
                // 如果连安全模式都失败，返回原始指令
                return instructions;
            }
        }
    }

    /// <summary>
    /// Utility tools for development and debugging (enhanced version)
    /// </summary>
    public static class TranslationDebugTools
    {
        private static readonly Dictionary<string, System.Diagnostics.Stopwatch> _methodTimers = 
            new Dictionary<string, System.Diagnostics.Stopwatch>();

        /// <summary>
        /// Apply debug transpiler to the specified method to UTF_Log string constants it contains.
        /// </summary>
        /// <param name="targetType">Target type</param>
        /// <param name="methodName">Target method name</param>
        public static void DebugMethod(Type targetType, string methodName)
        {
            try
            {
                var method = AccessTools.Method(targetType, methodName);
                if (method == null)
                {
                    UTF_Log.Error($"[UTF DEBUG] Cannot find method: {targetType.FullName}.{methodName}");
                    return;
                }

                var harmony = new Harmony("UTF.Debug.StringInspector");
                var transpilerMethod = typeof(UniversalStringTranspiler).GetMethod(nameof(UniversalStringTranspiler.DebugLogStrings));
                harmony.Patch(method, transpiler: new HarmonyMethod(transpilerMethod));
                
                UTF_Log.Message($"[UTF DEBUG] Debug patch applied to {targetType.FullName}.{methodName}");
            }
            catch (Exception ex)
            {
                UTF_Log.Error($"[UTF DEBUG] Error while debugging method: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply performance monitoring to a method
        /// </summary>
        public static void MonitorMethodPerformance(Type targetType, string methodName)
        {
            try
            {
                var method = AccessTools.Method(targetType, methodName);
                if (method == null)
                {
                    UTF_Log.Error($"[UTF DEBUG] Cannot find method: {targetType.FullName}.{methodName}");
                    return;
                }

                var harmony = new Harmony("UTF.Debug.PerformanceMonitor");
                var transpilerMethod = typeof(UniversalStringTranspiler).GetMethod(nameof(UniversalStringTranspiler.MonitorPerformance));
                harmony.Patch(method, transpiler: new HarmonyMethod(transpilerMethod));
                
                UTF_Log.Message($"[UTF DEBUG] Performance monitoring applied to {targetType.FullName}.{methodName}");
            }
            catch (Exception ex)
            {
                UTF_Log.Error($"[UTF DEBUG] Error while applying performance monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Print translation cache statistics.
        /// </summary>
        public static void PrintCacheStats()
        {
            UTF_Log.Message($"[UTF DEBUG] Translation cache stats: {TranslationCache.GetCacheStats()}");
        }

        /// <summary>
        /// Print detailed framework statistics
        /// </summary>
        public static void PrintFrameworkStats()
        {
            UTF_Log.Message($"[UTF DEBUG] Framework Status:");
            UTF_Log.Message($"  Initialized: {TranslationFrameworkMod.IsInitialized}");
            UTF_Log.Message($"  Loaded Patches: {TranslationFrameworkMod.LoadedPatchesCount}");
            UTF_Log.Message($"  {TranslationCache.GetCacheStats()}");
            
            // Memory usage information
            var memoryBefore = GC.GetTotalMemory(false);
            GC.Collect();
            var memoryAfter = GC.GetTotalMemory(true);
            UTF_Log.Message($"  Memory Usage: {memoryAfter / 1024 / 1024}MB (freed {(memoryBefore - memoryAfter) / 1024}KB)");
        }

        /// <summary>
        /// Benchmark translation performance
        /// </summary>
        public static void BenchmarkTranslations()
        {
            const int iterations = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                TranslationCache.GetCacheStats();
            }
            
            stopwatch.Stop();
            UTF_Log.Message($"[UTF DEBUG] Benchmark: {iterations} cache operations in {stopwatch.ElapsedMilliseconds}ms " +
                       $"({(double)stopwatch.ElapsedMilliseconds / iterations:F3}ms per operation)");
        }
    }
}