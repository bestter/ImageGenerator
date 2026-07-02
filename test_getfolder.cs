using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", "/tmp/non_existent_fake_xdg");
        Environment.SetEnvironmentVariable("LOCALAPPDATA", "/tmp/non_existent_fake_xdg"); // Windows
        Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }
}
