using System;
using System.Collections.Generic;
using System.Text;

namespace Amanuensis.Common.Enum
{
    public enum AmanuensisErrorCode_Type
    {
        InvalidFile = 601,
        UnsupportedFileFormat = 602,
        AudioTrackNotFound = 603,
        AudioExtractionFailed = 604,
        MissingApiKey = 605,
        ProviderUnavailable = 606,
        ProviderRequestRejected = 607,
        ProcessingTimeout = 608,
        FileNotFound = 609,
        DirectoryNotFound = 610,
        PlatformNotSupported = 611,
        LoadSettingsError = 612,
        SaveSettingsError = 613
    }
}
