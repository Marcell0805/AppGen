namespace AppGen.UI.Services;

public sealed class UiThemeService
{
    public bool IsDark { get; private set; }

    public event Action? Changed;

    public void SetDark(bool isDark, bool notify = true)
    {
        if (IsDark == isDark)
            return;

        IsDark = isDark;
        if (notify)
            Changed?.Invoke();
    }

    public void Toggle() => SetDark(!IsDark);
}
