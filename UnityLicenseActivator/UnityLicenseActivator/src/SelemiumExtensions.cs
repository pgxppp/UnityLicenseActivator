using Kurukuru;
using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityLicenseActivator.Extensions
{
    public static class SelemiumExtensions
    {
        public static IWebElement GetParent(this IWebElement node)
        {
            return node.FindElement(By.XPath(".."));
        }

        public static async Task OpenAsync(this ChromiumDriver driver, string url, int delay, CancellationToken token = default)
        {
            driver.Navigate().GoToUrl(url);
            await Task.Delay(delay, cancellationToken: token);
        }
        public static async Task OpenAsyncWithLog(this ChromiumDriver driver, string url, int delay, CancellationToken token = default)
        {
            await Spinner.StartAsync($"OpenUrl {url}...", async (spinner) =>
            {
                await driver.OpenAsync(url, delay, token);
                spinner.Succeed($"Open {url}.");
            });
        }

        public static void ExportNowPngScreenShot(this ChromiumDriver driver, string fileNameWithoutExtension)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileNameWithoutExtension) + ".png";
            driver.GetScreenshot().SaveAsFile(filePath, ScreenshotImageFormat.Png);
            Console.WriteLine($"Export ScreenShot to {filePath}");
        }
    }
}
