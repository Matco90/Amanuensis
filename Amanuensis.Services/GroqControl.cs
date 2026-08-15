using MaIN.Core;
using MaIN.Domain.Configuration;
using Amanuensis.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services
{
    public class GroqControl : ServiceBase
    {

        public GroqControl(Settings settings)
        {
            this.settings = settings;

            MaINBootstrapper.Initialize(configureSettings: (options) =>
            {
                options.BackendType = BackendType.GroqCloud; options.GroqCloudKey = settings.GroqApiKey;
            });
        }

        public string ConvertSpeechToText()
        {
            string speechText = "";



            return speechText;
        }

    }
}
