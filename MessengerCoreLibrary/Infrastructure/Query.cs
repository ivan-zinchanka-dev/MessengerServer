namespace MessengerCoreLibrary.Infrastructure;

public class Query
{
    private const int HeaderLength = 3;
    private const string NewLine = "\n";
    
    public QueryHeader Header { get; private set; }
    public string JsonDataString { get; private set; }
    
    public static Query FromRawLine(string rawLine)
    {
        QueryHeader header = (QueryHeader) Convert.ToByte(rawLine.Substring(0, HeaderLength));
        
        string jsonDataString = rawLine.Remove(0, HeaderLength).Replace(NewLine, string.Empty);

        return new Query(header, jsonDataString);
    }
    
    private static string ToByteNotation(QueryHeader header)
    {
        string result = ((byte)header).ToString();
        int emptyCharsCount = HeaderLength - result.Length;

        if (emptyCharsCount > 0)
        {
            result = result.Insert(0, new string('0', emptyCharsCount));
        }

        return result;
    }
    
    public Query(QueryHeader header, string jsonDataString = "")
    {
        Header = header;
        JsonDataString = jsonDataString;
    }
    
    public override string ToString()
    {
        return ToByteNotation(Header) + JsonDataString + NewLine;
    }
}