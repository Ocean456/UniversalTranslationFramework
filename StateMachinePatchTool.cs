using System;
using System.Collections.Generic;
using System.IO;
using UniversalTranslationFramework;

namespace UniversalTranslationFramework.Tools
{
    /// <summary>
    /// 状态机补丁工具控制台应用程序
    /// 用于演示和测试状态机补丁功能
    /// </summary>
    class StateMachinePatchTool
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Universal Translation Framework - 状态机补丁工具 ===");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "scan":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("错误: 请指定要扫描的类型名称");
                            Console.WriteLine("用法: StateMachinePatchTool scan <TypeName>");
                            return;
                        }
                        ScanStateMachines(args[1]);
                        break;

                    case "generate":
                        if (args.Length < 6)
                        {
                            Console.WriteLine("错误: 参数不足");
                            Console.WriteLine("用法: StateMachinePatchTool generate <BaseType> <Method> <FindText> <ReplaceText> <OutputFile>");
                            return;
                        }
                        GeneratePatch(args[1], args[2], args[3], args[4], args[5]);
                        break;

                    case "convert":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("错误: 请指定要转换的XML文件路径");
                            Console.WriteLine("用法: StateMachinePatchTool convert <InputXmlFile> [OutputXmlFile]");
                            return;
                        }
                        var outputFile = args.Length > 2 ? args[2] : null;
                        ConvertPatch(args[1], outputFile);
                        break;

                    case "validate":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("错误: 请指定要验证的XML文件路径");
                            Console.WriteLine("用法: StateMachinePatchTool validate <XmlFile>");
                            return;
                        }
                        ValidatePatch(args[1]);
                        break;

                    case "batch":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("错误: 请指定Patches目录路径");
                            Console.WriteLine("用法: StateMachinePatchTool batch <PatchesDirectory>");
                            return;
                        }
                        BatchConvert(args[1]);
                        break;

                    default:
                        Console.WriteLine($"未知命令: {args[0]}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void ShowUsage()
        {
            Console.WriteLine("用法:");
            Console.WriteLine("  StateMachinePatchTool scan <TypeName>");
            Console.WriteLine("    - 扫描指定类型的所有状态机方法");
            Console.WriteLine();
            Console.WriteLine("  StateMachinePatchTool generate <BaseType> <Method> <FindText> <ReplaceText> <OutputFile>");
            Console.WriteLine("    - 生成状态机补丁XML文件");
            Console.WriteLine();
            Console.WriteLine("  StateMachinePatchTool convert <InputXmlFile> [OutputXmlFile]");
            Console.WriteLine("    - 将普通补丁转换为状态机补丁");
            Console.WriteLine();
            Console.WriteLine("  StateMachinePatchTool validate <XmlFile>");
            Console.WriteLine("    - 验证补丁文件的有效性");
            Console.WriteLine();
            Console.WriteLine("  StateMachinePatchTool batch <PatchesDirectory>");
            Console.WriteLine("    - 批量转换目录中的所有补丁文件");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  StateMachinePatchTool scan \"MechCaller.MechConsole\"");
            Console.WriteLine("  StateMachinePatchTool generate \"MechCaller.MechConsole\" \"GetGizmos\" \"Mechanoid Raid\" \"机械族袭击\" \"output.xml\"");
            Console.WriteLine("  StateMachinePatchTool convert \"original.xml\" \"converted.xml\"");
            Console.WriteLine("  StateMachinePatchTool validate \"patch.xml\"");
            Console.WriteLine("  StateMachinePatchTool batch \"C:\\Mods\\MyMod\\Patches\"");
        }

        static void ScanStateMachines(string typeName)
        {
            Console.WriteLine($"正在扫描类型: {typeName}");
            Console.WriteLine();

            var stateMachines = StateMachinePatchGenerator.ScanStateMachineMethods(typeName);

            if (stateMachines.Count == 0)
            {
                Console.WriteLine("未找到状态机方法。");
                return;
            }

            Console.WriteLine($"找到 {stateMachines.Count} 个状态机方法:");
            Console.WriteLine();

            foreach (var sm in stateMachines)
            {
                Console.WriteLine($"方法名: {sm.MethodName}");
                Console.WriteLine($"基础类型: {sm.BaseType.FullName}");
                Console.WriteLine($"状态机类型: {sm.StateMachineTypeName}");
                Console.WriteLine($"完整路径: {sm}");
                Console.WriteLine("---");
            }
        }

        static void GeneratePatch(string baseType, string method, string findText, string replaceText, string outputFile)
        {
            Console.WriteLine($"正在生成补丁:");
            Console.WriteLine($"  基础类型: {baseType}");
            Console.WriteLine($"  方法名: {method}");
            Console.WriteLine($"  查找文本: {findText}");
            Console.WriteLine($"  替换文本: {replaceText}");
            Console.WriteLine($"  输出文件: {outputFile}");
            Console.WriteLine();

            var translations = new Dictionary<string, string>
            {
                { findText, replaceText }
            };

            var success = StateMachinePatchGenerator.GenerateStateMachinePatch(
                baseType, method, translations, outputFile);

            if (success)
            {
                Console.WriteLine("✓ 补丁生成成功!");
                Console.WriteLine($"文件已保存到: {outputFile}");
                
                // 显示生成的内容
                if (File.Exists(outputFile))
                {
                    Console.WriteLine();
                    Console.WriteLine("生成的补丁内容:");
                    Console.WriteLine("---");
                    Console.WriteLine(File.ReadAllText(outputFile));
                    Console.WriteLine("---");
                }
            }
            else
            {
                Console.WriteLine("✗ 补丁生成失败!");
            }
        }

        static void ConvertPatch(string inputFile, string outputFile)
        {
            Console.WriteLine($"正在转换补丁文件:");
            Console.WriteLine($"  输入文件: {inputFile}");
            Console.WriteLine($"  输出文件: {outputFile ?? "(自动生成)"}");
            Console.WriteLine();

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("✗ 输入文件不存在!");
                return;
            }

            TranslationFrameworkMod.ConvertToStateMachinePatch(inputFile, outputFile);
            Console.WriteLine("✓ 转换完成!");
        }

        static void ValidatePatch(string xmlFile)
        {
            Console.WriteLine($"正在验证补丁文件: {xmlFile}");
            Console.WriteLine();

            var result = StateMachinePatchGenerator.ValidateStateMachinePatch(xmlFile);

            if (result.IsValid)
            {
                Console.WriteLine("✓ 补丁验证成功!");
                Console.WriteLine($"  有效操作: {result.ValidOperations}");
                Console.WriteLine($"  无效操作: {result.InvalidOperations}");
                Console.WriteLine($"  消息: {result.SuccessMessage}");
            }
            else
            {
                Console.WriteLine("✗ 补丁验证失败!");
                Console.WriteLine($"  错误: {result.ErrorMessage}");
            }

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("警告:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"  - {warning}");
                }
            }
        }

        static void BatchConvert(string patchesDirectory)
        {
            Console.WriteLine($"正在批量转换目录: {patchesDirectory}");
            Console.WriteLine();

            if (!Directory.Exists(patchesDirectory))
            {
                Console.WriteLine("✗ 目录不存在!");
                return;
            }

            TranslationFrameworkMod.ConvertAllPatchesToStateMachine(patchesDirectory);
            Console.WriteLine("✓ 批量转换完成!");
        }
    }
}
