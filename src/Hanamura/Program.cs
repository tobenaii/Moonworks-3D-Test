using MoonWorks;

namespace Hanamura;

internal static class Program
{
    private static void Main()
    {
        var windowCreateInfo = new WindowCreateInfo
        {
            WindowWidth = 1920,
            WindowHeight = 1080,
            WindowTitle = "Hanamura",
            ScreenMode = ScreenMode.Windowed
        };
        var framePacingSettings = FramePacingSettings.CreateUncapped(60);
        var appInfo = new AppInfo("Tobenai", "Hanamura");
        var game = new Hanamura(
            appInfo,
            windowCreateInfo,
            framePacingSettings,
            MoonWorks.Graphics.ShaderFormat.SPIRV
        );
        game.Run();
    }
}