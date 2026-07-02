using System;
using System.IO;

class Program {
    static void Main() {
        string dir = Path.Combine(Path.GetTempPath(), "missing_dir_test_123");
        string file = Path.Combine(dir, "file.txt");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);

        try {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) { }
        }
        catch (Exception ex) {
            Console.WriteLine("Exception for Open: " + ex.GetType().Name);
        }
    }
}
