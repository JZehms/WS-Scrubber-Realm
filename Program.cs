using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using HtmlAgilityPack;
using System.Text.Json;



namespace my_app
{
    public class Pathfinder
    {
        public struct MyStruct
        {
            public List<string> children;
            public string link;
            public string title;
            public string screenshotLocal;
        }

        //this is what i output to a json file for vue
        public class scren
        {
            public string screenshotLoc { get; set; }
            public string screTitle { get; set; }
        }

        /// A queue of pages to be crawled
        private static Queue<string> queueToCheck = new Queue<string>();
        /// All pages visited
        private static HashSet<string> allCheckedPages = new HashSet<string>();
        private static Dictionary<string, int> pageCount = new Dictionary<string, int>();
        private static Dictionary<string, MyStruct> hierarchy = new Dictionary<string, MyStruct>();

        private static List<string> listFromJson = new List<string>();
        /// A Url that the crawled page must start with. 
        public static string siteName { get; set; }
        public static int totalCount = 0;
        /// Starting page of crawl.
        public static Uri beginning { get; set; }
        private static List<scren> screens = new List<scren>();
        public static IWebDriver driver = null;
        static HtmlDocument htmlDoc = new HtmlDocument();
        public static string title = "not provided";
        public static void Main()
        {
            if (File.Exists("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\index.html"))
            {
                File.Delete("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\index.html");
            }
            File.WriteAllText("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\index.html", "<!DOCTYPE html>\n<html>\n<head>\n<title> All Pages </title>\n</head><br/>\n<body>\n<h1> All Pages </h1>\n<div><ul>");


            beginning = new Uri("https://onrealm.t.ac.st");
            siteName = "https://onrealm.t.ac.st";
            driver = new ChromeDriver();
            driver.Navigate().GoToUrl(beginning);
            driver.FindElement(By.Id("emailAddress")).SendKeys("anneconley@example.org");
            driver.FindElement(By.Id("password")).SendKeys("RealmAcs#2018");
            driver.FindElement(By.Id("signInButton")).Click();
            Thread.Sleep(3000);
            driver.FindElement(By.Id("siteList")).Click();
            Thread.Sleep(3000);
            driver.FindElement(By.XPath("//*[@id='siteDialog']/div[1]/ul/div/li[26]")).Click();
            Thread.Sleep(3000);
            driver.FindElement(By.Id("selectSite")).Click();



            Start();

        }
        public static void Start()
        {

            if (!queueToCheck.Contains(driver.Url))
            {
                queueToCheck.Enqueue(driver.Url);
            }
            var threads = new ThreadStart(PathfinderThread);
            var thread = new Thread(threads);
            thread.Start();

        }

        private static void PathfinderThread()
        {
            while (true)
            {

                Thread.Sleep(5000);
                //if there is nothing left in queueToCheck
                Console.WriteLine(queueToCheck.Count);

                if (queueToCheck.Count == 0)
                {
                    var max = pageCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    Console.WriteLine("Done crawling");
                    using (FileStream indexFile = File.Open("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\index.html", FileMode.Append))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes("</ul><p>There are " + allCheckedPages.Count().ToString() + " pages contained in the domain of " + beginning + " <br/> The page with the most links is " + max + " with " + pageCount[max] + " links</body>\n</html>");
                        // Add some information to the file.  
                        indexFile.Write(info, 0, info.Length);
                    }
                    driver.Close();
                    var opt = new JsonSerializerOptions() { WriteIndented = true };
                    string strJson = JsonSerializer.Serialize<IList<scren>>(screens, opt);
                    using (FileStream indexFile = File.Open("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\index.html.json", FileMode.Append))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes(strJson);
                        // Add some information to the file.  
                        indexFile.Write(info, 0, info.Length);
                    }
                    Console.WriteLine(strJson);
                    return;
                }

                MyStruct pageinfo = new MyStruct();
                var webUrl = queueToCheck.First();
                Console.WriteLine(webUrl);
                if (webUrl == "https://onrealm.t.ac.st/connectdemochurch/Account/SignOut")
                {
                    if (!allCheckedPages.Contains(webUrl))
                    {
                        allCheckedPages.Add(webUrl);
                    }
                    queueToCheck.Dequeue();
                    continue;
                }
                if (allCheckedPages.Contains(webUrl))
                {
                    queueToCheck.Dequeue();
                    continue;
                }
                queueToCheck.Dequeue();
                driver.Navigate().GoToUrl(webUrl);
                try
                {
                    htmlDoc.LoadHtml(driver.PageSource);
                    // Console.WriteLine(html);
                }
                catch
                {
                    continue;
                }

                //load the html in the doc

                htmlDoc.OptionEmptyCollection = true;
                try
                {
                    title = htmlDoc.DocumentNode.SelectSingleNode("html/head/title").InnerText;
                }
                catch (System.NullReferenceException)
                {
                    title = "not provided";
                }
                Console.WriteLine(title);
                if (!allCheckedPages.Contains(webUrl))
                {
                    pageinfo.link = webUrl;
                    pageinfo.title = title;
                    pageinfo.children = new List<string>();
                    pageCount.Add(webUrl, 1);
                    allCheckedPages.Add(webUrl);
                    Visit(webUrl, title, pageinfo, driver);
                }
                //find the links in the page
                FindLinks(siteName, htmlDoc);

            }
        }
        public static void Visit(string webUrl, string title, MyStruct pageinfo, IWebDriver driver)
        {
            Console.WriteLine("Visited : " + webUrl);
            string filePath = "File.csv";
            File.AppendAllText(filePath, webUrl + "," + title + "\n");
            totalCount += 1;
            Console.WriteLine(totalCount);
            try
            {
                Pathfinder.driver.Navigate().GoToUrl(webUrl);

            }
            catch
            {

                return;
            }
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            ss.SaveAsFile("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\ScreenShot\\Screenshot" + allCheckedPages.Count().ToString() + ".png", ScreenshotImageFormat.Png);
            pageinfo.screenshotLocal = "C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\ScreenShot\\Screenshot" + allCheckedPages.Count().ToString() + ".png";
            using (FileStream indexFile = File.Open("C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\index.html", FileMode.Append))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes("<li><a href='C:\\Users\\joshuaz\\source\\repos\\WS Scrubber Realm\\ScreenShot\\Screensho" + allCheckedPages.Count().ToString() + ".png'>" + title + " </a></li>\n");
                // Add some information to the file.  
                indexFile.Write(info, 0, info.Length);
            }
            Console.WriteLine(Pathfinder.driver.Url);
            scren dept = new scren() { screenshotLoc = pageinfo.screenshotLocal, screTitle = title };
            screens.Add(dept);
        }
        public static List<string> FindLinks(string rootPage, HtmlDocument doc)
        {

            List<string> links = new List<string>();

            foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]"))
            {

                string link = node.Attributes["href"].Value;

                if (link.StartsWith(rootPage) || link.StartsWith("/"))
                {

                    link = FormatLink(link);

                    if (ValidLink(link))
                    {

                        links.Add(link);
                        queueToCheck.Enqueue(link);
                        Console.WriteLine($"Found page: {link}");
                    }
                }
            }

            return links;
        }

        public static string FormatLink(string link)
        {
            if (link.StartsWith("/"))
            {
                link = $"{siteName}{link}";
            }
            if (link.EndsWith("/"))
            {
                link = link.Remove(link.Length - 1);
            }

            return link;
        }

        public static bool ValidLink(string link)
        {
            if (link.Contains("#")
                    || queueToCheck.Contains(link)
                    || allCheckedPages.Contains(link))
            {

                return false;
            }

            return true;
        }


    }



}