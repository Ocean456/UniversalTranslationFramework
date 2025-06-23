using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Verse;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// 状态机补丁生成器 - 用于自动生成和转换状态机补丁
    /// </summary>
    public static class StateMachinePatchGenerator
    {
        /// <summary>
        /// 从普通方法补丁自动生成状态机补丁
        /// </summary>
        /// <param name="baseTypeName">基础类型名称（如：MechCaller.MechConsole）</param>
        /// <param name="methodName">方法名称（如：GetGizmos）</param>
        /// <param name="translations">翻译映射</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <returns>是否成功生成</returns>
        public static bool GenerateStateMachinePatch(string baseTypeName, string methodName, 
            Dictionary<string, string> translations, string outputPath)
        {
            try
            {
                var xmlContent = TranslationFrameworkMod.GenerateStateMachinePatchTemplate(
                    baseTypeName, methodName, translations);

                if (xmlContent.StartsWith("<!--"))
                {
                    UTF_Log.Warning($"[UTF] Failed to generate state machine patch: {xmlContent}");
                    return false;
                }

                // 确保输出目录存在
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outputPath, xmlContent);
                UTF_Log.Message($"[UTF] State machine patch generated: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                UTF_Log.Error($"[UTF] Error generating state machine patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 扫描指定类型的所有可能的状态机方法
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>状态机方法列表</returns>
        public static List<StateMachineInfo> ScanStateMachineMethods(string typeName)
        {
            var results = new List<StateMachineInfo>();

            try
            {
                var type = Type.GetType(typeName) ?? 
                          AppDomain.CurrentDomain.GetAssemblies()
                              .SelectMany(a => a.GetTypes())
                              .FirstOrDefault(t => t.FullName == typeName);

                if (type == null)
                {
                    UTF_Log.Warning($"[UTF] Could not find type: {typeName}");
                    return results;
                }

                // 获取所有嵌套类型（状态机类型）
                var nestedTypes = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var nestedType in nestedTypes)
                {
                    var typeName_nested = nestedType.Name;
                    
                    // 检查是否是状态机类型
                    if (typeName_nested.Contains("<") && typeName_nested.Contains(">") && 
                        (typeName_nested.Contains("d__") || typeName_nested.Contains("c__")))
                    {
                        var methodName = ExtractMethodNameFromStateMachine(typeName_nested);
                        if (!string.IsNullOrEmpty(methodName))
                        {
                            results.Add(new StateMachineInfo
                            {
                                BaseType = type,
                                StateMachineType = nestedType,
                                MethodName = methodName,
                                StateMachineTypeName = nestedType.FullName
                            });
                        }
                    }
                }

                UTF_Log.Message($"[UTF] Found {results.Count} state machine methods in {typeName}");
            }
            catch (Exception ex)
            {
                UTF_Log.Error($"[UTF] Error scanning state machine methods: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 从状态机类型名称中提取方法名称
        /// </summary>
        private static string ExtractMethodNameFromStateMachine(string stateMachineTypeName)
        {
            try
            {
                var startIndex = stateMachineTypeName.IndexOf('<');
                var endIndex = stateMachineTypeName.IndexOf('>');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    return stateMachineTypeName.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 验证状态机补丁是否有效
        /// </summary>
        /// <param name="xmlPath">XML补丁文件路径</param>
        /// <returns>验证结果</returns>
        public static PatchValidationResult ValidateStateMachinePatch(string xmlPath)
        {
            var result = new PatchValidationResult { IsValid = false };

            try
            {
                if (!File.Exists(xmlPath))
                {
                    result.ErrorMessage = "XML file not found";
                    return result;
                }

                var doc = XDocument.Load(xmlPath);
                var operations = doc.Root?.Elements("Operation")
                    .Where(op => op.Attribute("Class")?.Value == "UniversalTranslationFramework.PatchOperationStringTranslate");

                if (operations == null || !operations.Any())
                {
                    result.ErrorMessage = "No valid patch operations found";
                    return result;
                }

                var validOperations = 0;
                var invalidOperations = 0;

                foreach (var operation in operations)
                {
                    var targetType = operation.Element("targetType")?.Value;
                    var targetMethod = operation.Element("targetMethod")?.Value;

                    if (string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(targetMethod))
                    {
                        invalidOperations++;
                        continue;
                    }

                    // 尝试找到目标类型
                    var type = Type.GetType(targetType) ?? 
                              AppDomain.CurrentDomain.GetAssemblies()
                                  .SelectMany(a => a.GetTypes())
                                  .FirstOrDefault(t => t.FullName == targetType);

                    if (type == null)
                    {
                        invalidOperations++;
                        result.Warnings.Add($"Could not find type: {targetType}");
                        continue;
                    }

                    // 检查方法是否存在
                    var method = type.GetMethod(targetMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (method == null)
                    {
                        invalidOperations++;
                        result.Warnings.Add($"Could not find method: {targetType}.{targetMethod}");
                        continue;
                    }

                    validOperations++;
                }

                result.IsValid = validOperations > 0;
                result.ValidOperations = validOperations;
                result.InvalidOperations = invalidOperations;

                if (result.IsValid)
                {
                    result.SuccessMessage = $"Patch validation successful: {validOperations} valid operations";
                }
                else
                {
                    result.ErrorMessage = $"Patch validation failed: {invalidOperations} invalid operations";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Validation error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 创建状态机补丁的快速模板
        /// </summary>
        /// <param name="baseTypeName">基础类型</param>
        /// <param name="methodName">方法名</param>
        /// <param name="findText">要查找的文本</param>
        /// <param name="replaceText">替换文本</param>
        /// <returns>XML字符串</returns>
        public static string CreateQuickTemplate(string baseTypeName, string methodName, 
            string findText, string replaceText)
        {
            var translations = new Dictionary<string, string> { { findText, replaceText } };
            return TranslationFrameworkMod.GenerateStateMachinePatchTemplate(baseTypeName, methodName, translations);
        }
    }

    /// <summary>
    /// 状态机信息
    /// </summary>
    public class StateMachineInfo
    {
        public Type BaseType { get; set; }
        public Type StateMachineType { get; set; }
        public string MethodName { get; set; }
        public string StateMachineTypeName { get; set; }

        public override string ToString()
        {
            return $"{BaseType.FullName}.{MethodName} -> {StateMachineTypeName}";
        }
    }

    /// <summary>
    /// 补丁验证结果
    /// </summary>
    public class PatchValidationResult
    {
        public bool IsValid { get; set; }
        public int ValidOperations { get; set; }
        public int InvalidOperations { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
