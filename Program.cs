
class Program
{
    static void Main(string[] args)
    {
		CubeExtractor extractor = new() { CubeKey = args[0] };
		extractor.Extract();
	}
}
