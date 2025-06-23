using System;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// Universal Translation Framework 主Mod类
    /// </summary>
    public class UTF_Mod : Mod
    {
        /// <summary>
        /// 设置实例
        /// </summary>
        private UTF_Settings settings;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="content">Mod内容</param>
        public UTF_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<UTF_Settings>();
        }

        /// <summary>
        /// 设置页面标题
        /// </summary>
        public override string SettingsCategory()
        {
            return "Universal Translation Framework";
        }
        /// <summary>
        /// 绘制设置界面
        /// </summary>
        /// <param name="inRect">绘制区域</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // 标题和版本信息
            // listingStandard.Label("<b><size=16>Universal Translation Framework Settings</size></b>");
            listingStandard.Gap(12f);

            // 框架状态信息
            DrawFrameworkStatus(listingStandard);
            listingStandard.Gap(12f);

            // 日志设置部分
            DrawLogSettings(listingStandard);
            listingStandard.Gap(12f);

            // 性能设置部分
            DrawPerformanceSettings(listingStandard);
            listingStandard.Gap(12f);

            // 调试设置部分
            DrawDebugSettings(listingStandard);
            listingStandard.Gap(12f);

            // 操作按钮部分
            DrawActionButtons(listingStandard);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// 绘制框架状态信息
        /// </summary>
        private void DrawFrameworkStatus(Listing_Standard listingStandard)
        {
            listingStandard.Label("<b>Framework Status</b>");
            
            var isInitialized = TranslationFrameworkMod.IsInitialized;
            var patchCount = TranslationFrameworkMod.LoadedPatchesCount;
            var cacheStats = TranslationFrameworkMod.GetCacheStatistics();
            
            listingStandard.Label($"Status: {(isInitialized ? "<color=green>Initialized</color>" : "<color=red>Not Initialized</color>")}");
            listingStandard.Label($"Loaded Patches: {patchCount}");
            listingStandard.Label($"Cache: {cacheStats}");
            
            if (UTF_Settings.enablePatchStatistics)
            {
                var translationStats = TranslationCache.GetCacheStats();
                listingStandard.Label($"Translation Cache: {translationStats}");
            }
        }

        /// <summary>
        /// 绘制日志设置
        /// </summary>
        private void DrawLogSettings(Listing_Standard listingStandard)
        {
            listingStandard.Label("<b>Logging Settings</b>");
              // 日志级别选择
            if (listingStandard.ButtonTextLabeled("Log Level:", UTF_Settings.logLevel.ToString()))
            {
                var options = Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>().ToList();
                var floatMenuOptions = options.Select(level => new FloatMenuOption(
                    level.ToString() + GetLogLevelDescription(level),
                    () => UTF_Settings.SetLogLevel(level)
                )).ToList();
                
                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
            }
            
            listingStandard.Label("Current level: " + GetLogLevelDescription(UTF_Settings.logLevel));        }

        /// <summary>
        /// 绘制性能设置
        /// </summary>
        private void DrawPerformanceSettings(Listing_Standard listingStandard)
        {
            listingStandard.Label("<b>Performance Settings</b>");
            
            listingStandard.CheckboxLabeled("Enable Performance Monitoring", ref UTF_Settings.enablePerformanceMonitoring,
                "Monitor and log transpiler performance (may slightly impact performance)");
            
            listingStandard.CheckboxLabeled("Enable Patch Statistics", ref UTF_Settings.enablePatchStatistics,
                "Show patch application statistics");
            
            // 暂时注释掉的设置项
            /*
            listingStandard.CheckboxLabeled("Force Safe Mode", ref UTF_Settings.forceSafeMode,
                "Always use safe transpiler mode (slower but more stable)");
            
            // 并行线程数设置
            var threadsRect = listingStandard.GetRect(Text.LineHeight);
            var threadsLabelRect = new Rect(threadsRect.x, threadsRect.y, threadsRect.width * 0.7f, threadsRect.height);
            var threadsFieldRect = new Rect(threadsRect.x + threadsRect.width * 0.75f, threadsRect.y, threadsRect.width * 0.2f, threadsRect.height);
            
            Widgets.Label(threadsLabelRect, "Max Parallel Threads (0 = Auto):");
            var threadsString = UTF_Settings.maxParallelThreads.ToString();
            threadsString = Widgets.TextField(threadsFieldRect, threadsString);
            
            if (int.TryParse(threadsString, out int threadsValue) && threadsValue >= 0 && threadsValue <= 32)
            {
                UTF_Settings.maxParallelThreads = threadsValue;
            }
            */
        }
        /// <summary>
        /// 绘制调试设置
        /// </summary>
        private void DrawDebugSettings(Listing_Standard listingStandard)
        {
            listingStandard.Label("<b>Debug Settings</b>");

            // 暂时注释掉的设置项
            /*
            listingStandard.CheckboxLabeled("Enable State Machine Detail Logs", ref UTF_Settings.enableStateMachineDetailLogs,
                "Show detailed logs for state machine detection and conversion");
            */

            listingStandard.Label("Debug features are currently disabled for simplicity.");
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons(Listing_Standard listingStandard)
        {
            listingStandard.Label("<b>Actions</b>");
              if (listingStandard.ButtonText("Reset to Defaults"))
            {
                UTF_Settings.ResetToDefaults();
                Messages.Message("Settings reset to defaults", MessageTypeDefOf.NeutralEvent);
            }
            
            if (listingStandard.ButtonText("Force Framework Reinitialization"))
            {
                TranslationFrameworkMod.ForceInitialize();
                Messages.Message("Framework reinitialization attempted", MessageTypeDefOf.NeutralEvent);
            }
            
            if (listingStandard.ButtonText("Clear Translation Cache"))
            {
                TranslationCache.ClearCache();
                Messages.Message("Translation cache cleared", MessageTypeDefOf.NeutralEvent);
            }
            
            if (listingStandard.ButtonText("Print Framework Statistics"))
            {
                TranslationDebugTools.PrintFrameworkStats();
                Messages.Message("Framework statistics printed to log", MessageTypeDefOf.NeutralEvent);
            }

            if (listingStandard.ButtonText("Run Performance Benchmark"))
            {
                TranslationDebugTools.BenchmarkTranslations();
                Messages.Message("Performance benchmark completed", MessageTypeDefOf.NeutralEvent);
            }
        }

        /// <summary>
        /// 获取日志级别描述
        /// </summary>
        private string GetLogLevelDescription(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.None:
                    return " (No logs)";
                case LogLevel.Error:
                    return " (Errors only)";
                case LogLevel.Warning:
                    return " (Errors + Warnings)";
                case LogLevel.Message:
                    return " (Errors + Warnings + Messages)";
                case LogLevel.Debug:
                    return " (All logs including debug)";
                default:
                    return "";
            }
        }
    }
}
