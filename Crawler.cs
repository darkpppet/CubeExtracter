using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

class CubeCrawler
{
	private ChromeDriver chromeDriver;
	private WebDriverWait webDriverWaiter;
	
	public string CubeKey { get; set; }
	
	private string Url { get => @"https://maplestory.nexon.com/Guide/OtherProbability/cube/" + CubeKey; }
	private string PathBody { get => @"extracted/" + CubeKey; }

	private readonly Dictionary<string, string> cubeKeyDic = new() { ["red"] = "레드 큐브", ["black"] = "블랙 큐브", ["addi"] = "에디셔널 큐브", ["strange"] = "수상한 큐브", ["master"] = "장인의 큐브", ["artisan"] = "명장의 큐브" };
	private readonly string[] gradesArr = { "", "레어", "에픽", "유니크", "레전드리" };
	private readonly string[] partsTypesArr = { "" , "무기", "엠블렘", "보조무기(포스실드, 소울링 제외)", "포스실드, 소울링", "방패", "모자", "상의", "한벌옷", "하의", "신발", "장갑", "망토", "벨트", "어깨장식", "얼굴장식", "눈장식", "귀고리", "반지", "펜던트", "기계심장" };
	private readonly int[] reqLevelArr = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120 };
	private readonly Dictionary<int, string> reqLevelsDic = new() { [5] = "0~9", [10] = "10", [15] = "11~19", [20] = "20", [25] = "21~29", [30] = "30", [35] = "31~39", [40] = "40", [45] = "41~49",	[50] = "50", [55] = "51~59", [60] = "60", [65] = "61~69", [70] = "70", [75] = "71~79", [80] = "80", [85] = "81~89", [90] = "90", [95] = "91~99", [100] = "100", [105] = "101~109", [110] = "110", [115] = "111~119", [120] = "120 이상" };
	
	public CubeCrawler()
	{
		Console.WriteLine("Initializing CubeCrawler Instance");
		
		Console.Write("    Initialize ChromeDriverSerive...");
		ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
		chromeDriverService.HideCommandPromptWindow = true;
		Console.WriteLine("ok");
		
		Console.Write("    Initialize ChromeOptions...");
		ChromeOptions chromeOptions = new();
		chromeOptions.AddArgument("--headless");
		chromeOptions.AddArgument("--disable-gpu");
		chromeOptions.AddArgument("--verbose");
		chromeOptions.AddArgument("--whitelisted-ips=''");
		chromeOptions.AddArgument("--no-sandbox");
		chromeOptions.AddArgument("--disable-dev-shm-usage");
		Console.WriteLine("ok");
		
		Console.Write("    Initialize ChromeDriver...");
		chromeDriver = new(chromeDriverService, chromeOptions);
		Console.WriteLine("ok");
		
		Console.Write("    Initialize WebDriverWait...");
		webDriverWaiter = new WebDriverWait(chromeDriver ,TimeSpan.FromSeconds(10));
		Console.WriteLine("ok");
	}
	
	private IWebElement FindElementWithWait(By by)
	{
		webDriverWaiter.Until(ExpectedConditions.ElementExists(by));
		return chromeDriver.FindElement(by);
	}
	
	private List<CubeOption> MakeOptionsList(IEnumerable<IWebElement> trs)
	{
		List<CubeOption> optionsLine = new();
		
		foreach (var tr in trs)
		{
			var tds = tr.FindElements(By.TagName("td"));
			
			optionsLine.Add(new CubeOption()
			{
				Name = tds[0].Text,
				Probability = tds[1].Text
			});
		}
		
		return optionsLine;
	}
	
	public void Crawl()
	{
		Console.WriteLine($"Initialize Crawling: [{CubeKey}]");
		
		Console.Write($"    Connect To [{Url}]...");
		chromeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
		chromeDriver.Navigate().GoToUrl(Url);
		Console.WriteLine("ok");
		
		Console.Write("    Declare Variables...");
		
		Dictionary<string, CrawlPoint> stoppedPoint = (File.Exists("stoppedPoint.json") ? JsonSerializer.Deserialize<Dictionary<string, CrawlPoint>>(File.ReadAllText("stoppedPoint.json")) : new() { ["red"] = new CrawlPoint(1, 1, 0), ["black"] = new CrawlPoint(1, 1, 0), ["addi"] = new CrawlPoint(1, 1, 0), ["strange"] = new CrawlPoint(1, 1, 0), ["master"] = new CrawlPoint(1, 1, 0), ["artisan"] = new CrawlPoint(1, 1, 0) });
		
		int nowGradeCount = stoppedPoint[CubeKey].NowGradeCount;
		int nowPartsTypeCount = stoppedPoint[CubeKey].NowPartsTypeCount;
		int nowLevelCount = stoppedPoint[CubeKey].NowLevelCount;
		
		IWebElement grade, nowGrade, partsType, nowPartsType, level, searchButton;
		
		IEnumerable<IWebElement> firstOptions, secondOptions, thirdOptions;
		
		int gradeCount = FindElementWithWait(By.XPath("//*[@id='selectGrade']")).FindElement(By.TagName("select")).FindElements(By.TagName("option")).Count;
		Console.WriteLine("ok");
		
		Console.WriteLine("    Start Crawling");
		while (true)
		{
			try
			{
				while(nowGradeCount <= gradeCount)
				{
					Console.Write($"    Grade: [{gradesArr[nowGradeCount]}]...");
					grade = FindElementWithWait(By.XPath("//*[@id='selectGrade']/a"));
					grade.SendKeys(Keys.Enter);
					
					nowGrade = FindElementWithWait(By.XPath($"//*[@id='selectGrade']/ul/li[{nowGradeCount}]/a"));
					nowGrade.SendKeys(Keys.Enter);
					Console.WriteLine("ok");
					
					while (nowPartsTypeCount <= 20)
					{
						Console.Write($"        Parts: [{partsTypesArr[nowPartsTypeCount]}]...");
						partsType = FindElementWithWait(By.XPath("//*[@id='selectPartsType']/a"));
						partsType.SendKeys(Keys.Enter);
						
						nowPartsType = FindElementWithWait(By.XPath($"//*[@id='selectPartsType']/ul/li[{nowPartsTypeCount}]/a"));
						nowPartsType.SendKeys(Keys.Enter);
						Console.WriteLine("ok");
						
						while (nowLevelCount < 24)
						{
							Console.Write($"            Level: [{reqLevelsDic[reqLevelArr[nowLevelCount]]}]...");
							level = FindElementWithWait(By.XPath("//*[@id='lv']"));
							level.Clear();
							level.SendKeys(reqLevelArr[nowLevelCount].ToString());
							
							searchButton = FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/div[1]/button"));
							searchButton.SendKeys(Keys.Enter);
							
							if (FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/div[2]")).Text == "장비 분류 및 장비 레벨에 해당하는 장비 아이템이 없습니다.")
							{
								Console.WriteLine($"No Data in this Level");
								nowLevelCount++;
								continue;
							}
							Console.WriteLine("ok");
							
							Console.Write("            Declare Table...");
							CubeTable table = new()
							{
								CubeName = cubeKeyDic[CubeKey],
								GradeName = gradesArr[nowGradeCount],
								PartsTypeName = partsTypesArr[nowPartsTypeCount],
								RequiredLevel = reqLevelsDic[reqLevelArr[nowLevelCount]]
							};
							Console.WriteLine("ok");
							
							Console.Write("                1st Line...");
							firstOptions = FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/table[1]/tbody")).FindElements(By.TagName("tr"));
							table.FirstLineOptions = MakeOptionsList(firstOptions);
							Console.WriteLine("ok");
							
							Console.Write("                2nd Line...");
							secondOptions = FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/table[2]/tbody")).FindElements(By.TagName("tr"));
							table.SecondLineOptions = MakeOptionsList(secondOptions);
							Console.WriteLine("ok");
							
							Console.Write("                3rd Line...");
							thirdOptions = FindElementWithWait(By.XPath("//*[@id='CubeSearchGroupArea']/table[3]/tbody")).FindElements(By.TagName("tr"));
							table.ThirdLineOptions = MakeOptionsList(thirdOptions);
							Console.WriteLine("ok");
							
							Console.Write("            Serialize Table...");
							StringBuilder pathBuilder = new(PathBody);
							pathBuilder.Append('/');
							pathBuilder.Append(table.GradeName);
							pathBuilder.Append('/');
							pathBuilder.Append(table.PartsTypeName);
							
							table.SerializeThis(pathBuilder.ToString(), table.RequiredLevel.Replace("~", "to") + ".json");
							Console.WriteLine("ok");
							
							stoppedPoint[CubeKey] = new CrawlPoint(nowGradeCount, nowPartsTypeCount, nowLevelCount);
							File.WriteAllText("stoppedPoint.json", JsonSerializer.Serialize(stoppedPoint, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
							
							nowLevelCount++;
						}
						nowLevelCount = 0;
						nowPartsTypeCount++;
					}
					nowPartsTypeCount = 1;
					nowGradeCount++;
				}
			}
			catch (StaleElementReferenceException e) 
			{
				stoppedPoint[CubeKey] = new CrawlPoint(nowGradeCount, nowPartsTypeCount, nowLevelCount);
				File.WriteAllText("stoppedPoint.json", JsonSerializer.Serialize(stoppedPoint, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
				
				Console.WriteLine(e);
				continue;
			}
			catch (Exception e) 
			{
				stoppedPoint[CubeKey] = new CrawlPoint(nowGradeCount, nowPartsTypeCount, nowLevelCount);
				File.WriteAllText("stoppedPoint.json", JsonSerializer.Serialize(stoppedPoint, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
				
				Console.WriteLine(e);
			}
			break;
		}
		Console.WriteLine("    Finish Crawling!");
	}
}
