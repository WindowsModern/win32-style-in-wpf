using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

class Dump
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Assembly asm = Assembly.LoadFrom(args[0]);
            Uri uri = new Uri("/PresentationFramework.Theme;component/theme.base.xaml", UriKind.Relative);
            object component = Application.LoadComponent(uri);
            using (FileStream stream = File.Create(args[1]))
                XamlWriter.Save(component, stream);
        }
        catch (Exception ex)
        {
            File.WriteAllText(args[1] + ".error.txt", ex.ToString());
        }
    }
}