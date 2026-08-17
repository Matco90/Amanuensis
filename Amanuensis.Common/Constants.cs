using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Amanuensis.Common
{
    public static class Constants
    {

        public static readonly List<string> AudioExtensionsList = new() { ".aa", ".aac", ".aax", ".ac3", ".ac4", ".aif", ".aifc", ".aiff", ".amr", ".ape", ".apl", ".aptx", ".aptxhd", ".au", ".caf", ".dss", ".dsf", ".dts", ".dtshd", ".eac3", ".ec3", ".flac", ".g722", ".g723_1", ".g728", ".g729", ".gsm", ".iamf", ".ilbc", ".ircam", ".lc3", ".m2a", ".m4a", ".m4b", ".mka", ".mlp", ".mp2", ".mp3", ".mpa", ".mpc", ".msbc", ".oga", ".ogg", ".oma", ".opus", ".qcp", ".qoa", ".ra", ".sbc", ".shn", ".sln", ".snd", ".tak", ".thd", ".tta", ".voc", ".vqf", ".w64", ".wav", ".weba", ".wma", ".wv", ".xwma" };
        public static readonly List<string> VideoExtensionsList = new() { ".3g2", ".3gp", ".4xm", ".asf", ".avi", ".bik", ".bk2", ".bmv", ".cdxl", ".cine", ".dat", ".dav", ".dif", ".dv", ".dxa", ".f4v", ".flv", ".gxf", ".ifv", ".ivr", ".kux", ".lvf", ".m2t", ".m2ts", ".m4v", ".mjpeg", ".mjpg", ".mkv", ".mlv", ".mods", ".moflex", ".mov", ".mp4", ".mpeg", ".mpg", ".mts", ".mvi", ".mxf", ".mxg", ".nsv", ".nut", ".nuv", ".ogv", ".pmp", ".pva", ".r3d", ".rm", ".rmvb", ".roq", ".rpl", ".sga", ".smk", ".str", ".swf", ".thp", ".ts", ".ty", ".usm", ".viv", ".vob", ".webm", ".wmv", ".wtv", ".xmv", ".yop" };

        public const string OLLAMA_CLOUD_URL = "https://ollama.com/api/";
        public const int AIAssistantOllamaTimeoutSeconds = 120;
        public const string ProcessingTextModel = "gemma4:31b";

        public const string EnvironmentVariableDeepgramApiKeyName = "DEEPGRAM_API_KEY";
        public const int DeepgramTimeoutSeconds = 120;


        public const string logo = """
          .-.
        .'  /
       /   /
      / / /
     / / /
    / / /
   /_/ /
     \/
     ||
   __||__         A    M   M    A    N   N  U   U  EEEEE  N   N   SSS   IIIII   SSS
 .'  ||  '.      A A   MM MM   A A   NN  N  U   U  E      NN  N  S        I    S
/____||____\    AAAAA  M M M  AAAAA  N N N  U   U  EEEE   N N N   SSS     I     SSS
\__________/    A   A  M   M  A   A  N  NN  U   U  E      N  NN      S    I        S
     ~~~~~~~    A   A  M   M  A   A  N   N   UUU   EEEEE  N   N  SSSS   IIIII  SSSS

                                  INTELLIGENT TRANSCRIPTION
""";

    }
}
