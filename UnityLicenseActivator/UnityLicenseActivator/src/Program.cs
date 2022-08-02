using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Support;
using UnityLicenseActivator;

// Kurukuru
System.Console.OutputEncoding = System.Text.Encoding.UTF8;

var app = ConsoleApp.Create(args);
app.AddCommands<UnityLicenseCommand>();
app.Run();

