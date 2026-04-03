partial class Program
{
    int loggetTabs;

    string GetLoggerTabsString() => new string(' ', loggetTabs);

    void Print(string message) => Console.WriteLine(GetLoggerTabsString() + message);

    void PrintComment(string comment) => Console.WriteLine(GetLoggerTabsString() + "# " + comment);

    void IncreaseLoggerTab() => loggetTabs++;

    void DecreaseLoggerTab() => loggetTabs--;
}