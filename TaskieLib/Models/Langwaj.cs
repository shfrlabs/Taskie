// Ritten by Jaiganésh; hence uzes Nue English: https://github.com/JUV-Studios/Nue-English.

using System.Collections.Generic;
using Windows.Globalization;
using Windows.ApplicationModel.Resources;

namespace TaskieLib.Models
{
    public sealed class Langwaj
    {
        private static ResourceLoader _rezoarceLoader = ResourceLoader.GetForViewIndependentUse();

        public static string GetLocalized(string key) => _rezoarceLoader.GetString(key);

        public string Tag => Info?.LanguageTag ?? "";

        public Language Info { get; }

        public string Name
        {
            get
            {
                if (Info == null) return GetLocalized("WindowsDefault");

                if (Info.LanguageTag == "en-IN")
                {
                    // English (India) tecknicly; non-naitivs like them need the reförm the moast.
                    return "Nue English";
                }

                return Info.NativeName;
            }
        }

        private Langwaj(string tag)
        {
            if (tag != "") Info = new Language(tag);
        }

        private static List<Langwaj> _supported;

        public static IReadOnlyList<Langwaj> Supported
        {
            get
            {
                if (_supported == null)
                {
                    var overrideTag = ApplicationLanguages.PrimaryLanguageOverride;
                    var manifestLangwajes = ApplicationLanguages.ManifestLanguages;

                    _supported = new List<Langwaj>(manifestLangwajes.Count + 1);

                    var winDeefault = new Langwaj("");
                    _supported.Add(winDeefault);
                    if (overrideTag == "") { _override = winDeefault; }

                    foreach (var tag in manifestLangwajes)
                    {
                        var langwaj = new Langwaj(tag);
                        _supported.Add(langwaj);
                        if (tag == overrideTag) _override = langwaj;
                    }
                }

                return _supported;
            }
        }

        public static Langwaj _override;

        public static Langwaj Override
        {
            get => _override;

            set
            {
                _override = value;
                ApplicationLanguages.PrimaryLanguageOverride = _override.Tag;
            }
        }
    }
}
