using CommandLine;
using CommandLine.Text;

namespace JPEG
{
    public class Options
    {
        [Option('e', HelpText = "Path to bmp file.")]
        public string FileToCompress { get; set; }

        [Option('t', DefaultValue = 1, HelpText = "Max parallelism degree.")]
        public int ThreadsCount { get; set; }

        [Option('q', DefaultValue = 50, HelpText = "The quality of compression.")]
        public int Quality { get; set; }

        [Option('d', HelpText = "Path to encoded image.")]
        public string FileToDecompress { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this);
        }
    }
}
