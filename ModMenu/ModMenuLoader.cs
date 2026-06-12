namespace ModMenu
{
    public static class ModMenuLoader
    {
        // Writes a plugin log entry when the plugin logger is available
        public static void Log(string message)
        {
            try
            {
                ModMenuPlugin.Instance?.PluginLogger.LogInfo(message);
            }
            catch
            {
            }
        }
    }
}
