namespace PlagiarismCheckerWorkerService
{
    public static class Settings
    {
        public static string Server { get; } = "moss.stanford.edu";
        public static int Port { get; } = 7690;

        public static string MossOption { get; } = "moss";
        public static string DirectoryOption { get; } = "directory";
        public static string ExperimentalOption { get; } = "X";
        public static string MaxMatchesOption { get; } = "maxmatches";
        public static string ShowOption { get; } = "show";
        public static string EndOption { get; } = "end";
        public static string Moss_Request_URI_Error { get; } = "error";
    }
}
