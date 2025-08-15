using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace DBADash.ModernUI.Services;

public interface IPerformanceOptimizationService
{
    Task<IEnumerable<T>> LoadDataVirtualizedAsync<T>(Func<int, int, Task<IEnumerable<T>>> dataLoader, int pageSize = 100);
    ObservableCollection<T> CreateVirtualizedCollection<T>(IEnumerable<T> source, int virtualizedThreshold = 10000);
    void OptimizeForLargeDatasets();
    Task<IEnumerable<T>> SearchWithDebounceAsync<T>(IEnumerable<T> source, Func<T, string> searchSelector, string searchTerm, TimeSpan debounceDelay = default);
}

public class PerformanceOptimizationService : IPerformanceOptimizationService
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<string, CancellationTokenSource> _searchOperations = new();

    public PerformanceOptimizationService()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async Task<IEnumerable<T>> LoadDataVirtualizedAsync<T>(Func<int, int, Task<IEnumerable<T>>> dataLoader, int pageSize = 100)
    {
        var result = new List<T>();
        var offset = 0;
        
        while (true)
        {
            var batch = await dataLoader(offset, pageSize);
            var batchList = batch.ToList();
            
            if (!batchList.Any())
                break;
                
            result.AddRange(batchList);
            
            // If we got less than the page size, we've reached the end
            if (batchList.Count < pageSize)
                break;
                
            offset += pageSize;
            
            // Yield control to UI thread periodically
            if (offset % (pageSize * 10) == 0)
            {
                await Task.Delay(1);
            }
        }
        
        return result;
    }

    public ObservableCollection<T> CreateVirtualizedCollection<T>(IEnumerable<T> source, int virtualizedThreshold = 10000)
    {
        var sourceList = source.ToList();
        
        if (sourceList.Count <= virtualizedThreshold)
        {
            // For smaller datasets, return a regular ObservableCollection
            return new ObservableCollection<T>(sourceList);
        }
        
        // For larger datasets, return a virtualized collection
        return new VirtualizedObservableCollection<T>(sourceList);
    }

    public void OptimizeForLargeDatasets()
    {
        // Configure garbage collection for better performance with large datasets
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        
        // Suggest garbage collection if memory usage is high
        var memoryBefore = GC.GetTotalMemory(false);
        if (memoryBefore > 500 * 1024 * 1024) // 500MB threshold
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    public async Task<IEnumerable<T>> SearchWithDebounceAsync<T>(
        IEnumerable<T> source, 
        Func<T, string> searchSelector, 
        string searchTerm, 
        TimeSpan debounceDelay = default)
    {
        if (debounceDelay == default)
            debounceDelay = TimeSpan.FromMilliseconds(300);

        var operationKey = $"{typeof(T).Name}_{searchTerm}";
        
        // Cancel any existing search operation
        if (_searchOperations.ContainsKey(operationKey))
        {
            _searchOperations[operationKey].Cancel();
            _searchOperations.Remove(operationKey);
        }
        
        var cancellationTokenSource = new CancellationTokenSource();
        _searchOperations[operationKey] = cancellationTokenSource;
        
        try
        {
            // Wait for the debounce delay
            await Task.Delay(debounceDelay, cancellationTokenSource.Token);
            
            // Perform the search
            var result = await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return source;
                    
                return source.Where(item => 
                {
                    var searchValue = searchSelector(item);
                    return searchValue?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true;
                });
            }, cancellationTokenSource.Token);
            
            _searchOperations.Remove(operationKey);
            return result;
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled (new search started)
            return Enumerable.Empty<T>();
        }
    }
}

// Virtualized Observable Collection for large datasets
public class VirtualizedObservableCollection<T> : ObservableCollection<T>
{
    private readonly List<T> _allItems;
    private readonly int _pageSize;
    private int _currentPage;
    
    public VirtualizedObservableCollection(IEnumerable<T> source, int pageSize = 1000) : base()
    {
        _allItems = source.ToList();
        _pageSize = pageSize;
        _currentPage = 0;
        
        LoadNextPage();
    }
    
    public bool HasMoreItems => (_currentPage + 1) * _pageSize < _allItems.Count;
    
    public void LoadNextPage()
    {
        if (!HasMoreItems) return;
        
        var startIndex = _currentPage * _pageSize;
        var endIndex = Math.Min(startIndex + _pageSize, _allItems.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            Add(_allItems[i]);
        }
        
        _currentPage++;
    }
    
    public void LoadAllItems()
    {
        Clear();
        foreach (var item in _allItems)
        {
            Add(item);
        }
    }
    
    public void Reset()
    {
        Clear();
        _currentPage = 0;
        LoadNextPage();
    }
}

// Memory monitoring utility
public static class MemoryMonitor
{
    public static long GetCurrentMemoryUsage()
    {
        return GC.GetTotalMemory(false);
    }
    
    public static string GetFormattedMemoryUsage()
    {
        var bytes = GetCurrentMemoryUsage();
        return FormatBytes(bytes);
    }
    
    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }
    
    public static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
    
    public static bool IsMemoryPressureHigh(long thresholdBytes = 500 * 1024 * 1024) // 500MB default
    {
        return GetCurrentMemoryUsage() > thresholdBytes;
    }
}
