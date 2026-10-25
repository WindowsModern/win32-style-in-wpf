using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

class DumpAero
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Assembly asm = Assembly.LoadFrom(args[0]);
            Uri uri = new Uri(args[1], UriKind.Relative);
            object component = Application.LoadComponent(uri);
            using (FileStream stream = File.Create(args[2]))
                XamlWriter.Save(component, stream);
        }
        catch (Exception ex)
        {
            File.WriteAllText(args[2] + ".error.txt", ex.ToString());
        }
    }
}