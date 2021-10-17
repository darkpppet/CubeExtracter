# CubeExtracter
메이플 공홈 큐브 확률 표 데이터 긁어오는 프로그램

- - -
## Development Environment & Libraries
- goormIDE (Ubuntu 18.04 LTS)

- .NET SDK 5.0.401
  - Selenium.WebDriver                    4.0.0-rc3
  - Selenium.WebDriver.ChromeDriver       94.0.4606.610
  - Selenium.Support                      4.0.0-rc3
  - DotNetSeleniumExtras.WaitHelpers      3.11.0
  - HtmlAgilityPack                       1.11.37

- Google Chrome 94.0.4606.81

- - -
## How to Use
```bash
$ <Program name> [-p] <CubeKey>
```

- `[-p]`: Use Parallel Methods
 
- `<CubeKey>`
  - `red` (레드 큐브)
  - `black` (블랙 큐브)
  - `addi` (에디셔널 큐브)
  - `strange` (수상한 큐브)
  - `master` (장인의 큐브)
  - `artisan` (명장의 큐브)

- - -
## Extracted Json Data Info
 - `String`:
   - `CubeName`
   - `GradeName`
   - `PartsTypeName`
   - `RequiredLevel`
 
 - `Array`( `Object`: `String Name, String Probability`):	
   - `FirstLineOptions`
   - `SecondLineOptions`
   - `ThirdLineOptions`

