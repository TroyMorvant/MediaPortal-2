using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Models;

namespace MediasitePlugin.Models
{
  public class MediaSiteUIContributor : BaseVideoPlayerUIContributor
  {
    public const string SCREEN_FULLSCREEN_MEDIASITE = "FullscreenContentMediaSite";
    public const string SCREEN_CURRENTLY_PLAYING_MEDIASITE = "CurrentlyPlayingVideo"; // Default video screen

    public override bool BackgroundDisabled
    {
      get { return true; }
    }

    public override string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return SCREEN_CURRENTLY_PLAYING_MEDIASITE;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return SCREEN_FULLSCREEN_MEDIASITE;
        return null;
      }
    }
  }
}