using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Core.Interfaces;

public interface IOutputParserService
{
    OutputParseResult Parse(string line, bool isStandardError);
}
