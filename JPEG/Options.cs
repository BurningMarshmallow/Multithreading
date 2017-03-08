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

        [Option('w', "window", DefaultValue = 8,
            HelpText = "Size of DCT window")]
        public int WindowSize { get; set; }

        [Option('q', "quota", DefaultValue = 100,
            HelpText = "Percentage of not ignoring high frequency")]
        public int Quality { get; set; }

        [Option('d', "decode", HelpText = "Path to encoded image.")]
        public string PathToEncoded { get; set; }
    }
}