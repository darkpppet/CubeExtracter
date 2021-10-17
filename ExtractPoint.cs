
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

struct ExtractPoint
{
    public int NowGradeCount { get; set; }
    public int NowPartsTypeCount { get; set; }
    public int NowLevelCount { get; set; }
	
    public ExtractPoint(int nowGradeCount = 1, int nowPartsTypeCount = 1, int nowLevelCount = 0)
    {
        NowGradeCount = nowGradeCount;
        NowPartsTypeCount = nowPartsTypeCount;
        NowLevelCount = nowLevelCount;
    }
    
    public void SerializeThis(in string path, in string filename)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        File.WriteAllText($"{path}/{filename}", JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, //이스케이프시퀀스 작동하게
            WriteIndented = true, //줄바꿈 등 보기좋게
            IncludeFields = true //프로퍼티 저장
        }));
    }
}
