
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Diagnostics;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

using HtmlAgilityPack;

class CubeExtractor
{
	private ChromeDriver chromeDriver;
	private WebDriverWait webDriverWaiter;

	private HtmlDocument htmlDoc = new();
	
	public string CubeKey { get; set; }
	
	private string Url { get => @"https://maplestory.nexon.com/Guide/OtherProbability/cube/" + CubeKey; }
	private string PathBody { get => @"extracted/" + CubeKey; }
	private string StoppedPointPath { get => @"stoppedPoint/stoppedPoint" + CubeKey + ".json"; }

	private readonly Dictionary<string, string> cubeKeyDic = JsonSerializer.Deserialize<Dictionary <string, string>>(File.ReadAllText(@"Data/cubeKeyDic.json"));
	private readonly string[] gradesArr = JsonSerializer.Deserialize<string[]>(File.ReadAllText(@"Data/gradesArr.json"));
	private readonly string[] partsTypesArr = JsonSerializer.Deserialize<string[]>(File.ReadAllText(@"Data/partsTypesArr.json"));
	private readonly int[] reqLevelsArr = JsonSerializer.Deserialize<int[]>(File.ReadAllText(@"Data/reqLevelsArr.json"));
	private readonly Dictionary<int, string> reqLevelsDic = JsonSerializer.Deserialize<Dictionary <int, string>>(File.ReadAllText(@"Data/reqLevelsDic.json"));
	private readonly Dictionary<string, string> englishDic = JsonSerializer.Deserialize<Dictionary <string, string>>(File.ReadAllText(@"Data/englishDic.json"));
		
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
	
	private List<CubeOption> MakeOptionsList(HtmlNodeCollection trs)
	{
		List<CubeOption> optionsLine = new();
		
		foreach (HtmlNode tr in trs)
		{
			HtmlNodeCollection tds = tr.SelectNodes("td");
			
			optionsLine.Add(new CubeOption()
			{
				Name = tds[0].InnerText,
				Probability = tds[1].InnerText
			});
		}
		
		return optionsLine;
	}
	
	public void Extract()
	{
		Stopwatch stopwatch = new();
        stopwatch.Start();
		
		chromeDriver.Navigate().GoToUrl(Url);
		Console.WriteLine($"[{CubeKey}] Connect [{Url}]");
		
		ExtractPoint stoppedPoint = (File.Exists(StoppedPointPath) ? JsonSerializer.Deserialize<ExtractPoint>(File.ReadAllText(StoppedPointPath)) : new(1, 1, 0));
		
		int nowGradeCount = stoppedPoint.NowGradeCount;
		int nowPartsTypeCount = stoppedPoint.NowPartsTypeCount;
		int nowLevelCount = stoppedPoint.NowLevelCount;
		
		IWebElement grade, nowGrade, partsType, nowPartsType, level, searchButton;
		
		HtmlNodeCollection firstOptions, secondOptions, thirdOptions;
		
		webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='selectGrade']")));
		int gradeCount = chromeDriver.FindElement(By.XPath("//*[@id='selectGrade']")).FindElement(By.TagName("select")).FindElements(By.TagName("option")).Count;
		
		while (true)
		{
			try
			{
				while(nowGradeCount <= gradeCount)
				{
					webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='selectGrade']/a")));
					grade = chromeDriver.FindElement(By.XPath("//*[@id='selectGrade']/a"));
					grade.SendKeys(Keys.Enter);
					
					webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[@id='selectGrade']/ul/li[{nowGradeCount}]/a")));
					nowGrade = chromeDriver.FindElement(By.XPath($"//*[@id='selectGrade']/ul/li[{nowGradeCount}]/a"));
					nowGrade.SendKeys(Keys.Enter);
					
					while (nowPartsTypeCount <= 20)
					{
						webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='selectPartsType']/a")));
						partsType = chromeDriver.FindElement(By.XPath("//*[@id='selectPartsType']/a"));
						partsType.SendKeys(Keys.Enter);
						
						webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[@id='selectPartsType']/ul/li[{nowPartsTypeCount}]/a")));
						nowPartsType = chromeDriver.FindElement(By.XPath($"//*[@id='selectPartsType']/ul/li[{nowPartsTypeCount}]/a"));
						nowPartsType.SendKeys(Keys.Enter);
						
						while (nowLevelCount < 24)
						{
							webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='lv']")));
							level = chromeDriver.FindElement(By.XPath("//*[@id='lv']"));
							level.Clear();
							level.SendKeys(reqLevelsArr[nowLevelCount].ToString());
							
							webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='CubeSearchGroupArea']/div[1]/button")));
							searchButton = chromeDriver.FindElement(By.XPath("//*[@id='CubeSearchGroupArea']/div[1]/button"));
							searchButton.SendKeys(Keys.Enter);
							
							webDriverWaiter.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id='CubeSearchGroupArea']/div[2]")));
							if (chromeDriver.FindElement(By.XPath("//*[@id='CubeSearchGroupArea']/div[2]")).Text == "장비 분류 및 장비 레벨에 해당하는 장비 아이템이 없습니다.")
							{
								stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
								File.WriteAllText(StoppedPointPath, JsonSerializer.Serialize(stoppedPoint, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
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
							
							StringBuilder pathBuilder = new(PathBody);
							pathBuilder.Append('/');
							pathBuilder.Append(englishDic[table.GradeName]);
							pathBuilder.Append('/');
							pathBuilder.Append(englishDic[table.PartsTypeName]);
							
							table.SerializeThis(pathBuilder.ToString(), table.RequiredLevel.Replace("~", "to") + ".json");
							
							Console.WriteLine($"[{stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}] [{CubeKey}] [{gradesArr[nowGradeCount]}]({nowGradeCount}/{gradeCount}) [{partsTypesArr[nowPartsTypeCount]}]({nowPartsTypeCount}/20) [{reqLevelsDic[reqLevelsArr[nowLevelCount]]}]({nowLevelCount + 1}/24): Complete");
							
							stoppedPoint = new(nowGradeCount, nowPartsTypeCount, nowLevelCount);
							File.WriteAllText(StoppedPointPath, JsonSerializer.Serialize(stoppedPoint, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
							
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
				File.WriteAllText(StoppedPointPath, JsonSerializer.Serialize(stoppedPoint, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
				Console.WriteLine(e);
				continue;
			}
		}
	}
}
