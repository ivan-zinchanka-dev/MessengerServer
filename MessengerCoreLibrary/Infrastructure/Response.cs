namespace MessengerCoreLibrary.Infrastructure;

public class Response
{
    private const string NewLine = "\n";
    public string JsonDataString { get; private set; }

    public static Response FromRawLine(string rawLine)
    {
        string jsonDataString = rawLine.Replace(NewLine, string.Empty);
        return new Response(jsonDataString);
    }
    
    public Response(string jsonDataString)
    {
        JsonDataString = jsonDataString;
    }
    
    public override string ToString()
    {
        return JsonDataString + NewLine;
    }
}