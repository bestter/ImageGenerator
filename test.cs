using System;

class Program {
    static void Main() {
        Console.WriteLine(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
    }
}
