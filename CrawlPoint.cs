
struct CrawlPoint
{
	public int NowGradeCount { get; set; }
	public int NowPartsTypeCount { get; set; }
	public int NowLevelCount { get; set; }
	
	public CrawlPoint(int nowGradeCount = 1, int nowPartsTypeCount = 1, int nowLevelCount = 0)
	{
		NowGradeCount = nowGradeCount;
		NowPartsTypeCount = nowPartsTypeCount;
		NowLevelCount = nowLevelCount;
	}
}
