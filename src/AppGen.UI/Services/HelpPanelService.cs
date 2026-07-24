namespace AppGen.UI.Services;

public sealed class HelpPanelService
{
    public event Action? Changed;

    public bool IsOpen { get; private set; }

    public string? ActiveTabId { get; private set; }

    public void Open(string tabId)
    {
        ActiveTabId = tabId;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        ActiveTabId = null;
        Changed?.Invoke();
    }
}
