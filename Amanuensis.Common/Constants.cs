using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Amanuensis.Common
{
    public static class Constants
    {

        public static List<string> VideoExtensionsList = new List<string>() { ".mp4", ".mov", ".mkv", ".avi", ".wmv"};

        public const string OLLAMA_CLOUD_URL = "https://ollama.com/api/";
        public const int AIAssistantOllamaTimeoutSeconds = 60;
        public const string ProcessingTextModel = "gemma4:31b";


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
