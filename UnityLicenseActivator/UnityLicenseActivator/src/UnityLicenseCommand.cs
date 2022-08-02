using Kurukuru;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityLicenseActivator.Extensions;

namespace UnityLicenseActivator
{
    public class UnityLicenseCommand : ConsoleAppBase
    {
        private ChromiumDriver driver = null;

        [Command("auth-ulf")]
        public async Task RunAsync(
            [Option("e")] string email,
            [Option("p")] string password,
            [Option("a")] string alfFilePath,
            [Option("d")] float safetyDelay = 1.0f
            )
        {
            var fullPath = Path.GetFullPath(alfFilePath);
            Console.WriteLine($"Alf: {alfFilePath} -> {fullPath}");
            var delayMiliSeconds = (int)(safetyDelay * 1000);

            this.driver = CreateDriver();
            try
            {
                await Login(email, password, delayMiliSeconds);
                await AuthAlfFile(fullPath, delayMiliSeconds);
                await ChooseLicenseOptions(delayMiliSeconds);
                await DownloadUlf(delayMiliSeconds);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                var date = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
                this.driver.ExportNowPngScreenShot(date);
            }
            await Task.Delay(5000);
        }

        private async Task Login(string email, string password, int safetyDelayMiliSeconds)
        {
            await this.driver.OpenAsyncWithLog("https://license.unity3d.com/manual", safetyDelayMiliSeconds);
            await Spinner.StartAsync($"Login...", async (spinner) =>
            {
                await Task.Delay(safetyDelayMiliSeconds);

                this.driver.FindElement(By.Id("conversations_create_session_form_email")).SendKeys(email);
                this.driver.FindElement(By.Id("conversations_create_session_form_password")).SendKeys(password);
                this.driver.FindElement(By.Name("commit")).Click();

                spinner.Succeed($"Login Succeed.");
            });
        }
        private async Task AuthAlfFile(string licenseFile, int safetyDelayMiliSeconds)
        {
            await Spinner.StartAsync($"Alf Upload...", async (spinner) =>
            {
                await Task.Delay(safetyDelayMiliSeconds);

                this.driver.FindElement(By.Id("licenseFile")).SendKeys(licenseFile);
                this.driver.FindElement(By.Name("commit")).Click();

                spinner.Succeed($"Alf Upload Succeed.");
            });
        }
        private async Task ChooseLicenseOptions(int safetyDelayMiliSeconds)
        {
            await Spinner.StartAsync($"License Options...", async (spinner) =>
            {
                // Note: 不安定
                await Task.Delay(3000);
                await Task.Delay(safetyDelayMiliSeconds);
                // this.driver.FindElement(By.Id("type_serial")).GetParent().Click(); // Pro 
                this.driver.FindElement(By.Id("type_personal")).GetParent().Click();

                // this.driver.FindElement(By.Id("option1")).GetParent().Click(); // companyOrOrganizationOver100000Doller
                // this.driver.FindElement(By.Id("option2")).GetParent().Click(); // companyOrOrganizationLess100000Doller
                this.driver.FindElement(By.Id("option3")).GetParent().Click();

                // Plus / Pro側へ反応する場合があるので可視要素のみでフィルタリング
                driver.FindElements(By.Name("commit")).Where(m => m.Displayed).First().Click();

                spinner.Succeed($"License Option Selected.");
            });
        }

        private async Task DownloadUlf(int safetyDelayMiliSeconds)
        {
            await Spinner.StartAsync($"Download Ulf...", async (spinner) =>
            {
                await Task.Delay(safetyDelayMiliSeconds);
                this.driver.FindElement(By.Name("commit")).Click();

                spinner.Succeed("Download Ulf Succeed.");
            });
        }

        private static ChromiumDriver CreateDriver()
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            var options = new ChromeOptions();

            //ブラウザ非表示
            // service.HideCommandPromptWindow = true;

            // options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            // options.AddArgument("--window-position=-32000,-32000");
            options.AddArgument("--user-agent=unity-license-acitvator");

            var driver = new ChromeDriver(service, options);

            return driver;
        }
    }
}
