using System;
using System.Collections.Generic;

namespace GDF.Attributes;

public class StackableList<T> : StackableValue<List<T>, StackableList<T>.ListModification>
{
    public override List<T> CurrentValue => GetComputed().Param.ElementList;

    private readonly List<T> _list = new();

    protected override ListModification GetInitialParam()
    {
        _list.Clear();
        return new ListModification(0, _list);
    }

    protected override Modification Reduce(in Modification current, in Modification incoming, float weight)
    {
        if (weight == 0) return current;

        if (incoming.Param.Group < current.Param.Group) return current;
        if (incoming.Param.Group > current.Param.Group) current.Param.ElementList.Clear();

        current.Param.AddFrom(incoming.Param);

        var result = current;
        result.Param.Group = incoming.Param.Group;
        return result;
    }

    public struct ListModification
    {
        public int Group;
        public T SingleElement;
        public List<T> ElementList;

        public ListModification(int group)
        {
            Group = group;
        }

        public ListModification(int group, T singleElement)
        {
            Group = group;
            SingleElement = singleElement;
        }

        public ListModification(int group, List<T> elementList)
        {
            Group = group;
            ElementList = elementList;
        }

        public readonly void AddFrom(ListModification incoming)
        {
            if (incoming.ElementList != null && incoming.ElementList != this.ElementList)
            {
                this.ElementList.AddRange(incoming.ElementList);
            }

            if (incoming.SingleElement != null)
            {
                this.ElementList.Add(incoming.SingleElement);
            }
        }
    }
}

public static class StackableListExtensions
{
    public static void Set<T>(this StackableValue<List<T>, StackableList<T>.ListModification> target, StackableValueToken token, int group, T value)
    {
        target.Modify(token, new StackableList<T>.ListModification(group)
        {
            SingleElement = value
        });
    }
    
    public static StackableValueTokenTyped<List<T>, StackableList<T>.ListModification> Set<T>(this StackableValueTokenTyped<List<T>, StackableList<T>.ListModification> token, int group, T value)
    {
        token.Target.Set(token, group, value);
        return token;
    }
    
    public static void Set<T>(this StackableValue<List<T>, StackableList<T>.ListModification> target, StackableValueToken token, int group, List<T> values)
    {
        target.Modify(token, new StackableList<T>.ListModification(group, values));
    }
    
    public static StackableValueTokenTyped<List<T>, StackableList<T>.ListModification> Set<T>(this StackableValueTokenTyped<List<T>, StackableList<T>.ListModification> token, int group, List<T> values)
    {
        token.Target.Set(token, group, values);
        return token;
    }
}