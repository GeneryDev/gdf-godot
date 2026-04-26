namespace GDF.Data;

public interface ITextPreprocessor
{
    public bool RequiresProcessing(string input, IDataQueryOptions options);
    public string Process(string input, IDataQueryOptions options);
}