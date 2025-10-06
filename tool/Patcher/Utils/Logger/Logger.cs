static class Logger
{
    public static Verbose Verbose = Verbose.Phase | Verbose.Task | Verbose.Operation;

    static int tabs;

    static string ApplyTabsForLine(string line) => new string(' ', tabs) + line;

    static void WriteLine(string line, Verbose verbose)
    {
        if (Verbose.HasFlag(verbose))
            Console.WriteLine(ApplyTabsForLine(line));
    }

    public static void PushTab() => tabs++;

    public static void PopTab() => tabs--;

    public static void PrintGlobalPhase(string line) => WriteLine($"## {line}", Verbose.Phase);

    public static void PrintLocalPhase(string line) => WriteLine($"# {line}", Verbose.Phase);

    public static void PrintTask(string line) => WriteLine($"> {line}", Verbose.Task);

    public static void PrintOperation(string line) => WriteLine($"> {line}", Verbose.Operation);

    public static void PrintComment(string line) => WriteLine($"; {line}", Verbose.Comment);

    public static void NewLine(Verbose verbose) => WriteLine(string.Empty, verbose);
}