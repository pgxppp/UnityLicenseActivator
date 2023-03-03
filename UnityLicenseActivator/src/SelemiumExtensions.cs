using OpenQA.Selenium;
using System;
using System.IO;

namespace UnityLicenseActivator.Extensions
{
    public static class SelemiumExtensions
    {
        public static IWebElement GetParent(this IWebElement node)
        {
            return node.FindElement(By.XPath(".."));
        }

        public static void ExportNowPngScreenShot(this WebDriver driver, string fileNameWithoutExtension)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileNameWithoutExtension) + ".png";
            driver.GetScreenshot().SaveAsFile(filePath, ScreenshotImageFormat.Png);
            Console.WriteLine($"Export ScreenShot to {filePath}");
        }
    }
}
