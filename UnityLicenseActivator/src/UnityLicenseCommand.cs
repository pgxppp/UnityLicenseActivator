using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kurukuru;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Support.UI;
using UnityLicenseActivator.Extensions;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。

namespace UnityLicenseActivator
{
    public class UnityLicenseCommand : ConsoleAppBase
    {
        private static readonly string UlfPath = Path.Combine(Directory.GetCurrentDirectory(), "Ulf");
        private WebDriver _driver = null;
        private WebDriverWait _waiter = null;

        [Command("auth-ulf")]
        public async Task RunAsync(
            [Option("e")] string email,
            [Option("p")] string password,
            [Option("a")] string alfFilePath,
            [Option("u")] string ulfFilePath,
            [Option("h")] bool headless = false
            )
        {
            var fullPath = Path.GetFullPath(alfFilePath);
            if (File.Exists(alfFilePath) == false)
                throw new FileNotFoundException(fullPath);
            else
                Console.WriteLine($"Alf: {alfFilePath} -> {fullPath}");

            if (Directory.Exists(UlfPath))
                Directory.Delete(UlfPath, recursive: true);
            Directory.CreateDirectory(UlfPath);

            this._driver = CreateDriver(headless);
            this._waiter = new WebDriverWait(new SystemClock(), this._driver, timeout: TimeSpan.FromSeconds(15.0), sleepInterval: TimeSpan.FromSeconds(0.1));

            try
            {
                await this.LoginAsync(email, password);
                await this.AuthAlfFileAsync(fullPath);
                await this.ChooseLicenseOptionsAsync();
                await this.DownloadUlfAsync(ulfFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                var date = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
                this._driver.ExportNowPngScreenShot(date);
            }
            finally
            {
                this._driver.Quit();
                this._driver.Dispose();
            }
        }
        private static async Task SafetyWaitAsync()
        {
            await Task.Delay(100);
        }
        private async Task OpenUrlAsync(string url)
        {
            await Spinner.StartAsync($"OpenUrl {url}...", (spinner) =>
            {
                this._driver.Navigate().GoToUrl(url);
                spinner.Succeed($"Open {url}.");
                return Task.CompletedTask;
            });
        }

        private async Task LoginAsync(string email, string password)
        {
            await this.OpenUrlAsync("https://license.unity3d.com/manual");
            await Spinner.StartAsync($"Login...", async (spinner) =>
            {
                await SafetyWaitAsync();
                this._waiter.Until(m => m.FindElement(By.Id("conversations_create_session_form_email"))).SendKeys(email);
                await SafetyWaitAsync();
                this._waiter.Until(m => m.FindElement(By.Id("conversations_create_session_form_password"))).SendKeys(password);
                await SafetyWaitAsync();
                try
                {
                    this._waiter.Until(m => m.FindElement(By.Id("onetrust-accept-btn-handler"))).Click();
                    await SafetyWaitAsync();
                }
                catch (Exception) { }
                try
                {
                    this._waiter.Until(m => m.FindElement(By.Name("commit")));
                    await SafetyWaitAsync();
                }
                catch (Exception) { }
                this._waiter.Until(m => m.FindElement(By.Name("commit"))).Submit();

                spinner.Succeed($"Login Succeed.");
            });
        }
        private async Task AuthAlfFileAsync(string licenseFile)
        {
            await Spinner.StartAsync($"Alf Upload...", async (spinner) =>
            {
                await SafetyWaitAsync();
                this._waiter.Until(m => m.FindElement(By.Id("licenseFile"))).SendKeys(licenseFile);
                await SafetyWaitAsync();
                this._waiter.Until(m => m.FindElement(By.Name("commit"))).Submit();

                spinner.Succeed($"Alf Upload Succeed.");
            });
        }
        private async Task ChooseLicenseOptionsAsync()
        {
            await Spinner.StartAsync($"License Options...", async (spinner) =>
            {
                await SafetyWaitAsync();
                // this.waiter.Until(m => m.FindElement(By.Id("type_serial"))).GetParent().Click(); // Pro 
                this._waiter.Until(m => m.FindElement(By.Id("type_personal"))).GetParent().Click();

                await SafetyWaitAsync();
                // this.waiter.Until(m => m.FindElement(By.Id("option1"))).GetParent().Click(); // companyOrOrganizationOver100000Doller
                // this.waiter.Until(m => m.FindElement(By.Id("option2"))).GetParent().Click(); // companyOrOrganizationLess100000Doller
                this._waiter.Until(m => m.FindElement(By.Id("option3"))).GetParent().Click();

                await SafetyWaitAsync();
                // Plus / Pro側へ反応する場合があるので可視要素のみでフィルタリング
                this._waiter.Until(m => m.FindElements(By.Name("commit"))).Where(m => m.Displayed).First().Submit();

                spinner.Succeed($"License Option Selected.");
            });
        }

        private async Task DownloadUlfAsync(string ulfFile)
        {
            await Spinner.StartAsync($"Download Ulf...", async (spinner) =>
            {
                await SafetyWaitAsync();
                this._waiter.Until(m => m.FindElement(By.Name("commit")).Displayed);
                this._driver.FindElement(By.Name("commit")).Submit();

                await Task.Delay(2000);
                this._waiter.Until(m => Directory.GetFiles(UlfPath).Length > 0);

                var ulf = Directory.GetFiles(UlfPath).First();
                File.Move(ulf, ulfFile, overwrite: true);

                // wait file downloaded
                await Task.Delay(1000);
                spinner.Succeed("Download Ulf Succeed.");
            });
            Console.WriteLine($"UlfFile: {Path.GetFullPath(ulfFile)}");
        }

        private static ChromiumDriver CreateDriver(bool isHeadless)
        {
            // install chrome driver
            var chromeDriverPath = new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
            // Note: https://github.com/rosolko/WebDriverManager.Net/issues/199
            var options = new ChromeOptions();
            var directory = Path.GetDirectoryName(chromeDriverPath);

            // ファイルダウンロード時の保存先確認ウインドウを抑制する
            options.AddUserProfilePreference("disable-popup-blocking", "true");
            options.AddUserProfilePreference("download.default_directory", UlfPath);

            //ブラウザ非表示
            if (isHeadless)
            {
                // service.HideCommandPromptWindow = true;
                options.AddArgument("--headless");
                options.AddArgument("--window-position=-32000,-32000");
            }
            options.AddArgument("--no-sandbox");
            options.AddArgument("--user-agent=unity-license-acitvator");

            var driver = new ChromeDriver(directory, options);

            return driver;
        }
    }
}
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
