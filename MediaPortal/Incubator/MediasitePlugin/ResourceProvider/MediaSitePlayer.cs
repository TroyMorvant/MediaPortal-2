using System;
using DirectShowLib;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using MediasitePlugin.Models;

namespace MediasitePlugin.ResourceProvider
{
  public class MediaSitePlayer : VideoPlayer, IUIContributorPlayer
  {
    public const string MEDIASITE_MIMETYPE = "video/mediasite";
    const string SOURCE_FILTER_NAME = "LAV Splitter Source";

    protected override void AddFileSource()
    {
      IBaseFilter sourceFilter = null;
      try
      {
        sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, SOURCE_FILTER_NAME);
        int hr = ((IFileSourceFilter) sourceFilter).Load(_resourceAccessor.ResourcePathName, null);
        DsError.ThrowExceptionForHR(hr);
        FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
      }
      finally
      {
        FilterGraphTools.TryRelease(ref sourceFilter);
      }
    }

    #region IUIContributorPlayer Member

    public Type UIContributorType
    {
      get { return typeof(MediaSiteUIContributor); }
    }

    #endregion
  }
}
