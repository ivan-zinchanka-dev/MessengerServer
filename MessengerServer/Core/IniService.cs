using System.Runtime.InteropServices;
using System.Text;

namespace MessengerClient.Core.Services;

public class IniService
{
    private const int MaxDataSize = 1024;
    private readonly string _path;

    public IniService(string path)
    {
        _path = path;
    }

    public string GetString(string section, string key)
    {
        StringBuilder buffer = new StringBuilder(MaxDataSize);
        GetPrivateString(section, key, null, buffer, MaxDataSize, _path);
        return buffer.ToString();
    }
    
    public void SetString(string section, string key, string line)
    {
        WritePrivateString(section, key, line, _path);
    }
    
    [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
    private static extern int GetPrivateString(string section, string key, string def, 
        StringBuilder buffer, int size, string path);

    [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
    private static extern int WritePrivateString(string section, string key, string str, string path);

}