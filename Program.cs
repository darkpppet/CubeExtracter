
class Program
{
    static void Main(string[] args)
    {
		CubeCrawler crawler = new() { CubeKey = args[0] };
		crawler.Crawl();
	}
}
