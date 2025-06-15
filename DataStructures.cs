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

        /// <summary>Translation context or notes</summary>
        public string Context { get; set; }

        /// <summary>Creation timestamp</summary>
        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public override string ToString()
        {
            var regexIndicator = IsRegex ? " (Regex)" : "";
            var qualityIndicator = QualityScore < 100 ? $" (Q:{QualityScore})" : "";
            return $"'{OriginalText}' -> '{TranslatedText}'{regexIndicator}{qualityIndicator}";
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
            _cacheMetrics.Clear();
        }

        /// <summary>
        /// Remove translations for a specific method
        /// </summary>
        public static bool RemoveTranslations(string methodId)
        {
            var removed1 = _methodTranslations.TryRemove(methodId, out _);
            var removed2 = _cacheMetrics.TryRemove(methodId, out _);
            return removed1 || removed2;
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
    /// Performance metrics for translation methods
    /// </summary>
    public class PerformanceMetrics
    {
        private long _totalCalls = 0;
        private long _totalTime = 0; // in ticks
        private long _maxTime = 0;
        private long _minTime = long.MaxValue;

        public long TotalCalls => _totalCalls;
        public double AverageTime => _totalCalls > 0 ? (double)_totalTime / _totalCalls / TimeSpan.TicksPerMillisecond : 0;
        public double MaxTime => (double)_maxTime / TimeSpan.TicksPerMillisecond;
        public double MinTime => _minTime == long.MaxValue ? 0 : (double)_minTime / TimeSpan.TicksPerMillisecond;

        public void RecordCall(long elapsedTicks)
        {
            System.Threading.Interlocked.Increment(ref _totalCalls);
            System.Threading.Interlocked.Add(ref _totalTime, elapsedTicks);
            
            // Update max time
            long currentMax = _maxTime;
            while (elapsedTicks > currentMax)
            {
                long original = System.Threading.Interlocked.CompareExchange(ref _maxTime, elapsedTicks, currentMax);
                if (original == currentMax) break;
                currentMax = original;
            }
            
            // Update min time
            long currentMin = _minTime;
            while (elapsedTicks < currentMin)
            {
                long original = System.Threading.Interlocked.CompareExchange(ref _minTime, elapsedTicks, currentMin);
                if (original == currentMin) break;
                currentMin = original;
            }
        }

        public override string ToString()
        {
            return $"Calls: {TotalCalls}, Avg: {AverageTime:F2}ms, Max: {MaxTime:F2}ms, Min: {MinTime:F2}ms";
        }
    }
}