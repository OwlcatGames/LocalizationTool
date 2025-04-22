using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTracker.ViewModel
{
    public  class SpeakersChangedVM
    {
        [NotNull]
        public string Path { get; set; }

        [NotNull]
        public string Key { get; set; }
        public string Status { get; set; }

        public string? OldSpeaker { get; set; }
        public string? ActualSpeaker { get; set; }

    }
}
