using AppGen.Core.Models;

namespace AppGen.Engine;

public static class TargetFlags
{
    public static bool AuthEnabled(SolutionSpec spec) =>
        spec.Targets?.Web.Auth.Enabled == true;

    public static bool OfflineEnabled(SolutionSpec spec) =>
        spec.Targets?.Mobile.Offline.Enabled == true;

    public static bool MobileEnabled(SolutionSpec spec) =>
        spec.Targets?.Mobile.Enabled == true;
}
