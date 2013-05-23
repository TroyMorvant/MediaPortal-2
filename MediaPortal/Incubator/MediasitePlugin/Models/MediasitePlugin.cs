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
using MediasitePlugin.Items;
using MediasitePlugin.ResourceProvider;
using www.sonicfoundry.com.Mediasite.Services60.Messages;
using System.Collections.Generic;

namespace MediasitePlugin.Models
{
  /// <summary>
  /// Todo: Add comments. Add settings for list of sites/connection details
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
    public static Guid WF_CATEGORIES = new Guid("23DB4E53-EB0D-4315-9F4C-F5E1C13577C7");
    public static Guid WF_PRESENTATIONS = new Guid("4332F0ED-45FE-4845-BE08-C429E0137579");
    public const string KEY_PRESENTATION = "Presentation";

    #endregion

    #region Protected properties

    protected MediasiteHelper _msHelper;
    protected readonly ItemsList _presentations = new ItemsList();
    protected readonly ItemsList _slides = new ItemsList();
    protected readonly ItemsList _categories = new ItemsList();
    protected SlideDetails[] _slideDetails;
    protected PresentationDetails _currentPresentation;
    protected AbstractProperty _currentSlideURLProperty;
    protected AbstractProperty _currentCategoryProperty;
    protected AbstractProperty _currentPresentationProperty;
    protected AbstractProperty _presenterUrlProperty;
    protected AbstractProperty _presenterNameProperty;
    protected AbstractProperty _presentationDescriptionProperty;
    protected string _iconPath;
    protected string _bannerPath;
    protected string _categoryName;

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Constructor... this one is called by the WorkflowManager when this model is loaded due to a screen reference.
    /// </summary>
    public MediasitePlugin()
      : base(false, 500)
    {
      _currentSlideURLProperty = new WProperty(typeof(String), string.Empty);
      _currentCategoryProperty = new WProperty(typeof(CategoryCollection), null);
      _currentPresentationProperty = new WProperty(typeof(PresentationDetails), null);
      _presenterUrlProperty = new WProperty(typeof(string), null);
      _presenterNameProperty = new WProperty(typeof(string), null);
      _presentationDescriptionProperty = new WProperty(typeof(string), null);
      _currentCategoryProperty.Attach(CategoryChanged);
      _currentPresentationProperty.Attach(PresentationChanged);
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets an ItemList of all Categories.
    /// </summary>
    /// 
    public ItemsList Categories
    {
      get
      {
        return _categories;
      }
    }

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

    public CategoryCollection CurrentCategory
    {
      get { return (CategoryCollection) _currentCategoryProperty.GetValue(); }
      set { _currentCategoryProperty.SetValue(value); }
    }

    public AbstractProperty CurrentCategoryProperty
    {
      get { return _currentCategoryProperty; }
    }

    public AbstractProperty CurrentPresentationProperty
    {
      get { return _currentPresentationProperty; }
    }

    public PresentationDetails CurrentPresentation
    {
      get { return (PresentationDetails) _currentPresentationProperty.GetValue(); }
      set { _currentPresentationProperty.SetValue(value); }
    }

    public AbstractProperty PresenterUrlProperty
    {
      get { return _presenterUrlProperty; }
    }

    public string PresenterUrl
    {
      get { return (string) _presenterUrlProperty.GetValue(); }
      set { _presenterUrlProperty.SetValue(value); }
    }

    public AbstractProperty PresenterNameProperty
    {
      get { return _presenterNameProperty; }
    }

    public string PresenterName
    {
      get { return (string) _presenterNameProperty.GetValue(); }
      set { _presenterNameProperty.SetValue(value); }
    }

    public AbstractProperty PresentationDescriptionProperty
    {
      get { return _presentationDescriptionProperty; }
    }

    public string PresentationDescription
    {
      get { return (string) _presentationDescriptionProperty.GetValue(); }
      set { _presentationDescriptionProperty.SetValue(value); }
    }

    public void SetSelectedItem(ListItem item)
    {
      object odetails;
      if (item == null || !item.AdditionalProperties.TryGetValue(KEY_PRESENTATION, out odetails))
        return;

      PresentationDetails details = odetails as PresentationDetails;
      if (details == null)
        return;

      CurrentPresentation = details;
    }

    /// <summary>
    /// Refreshes the contents of <see cref="Presentations"/> list.
    /// </summary>
    public void RefreshPresentations()
    {
      _presentations.Clear();
      CategoryCollection categoryCollection = CurrentCategory;
      if (categoryCollection != null)
      {
        foreach (PresentationDetails presentation in categoryCollection.Presentations)
        {
          ListItem item = new ListItem("Name", presentation.Name);
          PresentationDetails localPresentation = presentation;
          // Keep local variable to avoid changing values in iterations
          item.Command = new MethodDelegateCommand(() => PlayVideo(localPresentation));
          item.AdditionalProperties[KEY_PRESENTATION] = localPresentation;
          _presentations.Add(item);
        }
        _categoryName = categoryCollection.CategoryName;
        _iconPath = categoryCollection.IconPath;
        _bannerPath = categoryCollection.BannerPath;
      }
      _presentations.FireChange();
    }

    /// <summary>
    /// Refreshes the contents of <see cref="Categories"/> list.
    /// </summary>
    public void RefreshCategories()
    {
      // Iterate through the new lecture struct so that you have access to the Category Name, Icon Path, Banner Path (fanart)
      _categories.Clear();
      foreach (CategoryCollection categoryCollection in _msHelper.LectureCollection)
      {
        CategoryCollection localCollection = categoryCollection;
        categoryCollection.Command = new MethodDelegateCommand(() => SelectCategory(localCollection));
        _categories.Add(categoryCollection);
      }
      _categories.FireChange();
    }

    private void CategoryChanged(AbstractProperty property, object oldvalue)
    {
      RefreshPresentations();
    }

    private void PresentationChanged(AbstractProperty property, object oldvalue)
    {
      PresentationDetails details = CurrentPresentation;
      if (details == null)
        return;

      PresentationDescription = details.Description;
      var presenter = details.Presenters.FirstOrDefault();
      if (presenter != null)
      {
        PresenterName = presenter.DisplayName;
        PresenterUrl = presenter.ImageUrl;
      }
    }

    public void SelectCategory(CategoryCollection category)
    {
      CurrentCategory = category;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (category == null)
        return;

      // if previously in videos state - pop that state off the stack
      if (workflowManager.NavigationContextStack.Peek().WorkflowState.StateId == WF_PRESENTATIONS)
        workflowManager.NavigationContextStack.Pop();

      workflowManager.NavigatePushAsync(WF_PRESENTATIONS, new NavigationContextConfig { NavigationContextDisplayLabel = category.CategoryName });
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
      if (mediaSitePlayer == null)
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
      _msHelper = new MediasiteHelper(API_ENDPOINT, PRIVATE_KEY, PUBLIC_KEY, APPLICATION, SOFOSITE);
      RefreshCategories();
      StartTimer();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StopTimer();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
      if (newContext.WorkflowState.StateId == WF_PRESENTATIONS)
      {
        RefreshPresentations();
      }
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
