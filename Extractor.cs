
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

using HtmlAgilityPack;

class CubeExtractor
{
    protected ChromeDriver chromeDriver;
    protected WebDriverWait webDriverWaiter;

    protected HtmlDocument htmlDoc = new();
    
    public string CubeKey { get; set; }
	
    protected string Url { get => @"https://maplestory.nexon.com/Guide/OtherProbability/cube/" + CubeKey; }
    protected string ExtractedPathBody { get => @"extracted/" + CubeKey; }
    protected string StoppedPointPathBodyName { get; } = @"stoppedPoint";
    protected string StoppedPointJsonName { get => @"stoppedPoint" + CubeKey + @".json"; }
    protected string StoppedPointFullPathName { get => $"{StoppedPointPathBodyName}/{StoppedPointJsonName}"; }

    protected readonly Dictionary<string, string> cubeKeyDic = JsonSerializer.Deserialize<Dictionary <string, string>>(File.ReadAllText(@"Data/cubeKeyDic.json"));
    protected readonly string[] gradesArr = JsonSerializer.Deserialize<string[]>(File.ReadAllText(@"Data/gradesArr.json"));
    protected readonly string[] partsTypesArr = JsonSerializer.Deserialize<string[]>(File.ReadAllText(@"Data/partsTypesArr.json"));
    protected readonly int[] reqLevelsArr = JsonSerializer.Deserialize<int[]>(File.ReadAllText(@"Data/reqLevelsArr.json"));
    protected readonly Dictionary<int, string> reqLevelsDic = JsonSerializer.Deserialize<Dictionary <int, string>>(File.ReadAllText(@"Data/reqLevelsDic.json"));
    protected readonly Dictionary<string, string> englishDic = JsonSerializer.Deserialize<Dictionary <string, string>>(File.ReadAllText(@"Data/englishDic.json"));

    public CubeExtractor()
    {
        ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
        chromeDriverService.HideCommandPromptWindow = true;
        Console.WriteLine("Initialize ChromeDriverSerive");

        ChromeOptions chromeOptions = new();
        chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument("--disable-gpu");
        chromeOptions.AddArgument("--verbose");
        chromeOptions.AddArgument("--whitelisted-ips=''");
        chromeOptions.AddArgument("--no-sandbox");
        chromeOptions.AddArgument("--disable-dev-shm-usage");
        Console.WriteLine("Initialize ChromeOptions");

        chromeDriver = new(chromeDriverService, chromeOptions);
        Console.WriteLine("Initialize ChromeDriver");

        webDriverWaiter = new(chromeDriver, TimeSpan.FromSeconds(10));
        Console.WriteLine("Initialize WebDriverWait");
    }
	
    protected IWebElement FindElementWithWait(By by)
    {
        webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(by));
        return chromeDriver.FindElement(by);
    }
    
    protected virtual List<CubeOption> MakeOptionsList(HtmlNodeCollection trs)
    {
        List<CubeOption> optionsLine = new();

        foreach (var tr in trs)
        {
            HtmlNodeCollection tds = tr.SelectNodes("td");
            
            optionsLine.Add(new CubeOption() { Name = tds[0].InnerText, Probability = tds[1].InnerText });
        }
        
        return optionsLine;
    }
    
    public virtual void Extract()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        chromeDriver.Navigate().GoToUrl(Url);
        Console.WriteLine($"[{CubeKey}] Connect [{Url}]");

        ExtractPoint stoppedPoint = (File.Exists(StoppedPointFullPathName) ? JsonSerializer.Deserialize<ExtractPoint>(File.ReadAllText(StoppedPointFullPathName)) : new(1, 1, 0));

        int nowGradeCount = stoppedPoint.NowGradeCount;
        int nowPartsTypeCount = stoppedPoint.NowPartsTypeCount;
        int nowLevelCount = stoppedPoint.NowLevelCount;

        IWebElement grade, nowGrade, partsType, nowPartsType, level, searchButton;

        HtmlNodeCollection firstOptions, secondOptions, thirdOptions;

        int gradeCount = FindElementWithWait(By.XPath("//*[@id='selectGrade']")).FindElement(By.TagName("select")).FindElements(By.TagName("option")).Count;

        while (true)
        {
            try
            {
                while(nowGradeCount <= gradeCount)
                {
                    grade = FindElementWithWait(By.XPath("//*[@id='selectGrade']/a"));
                    grade.SendKeys(Keys.Enter);

                    nowGrade = FindElementWithWait(By.XPath($"//*[@id='selectGrade']/ul/li[{nowGradeCount}]/a"));
                    nowGrade.SendKeys(Keys.Enter);

                    while (nowPartsTypeCount <= 20)
                    {
                        partsType = FindElementWithWait(By.XPath("//*[@id='selectPartsType']/a"));
                        partsType.SendKeys(Keys.Enter);

                        nowPartsType = FindElementWithWait(By.XPath($"//*[@id='selectPartsType']/ul/li[{nowPartsTypeCount}]/a"));
                        nowPartsType.SendKeys(Keys.Enter);

                        while (nowLevelCount < 24)
                        {
                            level = FindElementWithWait(By.XPath("//*[@id='lv']"));
                            level.Clear();
                            level.SendKeys(reqLevelsArr[nowLevelCount].ToString());

                            searchButton = FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/div[1]/button"));
                            searchButton.SendKeys(Keys.Enter);

                            if (FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/div[2]")).Text == "장비 분류 및 장비 레벨에 해당하는 장비 아이템이 없습니다.")
                            {
                                stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
                                stoppedPoint.SerializeThis(StoppedPointPathBodyName, StoppedPointJsonName);
                                Console.WriteLine($"[{stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}] [{CubeKey}] [{gradesArr[nowGradeCount]}]({nowGradeCount}/{gradeCount}) [{partsTypesArr[nowPartsTypeCount]}]({nowPartsTypeCount}/20) [{reqLevelsDic[reqLevelsArr[nowLevelCount]]}]({nowLevelCount + 1}/24): No Data");
                                nowLevelCount++;
                                continue;
                            }

                            htmlDoc.LoadHtml(chromeDriver.PageSource);

                            CubeTable table = new()
                            {
                                CubeName = cubeKeyDic[CubeKey],
                                GradeName = gradesArr[nowGradeCount],
                                PartsTypeName = partsTypesArr[nowPartsTypeCount],
                                RequiredLevel = reqLevelsDic[reqLevelsArr[nowLevelCount]]
                            };

                            List<CubeOption> firstLineOptions = null;
                            List<CubeOption> secondLineOptions = null;
                            List<CubeOption> thirdLineOptions = null;

                            firstOptions = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='CubeSearchGroupArea']/table[1]/tbody").SelectNodes("tr");
                            firstLineOptions = MakeOptionsList(firstOptions);
                            secondOptions = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='CubeSearchGroupArea']/table[2]/tbody").SelectNodes("tr");
                            secondLineOptions = MakeOptionsList(secondOptions);
                            thirdOptions = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='CubeSearchGroupArea']/table[3]/tbody").SelectNodes("tr");
                            thirdLineOptions = MakeOptionsList(thirdOptions);
                            
                            table.FirstLineOptions = firstLineOptions;
                            table.SecondLineOptions = secondLineOptions;
                            table.ThirdLineOptions = thirdLineOptions;

                            table.SerializeThis($"{ExtractedPathBody}/{englishDic[table.GradeName]}/{englishDic[table.PartsTypeName]}", table.RequiredLevel.Replace("~", "to") + ".json");

                            Console.WriteLine($"[{stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}] [{CubeKey}] [{gradesArr[nowGradeCount]}]({nowGradeCount}/{gradeCount}) [{partsTypesArr[nowPartsTypeCount]}]({nowPartsTypeCount}/20) [{reqLevelsDic[reqLevelsArr[nowLevelCount]]}]({nowLevelCount + 1}/24): Complete");

                            stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
                            stoppedPoint.SerializeThis(StoppedPointPathBodyName, StoppedPointJsonName);

                            nowLevelCount++;
                        }
                        nowLevelCount = 0;
                        nowPartsTypeCount++;
                    }
                    nowPartsTypeCount = 1;
                    nowGradeCount++;
                } 
                stopwatch.Stop();
                Console.WriteLine($"[{CubeKey}] Finish Extracting!({stopwatch.Elapsed.Minutes}m {stopwatch.Elapsed.Seconds}s)");
                break;
            }
            catch (Exception e)
            {
                stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
                stoppedPoint.SerializeThis(StoppedPointPathBodyName, StoppedPointJsonName);
                Console.WriteLine(e);
                continue;
            }
        }
    }
}

class CubeExtractorParallel : CubeExtractor
{
    private object lockObject = new();
    
    protected override List<CubeOption> MakeOptionsList(HtmlNodeCollection trs)
    {
        List<CubeOption> optionsLine = new();
        
        Parallel.ForEach(trs, (tr) => 
                         {
                             HtmlNodeCollection tds = tr.SelectNodes("td");
                             
                             lock(lockObject)
                                 optionsLine.Add(new CubeOption() { Name = tds[0].InnerText, Probability = tds[1].InnerText });
                         });
        
        return optionsLine;
    }
    
    public override void Extract()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        chromeDriver.Navigate().GoToUrl(Url);
        Console.WriteLine($"[{CubeKey}] Connect [{Url}]");

        ExtractPoint stoppedPoint = (File.Exists(StoppedPointFullPathName) ? JsonSerializer.Deserialize<ExtractPoint>(File.ReadAllText(StoppedPointFullPathName)) : new(1, 1, 0));

        int nowGradeCount = stoppedPoint.NowGradeCount;
        int nowPartsTypeCount = stoppedPoint.NowPartsTypeCount;
        int nowLevelCount = stoppedPoint.NowLevelCount;

        IWebElement grade, nowGrade, partsType, nowPartsType, level, searchButton;

        HtmlNodeCollection firstOptions, secondOptions, thirdOptions;

        int gradeCount = FindElementWithWait(By.XPath("//*[@id='selectGrade']")).FindElement(By.TagName("select")).FindElements(By.TagName("option")).Count;

        while (true)
        {
            try
            {
                while(nowGradeCount <= gradeCount)
                {
                    grade = FindElementWithWait(By.XPath("//*[@id='selectGrade']/a"));
                    grade.SendKeys(Keys.Enter);

                    nowGrade = FindElementWithWait(By.XPath($"//*[@id='selectGrade']/ul/li[{nowGradeCount}]/a"));
                    nowGrade.SendKeys(Keys.Enter);

                    while (nowPartsTypeCount <= 20)
                    {
                        partsType = FindElementWithWait(By.XPath("//*[@id='selectPartsType']/a"));
                        partsType.SendKeys(Keys.Enter);

                        nowPartsType = FindElementWithWait(By.XPath($"//*[@id='selectPartsType']/ul/li[{nowPartsTypeCount}]/a"));
                        nowPartsType.SendKeys(Keys.Enter);

                        while (nowLevelCount < 24)
                        {
                            level = FindElementWithWait(By.XPath("//*[@id='lv']"));
                            level.Clear();
                            level.SendKeys(reqLevelsArr[nowLevelCount].ToString());

                            searchButton = FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/div[1]/button"));
                            searchButton.SendKeys(Keys.Enter);

                            if (FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/div[2]")).Text == "장비 분류 및 장비 레벨에 해당하는 장비 아이템이 없습니다.")
                            {
                                stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
                                stoppedPoint.SerializeThis(StoppedPointPathBodyName, StoppedPointJsonName);
                                Console.WriteLine($"[{stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}] [{CubeKey}] [{gradesArr[nowGradeCount]}]({nowGradeCount}/{gradeCount}) [{partsTypesArr[nowPartsTypeCount]}]({nowPartsTypeCount}/20) [{reqLevelsDic[reqLevelsArr[nowLevelCount]]}]({nowLevelCount + 1}/24): No Data");
                                nowLevelCount++;
                                continue;
                            }

                            htmlDoc.LoadHtml(chromeDriver.PageSource);

                            CubeTable table = new()
                            {
                                CubeName = cubeKeyDic[CubeKey],
                                GradeName = gradesArr[nowGradeCount],
                                PartsTypeName = partsTypesArr[nowPartsTypeCount],
                                RequiredLevel = reqLevelsDic[reqLevelsArr[nowLevelCount]]
                            };

                            List<CubeOption> firstLineOptions = null;
                            List<CubeOption> secondLineOptions = null;
                            List<CubeOption> thirdLineOptions = null;

                            Parallel.Invoke
                            (
                                () =>
                                {
                                    firstOptions = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='CubeSearchGroupArea']/table[1]/tbody").SelectNodes("tr");
                                    firstLineOptions = MakeOptionsList(firstOptions);
                                },
                                () =>
                                {
                                    secondOptions = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='CubeSearchGroupArea']/table[2]/tbody").SelectNodes("tr");
                                    secondLineOptions = MakeOptionsList(secondOptions);
                                },
                                () =>
                                {
                                    thirdOptions = htmlDoc.DocumentNode.SelectSingleNode("//*[@id='CubeSearchGroupArea']/table[3]/tbody").SelectNodes("tr");
                                    thirdLineOptions = MakeOptionsList(thirdOptions);
                                }
                            );

                            table.FirstLineOptions = firstLineOptions;
                            table.SecondLineOptions = secondLineOptions;
                            table.ThirdLineOptions = thirdLineOptions;

                            table.SerializeThis($"{ExtractedPathBody}/{englishDic[table.GradeName]}/{englishDic[table.PartsTypeName]}", table.RequiredLevel.Replace("~", "to") + ".json");

                            Console.WriteLine($"[{stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}] [{CubeKey}] [{gradesArr[nowGradeCount]}]({nowGradeCount}/{gradeCount}) [{partsTypesArr[nowPartsTypeCount]}]({nowPartsTypeCount}/20) [{reqLevelsDic[reqLevelsArr[nowLevelCount]]}]({nowLevelCount + 1}/24): Complete");

                            stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
                            stoppedPoint.SerializeThis(StoppedPointPathBodyName, StoppedPointJsonName);

                            nowLevelCount++;
                        }
                        nowLevelCount = 0;
                        nowPartsTypeCount++;
                    }
                    nowPartsTypeCount = 1;
                    nowGradeCount++;
                } 
                stopwatch.Stop();
                Console.WriteLine($"[{CubeKey}] Finish Extracting!({stopwatch.Elapsed.Minutes}m {stopwatch.Elapsed.Seconds}s)");
                break;
            }
            catch (Exception e)
            {
                stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
                stoppedPoint.SerializeThis(StoppedPointPathBodyName, StoppedPointJsonName);
                Console.WriteLine(e);
                continue;
            }
        }
    }
}


