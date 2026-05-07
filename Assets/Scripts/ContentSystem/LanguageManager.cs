using System;

public enum Language { ZH, EN }

public static class LanguageManager
{
    public static Language Current { get; private set; } = Language.ZH;

    public static event Action<Language> OnLanguageChanged;

    public static void SetLanguage(Language lang)
    {
        if (Current == lang) return;
        Current = lang;
        OnLanguageChanged?.Invoke(lang);
    }

    public static string GetLocalized(LocalizedString s)
    {
        return Current == Language.ZH ? s.zh : s.en;
    }

    public static LocalizedText GetLocalizedText(NodeData d)
    {
        return Current == Language.ZH ? d.zhText : d.enText;
    }

    public static LocalizedAudio GetLocalizedAudio(NodeData d)
    {
        return Current == Language.ZH ? d.zhAudio : d.enAudio;
    }
}
