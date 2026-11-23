using System.Collections.Generic;

namespace GDF.Attributes.Impl;

public class ListAttribute<T> : Attribute<List<T>, ListModifierChain<T>.ListModification, ListModifierChain<T>>
{
    public override List<T> CurrentValue => GetComputed().Param.ElementList;

    private readonly List<T> _list = new();

    protected override ListModifierChain<T>.ListModification GetInitialParam()
    {
        _list.Clear();
        return new ListModifierChain<T>.ListModification(0, _list);
    }
}

public class ListModifierChain<T> : ModifierChain<ListModifierChain<T>.ListModification>
{
    protected override void Reduce(ref Step current, in Step incoming, float weight)
    {
        if (weight == 0) return;

        if (incoming.Param.Group < current.Param.Group) return;
        if (incoming.Param.Group > current.Param.Group) current.Param.ElementList.Clear();

        current.Param.AddFrom(incoming.Param);

        var result = current;
        result.Param.Group = incoming.Param.Group;
        current = result;
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