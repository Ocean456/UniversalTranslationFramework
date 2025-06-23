using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// Universal Translation Framework 设置类
    /// </summary>
    public class UTF_Settings : ModSettings
    {
        /// <summary>
        /// 日志显示级别
        /// </summary>
        public static LogLevel logLevel = LogLevel.None;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public static bool enablePerformanceMonitoring = false;

        /// <summary>
        /// 是否启用补丁统计信息
        /// </summary>
        public static bool enablePatchStatistics = true;

        // 以下功能暂时注释掉，按需启用
        /// <summary>
        /// 是否启用详细的状态机检测日志
        /// </summary>
        //public static bool enableStateMachineDetailLogs = false;

        /// <summary>
        /// 是否启用安全模式（强制使用安全转译器）
        /// </summary>
        //public static bool forceSafeMode = false;

        /// <summary>        
        /// 最大并行处理线程数（0表示自动）
        /// </summary>
        //public static int maxParallelThreads = 0;
        
        /// <summary>
        /// 保存设置
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref logLevel, "logLevel", LogLevel.None);
            Scribe_Values.Look(ref enablePerformanceMonitoring, "enablePerformanceMonitoring", false);
            Scribe_Values.Look(ref enablePatchStatistics, "enablePatchStatistics", true);
            
            // 暂时注释掉的设置项
            //Scribe_Values.Look(ref enableStateMachineDetailLogs, "enableStateMachineDetailLogs", false);
            //Scribe_Values.Look(ref forceSafeMode, "forceSafeMode", false);
            //Scribe_Values.Look(ref maxParallelThreads, "maxParallelThreads", 0);

            // 同步日志级别到UTF_Log
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                UTF_Log.CurrentLogLevel = logLevel;
            }

            base.ExposeData();
        }

        /// <summary>
        /// 设置日志级别并同步到UTF_Log
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            logLevel = level;
            UTF_Log.CurrentLogLevel = level;
        }

        /// <summary>
        /// 检查是否应该输出指定级别的日志
        /// </summary>
        /// <param name="level">要检查的日志级别</param>
        /// <returns>是否应该输出</returns>
        public static bool ShouldLog(LogLevel level)
        {
            return (int)level <= (int)logLevel;
        }

        /// <summary>
        /// 根据设置输出日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="level">日志级别</param>
        public static void LogMessage(string message, LogLevel level = LogLevel.Message)
        {
            if (!ShouldLog(level))
                return;

            switch (level)
            {
                case LogLevel.Error:
                    Log.Error(message);
                    break;
                case LogLevel.Warning:
                    Log.Warning(message);
                    break;
                case LogLevel.Message:
                    Log.Message(message);
                    break;
                case LogLevel.Debug:
                    Log.Message($"[DEBUG] {message}");
                    break;
            }
        }

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public static void ResetToDefaults()
        {
            logLevel = LogLevel.None;
            enablePerformanceMonitoring = false;
            enablePatchStatistics = true;
              // 暂时注释掉的设置项
            //enableStateMachineDetailLogs = false;
            //forceSafeMode = false;
            //maxParallelThreads = 0;
        }

        /// <summary>
        /// 获取设置摘要文本
        /// </summary>
        /// <returns>设置摘要</returns>
        public static string GetSettingsSummary()
        {
            return $"Log Level: {logLevel}, Performance Monitoring: {enablePerformanceMonitoring}, Statistics: {enablePatchStatistics}";
            
            // 暂时注释掉的设置项
            //return $"Log Level: {logLevel}, Performance Monitoring: {enablePerformanceMonitoring}, " +
            //       $"Safe Mode: {forceSafeMode}, Statistics: {enablePatchStatistics}, " +
            //       $"Max Threads: {(maxParallelThreads == 0 ? "Auto" : maxParallelThreads.ToString())}";
        }
    }
}
