using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// 状态机诊断工具，用于调试和测试状态机类型查找
    /// </summary>
    public static class StateMachineDebugger
    {
        /// <summary>
        /// 调试指定类型的状态机信息
        /// </summary>
        public static void DebugStateMachine(string baseTypeName, string methodName)
        {
            try
            {
                Log.Message($"[UTF Debug] 开始调试状态机: {baseTypeName}.{methodName}");
                
                // 查找基础类型
                var baseType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == baseTypeName);
                
                if (baseType == null)
                {
                    Log.Error($"[UTF Debug] 未找到基础类型: {baseTypeName}");
                    return;
                }
                
                Log.Message($"[UTF Debug] 找到基础类型: {baseType.FullName}");
                
                // 检查基础类型的方法
                var methods = baseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                var targetMethod = methods.FirstOrDefault(m => m.Name == methodName);
                
                if (targetMethod != null)
                {
                    Log.Message($"[UTF Debug] 找到目标方法: {targetMethod.Name}");
                    Log.Message($"[UTF Debug] 方法返回类型: {targetMethod.ReturnType.FullName}");
                    
                    // 检查是否是迭代器或异步方法
                    var returnType = targetMethod.ReturnType;
                    var isIterator = returnType.GetInterfaces().Any(i => i.Name.StartsWith("IEnumerable"));
                    var isAsync = returnType.Name.StartsWith("Task") || returnType.Name.StartsWith("ValueTask");
                    
                    Log.Message($"[UTF Debug] 是否为迭代器: {isIterator}");
                    Log.Message($"[UTF Debug] 是否为异步方法: {isAsync}");
                }
                else
                {
                    Log.Warning($"[UTF Debug] 未找到目标方法: {methodName}");
                }
                
                // 列出所有嵌套类型
                var nestedTypes = baseType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
                Log.Message($"[UTF Debug] 嵌套类型数量: {nestedTypes.Length}");
                
                foreach (var nestedType in nestedTypes)
                {
                    Log.Message($"[UTF Debug] 嵌套类型: {nestedType.Name} (完整名称: {nestedType.FullName})");
                    
                    // 检查是否包含方法名
                    if (nestedType.Name.Contains(methodName))
                    {
                        Log.Message($"[UTF Debug] *** 可能的状态机类型: {nestedType.FullName}");
                        
                        // 列出该类型的方法
                        var stateMachineMethods = nestedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var method in stateMachineMethods)
                        {
                            Log.Message($"[UTF Debug]   方法: {method.Name}");
                        }
                        
                        // 检查接口
                        var interfaces = nestedType.GetInterfaces();
                        foreach (var iface in interfaces)
                        {
                            Log.Message($"[UTF Debug]   实现接口: {iface.Name}");
                        }
                    }
                }
                
                // 使用现有的查找方法测试
                var foundType = TranslationFrameworkMod.FindStateMachineTypePublic(baseTypeName, methodName);
                if (foundType != null)
                {
                    Log.Message($"[UTF Debug] 框架找到的状态机类型: {foundType.FullName}");
                }
                else
                {
                    Log.Warning($"[UTF Debug] 框架未找到状态机类型");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF Debug] 调试过程中发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试特定状态机类型的补丁应用
        /// </summary>
        public static void TestStateMachinePatch(string stateMachineTypeName)
        {
            try
            {
                Log.Message($"[UTF Debug] 测试状态机补丁: {stateMachineTypeName}");
                
                var type = Type.GetType(stateMachineTypeName);
                if (type == null)
                {
                    // 尝试从所有程序集查找
                    type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == stateMachineTypeName);
                }
                
                if (type == null)
                {
                    Log.Error($"[UTF Debug] 未找到状态机类型: {stateMachineTypeName}");
                    return;
                }
                
                Log.Message($"[UTF Debug] 找到状态机类型: {type.FullName}");
                
                // 查找MoveNext方法
                var moveNextMethod = AccessTools.Method(type, "MoveNext");
                if (moveNextMethod != null)
                {
                    Log.Message($"[UTF Debug] 找到MoveNext方法: {moveNextMethod.Name}");
                    
                    // 尝试应用一个测试补丁
                    try
                    {
                        var harmony = new Harmony("test.statemachine.debug");
                        var transpiler = typeof(UniversalStringTranspiler).GetMethod("ReplaceStrings");
                        
                        // 注册一个测试翻译
                        TranslationCache.RegisterTranslations($"{type.FullName}.MoveNext", 
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                ["Test"] = "测试"
                            });
                        
                        harmony.Patch(moveNextMethod, transpiler: new HarmonyMethod(transpiler));
                        Log.Message($"[UTF Debug] 成功应用测试补丁到 {type.FullName}.MoveNext");
                    }
                    catch (Exception patchEx)
                    {
                        Log.Error($"[UTF Debug] 应用补丁时发生错误: {patchEx.Message}");
                    }
                }
                else
                {
                    Log.Error($"[UTF Debug] 未找到MoveNext方法");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[UTF Debug] 测试过程中发生错误: {ex.Message}");
            }
        }
    }
}
