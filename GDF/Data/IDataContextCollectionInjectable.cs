using System.Collections.Generic;

namespace GDF.Data;

public interface IDataContextCollectionInjectable
{
    public void InjectCollection(List<IDataContext> collection);
}