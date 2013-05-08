#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Models;
using MediasitePlugin.ResourceProvider;
using www.sonicfoundry.com.Mediasite.Services60.Messages;
using MediasitePlugin;

using System.Collections.Generic;


namespace MediasitePlugin
{
  /// <summary>
  /// Todo: Add comments.
  /// </summary>
  public class MediasitePlugin : BaseTimerControlledModel, IWorkflowModel
  {
    #region Consts
    private const string API_ENDPOINT = "http://ais.sofodev.com/mediasite/services60/edassixonefive.svc";
    private const string PRIVATE_KEY = "uTBisLe83ZgZExYUYcKkVA==";
    private const string PUBLIC_KEY = "EAJos+ME82eh+rCUA+96tA==";
    private const string APPLICATION = "MediaPortal2";
    private const string SOFOSITE = "ais.sofodev.com";
    public const string MODEL_ID_STR = "89A89847-7523-47CB-9276-4EC544B8F19A";
    public static Guid MODEL_ID = new Guid(MODEL_ID_STR);
    

    #endregion

    #region Protected properties

    /// <summary>
    /// This property holds a string that we will modify in this tutorial.
    /// </summary>

    protected MediasiteHelper _msHelper;
    protected readonly ItemsList _presentations = new ItemsList();
    protected readonly ItemsList _slides = new ItemsList();
    protected SlideDetails[] _slideDetails;
    protected PresentationDetails _currentPresentation;
    protected AbstractProperty _currentSlideURLProperty;
    protected string _iconPath;
    protected string _bannerPath;
    protected string _categoryName;

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Constructor... this one is called by the WorkflowManager when this model is loaded due to a screen reference.
    /// </summary>
    public MediasitePlugin()
      :base(false, 500)
    {
      _currentSlideURLProperty = new WProperty(typeof(String), string.Empty);
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets an ItemList of all Presentations.
    /// </summary>
    /// 
    public ItemsList Presentations
    {
      get
      {
        return _presentations;
      }
    }
    /// <summary>
    /// Gets an ItemList of slides for current selected Presentation.
    /// </summary>
    public ItemsList Slides
    {
      get
      {
        return _slides;
      }
    }

    public string CurrentSlideURL
    {
      get { return (string) _currentSlideURLProperty.GetValue(); }
      set { _currentSlideURLProperty.SetValue(value); }
    }
    public AbstractProperty CurrentSlideURLProperty
    {
      get { return _currentSlideURLProperty; }
    }

    /// <summary>
    /// Refreshes the contents of <see cref="Presentations"/> list.
    /// </summary>
    public void RefreshPresentations()
    {
      _presentations.Clear();
      //iterate through the new lecture struct so that you have access to the Category Name, Icon Path, Banner Path (fanart)
      foreach (MediasiteHelper.CategoryCollection _item in _msHelper.LectureCollection)
      {
        _categoryName = _item.CategoryName;
        _iconPath = _item.IconPath;
        _bannerPath = _item.BannerPath;

        foreach (PresentationDetails presentation in _item.Presentations)
        {
          ListItem item = new ListItem("Name", presentation.Name);
          PresentationDetails localPresentation = presentation; // Keep local variable to avoid changing values in iterations
          item.Command = new MethodDelegateCommand(() => PlayVideo(localPresentation));
          _presentations.Add(item);
        }
        _presentations.FireChange();
      }
    }

    private void LoadSlides(PresentationDetails presentation)
    {
      _slides.Clear();
      _slideDetails = _msHelper.LoadSlides(presentation);
      foreach (SlideDetails slide in _slideDetails)
      {
        string url = _msHelper.GetSlideUrl(presentation, slide);

        string title = slide.Title;
        if (string.IsNullOrWhiteSpace(title))
          title = string.Format("Time: " + TimeSpan.FromMilliseconds(slide.Time));

        ListItem item = new ListItem("Name", title);
        item.SetLabel("URL", url);
        SlideDetails localSlide = slide;
        item.Command = new MethodDelegateCommand(() => ShowSlide(localSlide));
        _slides.Add(item);
      }
      _slides.FireChange();
    }

    protected override void Update()
    {
      MediaSitePlayer mediaSitePlayer = GetMediaSitePlayer();
      if (mediaSitePlayer == null)
        return;
      
      TimeSpan currentPlayerTime = mediaSitePlayer.CurrentTime;
      var currentSlide = _slideDetails.LastOrDefault(slide => TimeSpan.FromMilliseconds(slide.Time) < currentPlayerTime);
      if (currentSlide != null)
        CurrentSlideURL = _msHelper.GetSlideUrl(_currentPresentation, currentSlide);
    }

    protected MediaSitePlayer GetMediaSitePlayer()
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext ctx = pcm.GetPlayerContext(PlayerChoice.CurrentPlayer);
      if (ctx == null)
        return null;
      return ctx.CurrentPlayer as MediaSitePlayer;

    }
    protected void ShowSlide(SlideDetails slide)
    {
      MediaSitePlayer mediaSitePlayer = GetMediaSitePlayer();
      if (mediaSitePlayer ==  null)
        return;
      mediaSitePlayer.CurrentTime = TimeSpan.FromMilliseconds(slide.Time);
    }

    private void PlayVideo(PresentationDetails presentation)
    {
      _currentPresentation = presentation;

      LoadSlides(presentation);

      IDictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();

      MediaItemAspect providerResourceAspect;
      aspects[ProviderResourceAspect.ASPECT_ID] = providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
      MediaItemAspect mediaAspect;
      aspects[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
      MediaItemAspect videoAspect;
      aspects[VideoAspect.ASPECT_ID] = videoAspect = new MediaItemAspect(VideoAspect.Metadata);

      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlMediaProvider.ToProviderResourcePath(presentation.VideoUrl).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);

      mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, MediaSitePlayer.MEDIASITE_MIMETYPE);
      mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, presentation.Name);
      videoAspect.SetAttribute(VideoAspect.ATTR_STORYPLOT, presentation.Description);

      MediaItem mediaItem = new MediaItem(Guid.Empty, aspects);

      PlayItemsModel.PlayItem(mediaItem);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _msHelper = new MediasiteHelper(API_ENDPOINT,PRIVATE_KEY,PUBLIC_KEY,APPLICATION,SOFOSITE);
      RefreshPresentations();
      StartTimer();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StopTimer();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
