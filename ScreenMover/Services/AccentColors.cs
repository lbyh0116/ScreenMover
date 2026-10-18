namespace ScreenMover.Services;

public static class AccentColors {
    public static string Normalize(string? value) {
        value=value?.Trim();
        return value is {Length:7} && value[0]=='#' && value.Skip(1).All(Uri.IsHexDigit)
            ? value.ToUpperInvariant() : "";
    }
    public static (byte R,byte G,byte B) Overlay(byte r,byte g,byte b) {
        // Keep white drop text readable even when the chosen accent is very pale.
        double luminance=.2126*Linear(r)+.7152*Linear(g)+.0722*Linear(b);
        double scale=luminance>.22?.64:1;
        while(.2126*Linear((byte)(r*scale))+.7152*Linear((byte)(g*scale))+.0722*Linear((byte)(b*scale))>.16)scale*=.9;
        return ((byte)(r*scale),(byte)(g*scale),(byte)(b*scale));
    }
    static double Linear(byte value) {double v=value/255.0;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
    public static bool UseBlackText(byte r,byte g,byte b)=>.2126*Linear(r)+.7152*Linear(g)+.0722*Linear(b)>.179;
}
