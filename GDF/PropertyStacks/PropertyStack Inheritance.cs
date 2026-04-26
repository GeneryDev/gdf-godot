using System.Collections.Generic;
using GDF.PropertyStacks.Internal;

namespace GDF.PropertyStacks;

public partial class PropertyStack
{
    private readonly List<PropertyStack> _inheritedPropertyStacks = new();
    private readonly Dictionary<string, List<(PropertyStack, int)>> _inheritedStackModCounts = new();
    private readonly Dictionary<string, List<InheritedPropertyCache>> _inheritedPropertyHandleCache = new();
    private readonly List<InheritedPropertyCache> _tempUnusedHandles = new();
    private readonly List<(PropertyStack, int)> _tempInheritedStackModCounts = new();
    
    private void UpdateInheritableProperty(string propertyId, IProperty property)
    {
        // Find which stacks are being inherited from
        _inheritedPropertyStacks.Clear();
        _tempInheritedStackModCounts.Clear();
        var parent = this.GetParent();

        foreach (var otherStack in AllActiveStacks)
        {
            if (!otherStack.HasProperty(propertyId)) continue;
            
            if (otherStack == this) continue;
            var otherParent = otherStack.GetParent();
            if (otherParent == parent) continue;
            if (otherParent.IsAncestorOf(this))
            {
                _inheritedPropertyStacks.Add(otherStack);
                _tempInheritedStackModCounts.Add((otherStack, otherStack.GetModCount(propertyId)));
            }
        }
        
        // Compare mod counts to see if property needs updating
        var inheritedStackModCounts = _inheritedStackModCounts.GetValueOrDefault(propertyId);
        if (inheritedStackModCounts == null) _inheritedStackModCounts[propertyId] = inheritedStackModCounts = new();
        if (_tempInheritedStackModCounts.Count == inheritedStackModCounts.Count)
        {
            var allMatch = true;
            for (int i = 0; i < inheritedStackModCounts.Count; i++)
            {
                if (inheritedStackModCounts[i].Item1 != _tempInheritedStackModCounts[i].Item1)
                {
                    allMatch = false;
                    break;
                }
                if (inheritedStackModCounts[i].Item2 != _tempInheritedStackModCounts[i].Item2)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                //no changes
                return;
            }
        }
        inheritedStackModCounts.Clear();
        inheritedStackModCounts.AddRange(_tempInheritedStackModCounts);
        
        // Refresh handles and remove unused ones
        if (!_inheritedPropertyHandleCache.TryGetValue(propertyId, out var handleCache))
        {
            handleCache = new List<InheritedPropertyCache>();
            _inheritedPropertyHandleCache[propertyId] = handleCache;
        }
        _tempUnusedHandles.Clear();
        _tempUnusedHandles.AddRange(handleCache);
        handleCache.Clear();

        foreach (var otherStack in _inheritedPropertyStacks)
        {
            if (!otherStack._properties.TryGetValue(propertyId, out var otherProperty)) continue;
            foreach (var otherHandle in otherStack._frameHandles)
            {
                if (!otherProperty.ContainsHandle(otherHandle)) continue;

                var inheritingHandle = GetInheritingHandle(_tempUnusedHandles, otherStack, otherHandle, out int index);
                handleCache.Add(_tempUnusedHandles[index]);
                _tempUnusedHandles.RemoveAt(index);
                property.CopyFrom(otherProperty, otherHandle, inheritingHandle);
            }
        }

        foreach (var unusedEntry in _tempUnusedHandles)
        {
            var unusedHandle = unusedEntry.InheritingHandle;
            this.RemoveHandle(ref unusedHandle);
        }
        _tempUnusedHandles.Clear();
    }

    private PropertyFrameHandle GetInheritingHandle(List<InheritedPropertyCache> handleCache, PropertyStack otherStack, PropertyFrameHandle otherHandle, out int index)
    {
        index = -1;

        ulong otherStackId = otherStack.GetInstanceId();

        for (var i = 0; i < handleCache.Count; i++)
        {
            var entry = handleCache[i];
            if (entry.OtherStackId != otherStackId) continue;
            if (entry.OtherHandle != otherHandle.Id) continue;
            index = i;
            return entry.InheritingHandle;
        }

        index = handleCache.Count;
        var inheritingHandle = otherStack.NewHandle("Inherited", otherHandle.Order);
        handleCache.Add(new InheritedPropertyCache()
        {
            OtherStackId = otherStackId,
            OtherHandle = otherHandle.Id,
            InheritingHandle = inheritingHandle
        });
        this.SetFrameValidator(inheritingHandle, new FrameValidator()
        {
            BoundNodeId = otherStack.GetInstanceId()
        });
        return inheritingHandle;
    }

    private struct InheritedPropertyCache
    {
        public ulong OtherStackId;
        public int OtherHandle;
        public PropertyFrameHandle InheritingHandle;
    }
}