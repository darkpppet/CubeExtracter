
using System;

class Program
{
    static void Main(string[] args)
    {
        CubeExtractor extractor;
        
        if (args[0].ToLower() == "-p")
        {
            extractor = new CubeExtractorParallel() { CubeKey = args[1] };
            extractor.Extract();
        }
        else
        {
            extractor = new() { CubeKey = args[0] };
            extractor.Extract();
        }
    }
}
