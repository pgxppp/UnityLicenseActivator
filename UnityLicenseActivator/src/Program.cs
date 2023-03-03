using UnityLicenseActivator;

// Kurukuru
System.Console.OutputEncoding = System.Text.Encoding.UTF8;

var app = ConsoleApp.Create(args);
app.AddCommands<UnityLicenseCommand>();
app.Run();

