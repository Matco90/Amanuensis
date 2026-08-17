using Amanuensis.Common.Entities;
using Amanuensis.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Tests.Fakes
{
    public class FakeSettingsService : ISettingsService
    {
        Settings settings;

        public FakeSettingsService()
        {
            settings = new Settings();
        }

        public Settings GetSettings()
        {
           return settings;
        }

        public void ReadSettings()
        {
            //not implemented
        }

        public void SaveSettings(Settings settings)
        {
            this.settings = settings;
        }
    }
}
