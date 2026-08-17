using Amanuensis.Common.Entities;
using Amanuensis.Common.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Services.Contracts
{
    public interface ISettingsService
    {
        void ReadSettings();
        Settings GetSettings();
        void SaveSettings(Settings settings);
    }
}
