namespace ScreenMover.Services;
public static class UpdatePresentation {
    public static string Notes(GitHubRelease release,string installed)=>
        ReleaseVersion.Parse(release.Tag).CompareTo(ReleaseVersion.Parse(installed))<=0?L.T("已是最新版"):
        string.IsNullOrWhiteSpace(release.Notes)?L.T("此版本暂无更新说明，请查看发布页。"):release.Notes;
}
