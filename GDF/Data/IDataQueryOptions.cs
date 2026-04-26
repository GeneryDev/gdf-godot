namespace GDF.Data;

public interface IDataQueryOptions
{
    public bool SupportsNullOperands => false;
    public int? FontSize => null;
    public bool? BbcodeEnabled => null;
}