# Localization

The interface uses Chinese source strings as keys in `ScreenMover/Localization/Strings.json`. Each entry contains English, German, French and Spanish translations in that order. Keep all format placeholders, including numeric/date formats. Do not translate application data such as document titles, user-defined desktop names, process identities or persisted state identifiers.

Use `L.T(source)` for labels, `L.F(source, arguments)` for formatted messages, and `{views:Loc Key='source'}` in XAML. New strings must have all four translations. `L.External` is limited to legacy helper diagnostics and persisted status text; never pass arbitrary window titles through it.

The saved `Language` value is `System`, `zh-CN`, `en`, `de`, `fr` or `es`. System mode resolves Windows UI culture, including regional variants, with English as the fallback. Restarting applies the choice consistently to XAML, tray menus and native controls. Dates and numbers keep the user's regional formatting. Updater dialogs inherit translated captions from the launching process; standalone diagnostic invocations retain fallback captions.

Run `dotnet run --project Diagnostics -- --pure` to validate catalog completeness, format arguments, language resolution, settings persistence and update presentation. Also build the WinUI project to validate XAML markup. Manually review long German/French labels at narrow widths before accepting visual layout changes.
