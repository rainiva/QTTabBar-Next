namespace QTTabBarLib {
    internal partial class ExplorerController {
        private static string GetCommandLine() {
            return ExplorerCommandDispatcher.GetCommandLine();
        }

        private static string GetNameToSelectFromCommandLineArg(string value) {
            return ExplorerCommandDispatcher.GetNameToSelectFromCommandLineArg(value);
        }

        private static bool TryParseCommandlineParams(string param, out string path, out string selection) {
            return ExplorerCommandDispatcher.TryParseCommandlineParams(param, out path, out selection);
        }
    }
}
