using CommandLine;

namespace JPEG
{
    public class Options
    {
        [Option('e', "encode", DefaultValue = @"..\..\sample.bmp",
            HelpText = "Path to bmp file.")]
        public string PathToBmp { get; set; }

        [Option('t', "threads", DefaultValue = 4,
            HelpText = "Max parallelism degree.")]
        public int Threads { get; set; }
        
        [Option('q', "quality", DefaultValue = 100,
            HelpText = "Percentage of final quality")]
        public int Quality { get; set; }

        [Option('d', "decode", HelpText = "Path to encoded image.")]
        public string PathToEncoded { get; set; }
    }
}