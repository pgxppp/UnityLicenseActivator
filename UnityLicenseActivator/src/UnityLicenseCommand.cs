using Kurukuru;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityLicenseActivator.Extensions;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace UnityLicenseActivator
{
    public class UnityLicenseCommand : ConsoleAppBase
    {
        private static readonly string UlfPath = Path.Combine(Directory.GetCurrentDirectory(), "Ulf");
        private WebDriver driver = null;
        private WebDriverWait waiter = null;

        [Command("auth-ulf")]
        public async Task RunAsync(
            [Option("e")] string email,
            [Option("p")] string password,
            [Option("a")] string alfFilePath,
            [Option("u")] string ulfFilePath
            )
        {
            var fullPath = Path.GetFullPath(alfFilePath);
            Console.WriteLine($"Alf: {alfFilePath} -> {fullPath}");

            if (Directory.Exists(UlfPath))
                Directory.Delete(UlfPath, recursive: true);
            Directory.CreateDirectory(UlfPath);

            this.driver = CreateDriver();
            this.waiter = new WebDriverWait(new SystemClock(), this.driver, timeout: TimeSpan.FromSeconds(15.0), sleepInterval: TimeSpan.FromSeconds(0.1));

            try
            {
                await Login(email, password);
                await AuthAlfFile(fullPath);
                await ChooseLicenseOptions();
                await DownloadUlf(ulfFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                var date = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
                this.driver.ExportNowPngScreenShot(date);
            }
        }
        private async Task SafetyWait()
        {
            await Task.Delay(100);
        }
        private async Task OpenUrl(string url)
        {
            await Spinner.StartAsync($"OpenUrl {url}...", async (spinner) =>
            {
                this.driver.Navigate().GoToUrl(url);
                spinner.Succeed($"Open {url}.");
            });
        }

        private async Task Login(string email, string password)
        {
            await this.OpenUrl("https://license.unity3d.com/manual");
            await Spinner.StartAsync($"Login...", async (spinner) =>
            {
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Id("conversations_create_session_form_email"))).SendKeys(email);
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Id("conversations_create_session_form_password"))).SendKeys(password);
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Id("onetrust-accept-btn-handler"))).Click();
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Name("commit"))).Click();

                spinner.Succeed($"Login Succeed.");
            });
        }
        private async Task AuthAlfFile(string licenseFile)
        {
            await Spinner.StartAsync($"Alf Upload...", async (spinner) =>
            {
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Id("licenseFile"))).SendKeys(licenseFile);
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Name("commit"))).Click();

                spinner.Succeed($"Alf Upload Succeed.");
            });
        }
        private async Task ChooseLicenseOptions()
        {
            await Spinner.StartAsync($"License Options...", async (spinner) =>
            {
                await this.SafetyWait();
                // this.waiter.Until(m => m.FindElement(By.Id("type_serial"))).GetParent().Click(); // Pro 
                this.waiter.Until(m => m.FindElement(By.Id("type_personal"))).GetParent().Click();

                await this.SafetyWait();
                // this.waiter.Until(m => m.FindElement(By.Id("option1"))).GetParent().Click(); // companyOrOrganizationOver100000Doller
                // this.waiter.Until(m => m.FindElement(By.Id("option2"))).GetParent().Click(); // companyOrOrganizationLess100000Doller
                this.waiter.Until(m => m.FindElement(By.Id("option3"))).GetParent().Click();

                await this.SafetyWait();
                // Plus / Pro側へ反応する場合があるので可視要素のみでフィルタリング
                this.waiter.Until(m => m.FindElements(By.Name("commit"))).Where(m => m.Displayed).First().Click();

                spinner.Succeed($"License Option Selected.");
            });
        }

        private async Task DownloadUlf(string ulfFile)
        {
            await Spinner.StartAsync($"Download Ulf...", async (spinner) =>
            {
                await this.SafetyWait();
                this.waiter.Until(m => m.FindElement(By.Name("commit")).Displayed);
                this.driver.FindElement(By.Name("commit")).Click();
                await this.SafetyWait();
                this.waiter.Until(m => Directory.GetFiles(UlfPath).Length > 0);

                var ulf = Directory.GetFiles(UlfPath).First();
                File.Move(ulf, ulfFile, overwrite: true);

                // wait file downloaded
                await Task.Delay(1000);
                spinner.Succeed("Download Ulf Succeed.");
            });
            Console.WriteLine($"UlfFile: {Path.GetFullPath(ulfFile)}");
        }

        private static ChromiumDriver CreateDriver()
        {
            // install chrome driver
            var _ = new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

            var service = ChromeDriverService.CreateDefaultService();
            var options = new ChromeOptions();

            // ファイルダウンロード時の保存先確認ウインドウを抑制する
            options.AddUserProfilePreference("disable-popup-blocking", "true");
            options.AddUserProfilePreference("download.default_directory", UlfPath);
            //ブラウザ非表示
            // service.HideCommandPromptWindow = true;

            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--window-position=-32000,-32000");
            options.AddArgument("--user-agent=unity-license-acitvator");

            var driver = new ChromeDriver(service, options);

            return driver;
        }
    }
}
