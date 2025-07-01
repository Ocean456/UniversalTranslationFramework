using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Verse;

namespace UniversalTranslationFramework
{
    /// <summary>
    /// Translation patch data structure
    /// </summary>
    public class TranslationPatch
    {
        /// <summary>Source mod</summary>
        public ModContentPack SourceMod { get; set; }
        
        /// <summary>Source file path</summary>
        public string SourceFile { get; set; }
        
        /// <summary>Target assembly name (optional)</summary>
        public string TargetAssembly { get; set; }
        
        /// <summary>Target type name</summary>
        public string TargetTypeName { get; set; }
        
        /// <summary>Target method name</summary>
        public string TargetMethodName { get; set; }
        
        /// <summary>List of string translations</summary>
        public List<StringTranslation> Translations { get; set; } = new List<StringTranslation>();

        /// <summary>Patch priority (higher numbers are applied later)</summary>
        public int Priority { get; set; } = 0;

        /// <summary>Whether this patch is enabled</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Creation timestamp</summary>
        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"[{SourceMod?.Name}] {TargetTypeName}.{TargetMethodName} ({Translations.Count} translations, Priority: {Priority})";
        }

        public override bool Equals(object obj)
        {
            if (obj is TranslationPatch other)
            {
                return TargetTypeName == other.TargetTypeName && 
                       TargetMethodName == other.TargetMethodName &&
                       SourceFile == other.SourceFile;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // 兼容性哈希计算
            int hash = 17;
            hash = hash * 23 + (TargetTypeName?.GetHashCode() ?? 0);
            hash = hash * 23 + (TargetMethodName?.GetHashCode() ?? 0);
            hash = hash * 23 + (SourceFile?.GetHashCode() ?? 0);
            return hash;
        }
    }

    /// <summary>
    /// Single string translation rule
    /// </summary>
    public class StringTranslation
    {
        /// <summary>Original text</summary>
        public string OriginalText { get; set; }
        
        /// <summary>Translated text</summary>
        public string TranslatedText { get; set; }

        /// <summary>Translation quality score (0-100)</summary>
        public int QualityScore { get; set; } = 100;

        /// <summary>Whether this translation uses regex matching</summary>
        public bool IsRegex { get; set; } = false;

        /// <summary>Whether this translation is for format strings with placeholders</summary>
        public bool IsFormatString { get; set; } = false;

        /// <summary>Pattern for matching format strings (regex or wildcard)</summary>
        public string Pattern { get; set; }

        /// <summary>Translation context or notes</summary>
        public string Context { get; set; }

        /// <summary>Creation timestamp</summary>
        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public override string ToString()
        {
            var regexIndicator = IsRegex ? " (Regex)" : "";
            var formatIndicator = IsFormatString ? " (Format)" : "";
            var qualityIndicator = QualityScore < 100 ? $" (Q:{QualityScore})" : "";
            return $"'{OriginalText}' -> '{TranslatedText}'{regexIndicator}{formatIndicator}{qualityIndicator}";
        }

        public override bool Equals(object obj)
        {
            if (obj is StringTranslation other)
            {
                return OriginalText == other.OriginalText && TranslatedText == other.TranslatedText;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // 兼容性哈希计算
            int hash = 17;
            hash = hash * 23 + (OriginalText?.GetHashCode() ?? 0);
            hash = hash * 23 + (TranslatedText?.GetHashCode() ?? 0);
            return hash;
        }
    }

    /// <summary>
    /// Translation cache manager (compatible version without ImmutableDictionary)
    /// </summary>
    public static class TranslationCache
    {
        private static readonly ConcurrentDictionary<string, ReadOnlyDictionary<string, string>> _methodTranslations 
            = new ConcurrentDictionary<string, ReadOnlyDictionary<string, string>>();

        private static readonly ConcurrentDictionary<string, List<StringTranslation>> _formatStringPatterns
            = new ConcurrentDictionary<string, List<StringTranslation>>();

        private static readonly ConcurrentDictionary<string, CacheMetrics> _cacheMetrics
            = new ConcurrentDictionary<string, CacheMetrics>();

        /// <summary>
        /// Register string translation mappings for a specific method.
        /// </summary>
        /// <param name="methodId">Method identifier (format: FullTypeName.MethodName)</param>
        /// <param name="translations">String translation mappings</param>
        public static void RegisterTranslations(string methodId, Dictionary<string, string> translations)
        {
            if (translations == null || translations.Count == 0)
                return;

            // 创建只读副本以确保线程安全
            var readOnlyTranslations = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(translations));
            
            _methodTranslations[methodId] = readOnlyTranslations;
            _cacheMetrics[methodId] = new CacheMetrics();
        }

        /// <summary>
        /// Register format string patterns for a specific method.
        /// </summary>
        /// <param name="methodId">Method identifier</param>
        /// <param name="formatTranslations">List of format string translations</param>
        public static void RegisterFormatPatterns(string methodId, List<StringTranslation> formatTranslations)
        {
            if (formatTranslations == null || formatTranslations.Count == 0)
                return;

            var patterns = formatTranslations.Where(t => t.IsFormatString || t.IsRegex).ToList();
            if (patterns.Count > 0)
            {
                _formatStringPatterns[methodId] = patterns;
            }
        }

        /// <summary>
        /// Get the string translation mappings for a specific method.
        /// </summary>
        /// <param name="methodId">Method identifier</param>
        /// <returns>Translation mappings, or null if not found</returns>
        public static ReadOnlyDictionary<string, string> GetTranslations(string methodId)
        {
            if (_methodTranslations.TryGetValue(methodId, out var translations))
            {
                // Update cache metrics
                if (_cacheMetrics.TryGetValue(methodId, out var metrics))
                {
                    metrics.IncrementHit();
                }
                return translations;
            }

            // Update cache metrics for miss
            if (_cacheMetrics.TryGetValue(methodId, out var missMetrics))
            {
                missMetrics.IncrementMiss();
            }

            return null;
        }

        /// <summary>
        /// Get format string patterns for a specific method.
        /// </summary>
        /// <param name="methodId">Method identifier</param>
        /// <returns>List of format string translations, or null if not found</returns>
        public static List<StringTranslation> GetFormatPatterns(string methodId)
        {
            return _formatStringPatterns.TryGetValue(methodId, out var patterns) ? patterns : null;
        }

        /// <summary>
        /// Try to match a string against format patterns and return translated result.
        /// </summary>
        /// <param name="methodId">Method identifier</param>
        /// <param name="inputString">String to match</param>
        /// <returns>Translated string if pattern match found, null otherwise</returns>
        public static string TryFormatPatternMatch(string methodId, string inputString)
        {
            var patterns = GetFormatPatterns(methodId);
            if (patterns == null || patterns.Count == 0)
                return null;

            foreach (var pattern in patterns)
            {
                if (pattern.IsFormatString)
                {
                    var result = FormatStringUtils.TryMatchAndTranslate(inputString, pattern.OriginalText, pattern.TranslatedText);
                    if (result != null)
                        return result;
                }
                else if (pattern.IsRegex && !string.IsNullOrEmpty(pattern.Pattern))
                {
                    try
                    {
                        var regex = new System.Text.RegularExpressions.Regex(pattern.Pattern);
                        if (regex.IsMatch(inputString))
                        {
                            return regex.Replace(inputString, pattern.TranslatedText);
                        }
                    }
                    catch
                    {
                        // Ignore regex errors
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Check if translation mappings exist for a given method.
        /// </summary>
        /// <param name="methodId">Method identifier</param>
        /// <returns>True if translation mappings exist</returns>
        public static bool HasTranslations(string methodId)
        {
            return _methodTranslations.ContainsKey(methodId);
        }

        /// <summary>
        /// Get cache statistics.
        /// </summary>
        public static string GetCacheStats()
        {
            var totalMethods = _methodTranslations.Count;
            var totalTranslations = _methodTranslations.Values.Sum(t => t.Count);
            var totalHits = _cacheMetrics.Values.Sum(m => m.Hits);
            var totalMisses = _cacheMetrics.Values.Sum(m => m.Misses);
            var hitRate = totalHits + totalMisses > 0 ? (double)totalHits / (totalHits + totalMisses) * 100 : 0;
            
            return $"Cached {totalMethods} methods with {totalTranslations} string translations " +
                   $"(Hit rate: {hitRate:F1}%, {totalHits} hits, {totalMisses} misses)";
        }

        /// <summary>
        /// Get detailed cache metrics
        /// </summary>
        public static Dictionary<string, CacheMetrics> GetDetailedMetrics()
        {
            return _cacheMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone());
        }

        /// <summary>
        /// Clear all caches (used for testing or reloading).
        /// </summary>
        public static void ClearCache()
        {
            _methodTranslations.Clear();
            _formatStringPatterns.Clear();
            _cacheMetrics.Clear();
        }

        /// <summary>
        /// Remove translations for a specific method
        /// </summary>
        public static bool RemoveTranslations(string methodId)
        {
            var removed1 = _methodTranslations.TryRemove(methodId, out _);
            var removed2 = _formatStringPatterns.TryRemove(methodId, out _);
            var removed3 = _cacheMetrics.TryRemove(methodId, out _);
            return removed1 || removed2 || removed3;
        }

        /// <summary>
        /// Get memory usage estimate in bytes
        /// </summary>
        public static long GetEstimatedMemoryUsage()
        {
            long totalSize = 0;
            
            foreach (var kvp in _methodTranslations)
            {
                totalSize += kvp.Key.Length * 2; // UTF-16 encoding
                foreach (var translation in kvp.Value)
                {
                    totalSize += translation.Key.Length * 2;
                    totalSize += translation.Value.Length * 2;
                }
            }
            
            return totalSize;
        }
    }

    /// <summary>
    /// Cache performance metrics (thread-safe)
    /// </summary>
    public class CacheMetrics
    {
        private long _hits = 0;
        private long _misses = 0;
        private readonly object _lock = new object();

        public long Hits 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _hits; 
                } 
            } 
        }

        public long Misses 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _misses; 
                } 
            } 
        }

        public double HitRate 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    var total = _hits + _misses;
                    return total > 0 ? (double)_hits / total : 0;
                } 
            } 
        }

        public void IncrementHit()
        {
            lock (_lock)
            {
                _hits++;
            }
        }

        public void IncrementMiss()
        {
            lock (_lock)
            {
                _misses++;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _hits = 0;
                _misses = 0;
            }
        }

        public CacheMetrics Clone()
        {
            lock (_lock)
            {
                return new CacheMetrics
                {
                    _hits = _hits,
                    _misses = _misses
                };
            }
        }

        public override string ToString()
        {
            return $"Hits: {Hits}, Misses: {Misses}, Hit Rate: {HitRate:P2}";
        }
    }

    /// <summary>
    /// Format string utilities for pattern matching and placeholder handling
    /// </summary>
    public static class FormatStringUtils
    {
        /// <summary>
        /// Detects if a string contains format placeholders like {0}, {1}, etc.
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if format placeholders are detected</returns>
        public static bool ContainsFormatPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Simple regex to detect {0}, {1}, etc.
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"\{\d+\}");
        }

        /// <summary>
        /// Extracts format placeholder indices from a string
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>List of placeholder indices found</returns>
        public static List<int> ExtractPlaceholderIndices(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<int>();

            var indices = new List<int>();
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\{(\d+)\}");
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int index))
                {
                    if (!indices.Contains(index))
                        indices.Add(index);
                }
            }
            
            indices.Sort();
            return indices;
        }

        /// <summary>
        /// Validates that source and target format strings have compatible placeholders
        /// </summary>
        /// <param name="sourceText">Original format string</param>
        /// <param name="targetText">Translated format string</param>
        /// <returns>True if placeholders are compatible</returns>
        public static bool ValidatePlaceholderCompatibility(string sourceText, string targetText)
        {
            var sourcePlaceholders = ExtractPlaceholderIndices(sourceText);
            var targetPlaceholders = ExtractPlaceholderIndices(targetText);
            
            // Check that all placeholders in source exist in target
            return sourcePlaceholders.All(p => targetPlaceholders.Contains(p));
        }

        /// <summary>
        /// Creates a regex pattern from a format string for matching
        /// </summary>
        /// <param name="formatString">Format string with placeholders</param>
        /// <returns>Regex pattern that can match the format string with any values</returns>
        public static string CreateMatchingPattern(string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
                return string.Empty;

            // Escape special regex characters except our placeholders
            var escaped = System.Text.RegularExpressions.Regex.Escape(formatString);
            
            // Replace escaped placeholders with regex groups
            escaped = System.Text.RegularExpressions.Regex.Replace(escaped, @"\\?\{(\d+)\\?\}", @"(.+?)");
            
            return $"^{escaped}$";
        }

        /// <summary>
        /// Attempts to match a runtime string against a format pattern and extract values
        /// </summary>
        /// <param name="runtimeString">String to match</param>
        /// <param name="formatPattern">Format pattern</param>
        /// <param name="targetFormat">Target format string</param>
        /// <returns>Formatted target string if match successful, null otherwise</returns>
        public static string TryMatchAndTranslate(string runtimeString, string formatPattern, string targetFormat)
        {
            try
            {
                var pattern = CreateMatchingPattern(formatPattern);
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                var match = regex.Match(runtimeString);
                
                if (!match.Success)
                    return null;

                // Extract captured values
                var values = new List<string>();
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    values.Add(match.Groups[i].Value);
                }

                // Apply values to target format
                return string.Format(targetFormat, values.ToArray());
            }
            catch
            {
                return null;
            }
        }
    }
}