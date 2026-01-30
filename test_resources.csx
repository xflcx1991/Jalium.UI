#r "samples/Jalium.UI.Gallery/bin/Release/net10.0-windows/Jalium.UI.Gallery.dll"
using System.Reflection;
var asm = Assembly.LoadFrom("samples/Jalium.UI.Gallery/bin/Release/net10.0-windows/Jalium.UI.Gallery.dll");
foreach (var name in asm.GetManifestResourceNames())
{
    Console.WriteLine(name);
}
