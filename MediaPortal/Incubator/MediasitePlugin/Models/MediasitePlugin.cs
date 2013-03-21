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
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.DataObjects;
using www.sonicfoundry.com.Mediasite.Services60.Messages;
using MediasiteAPIConnector;
using System.Collections.Generic;
using System.ServiceModel;

namespace MediasitePlugin
{
  /// <summary>
  /// Todo: Add comments.
  /// </summary>
  public class MediasitePlugin : IWorkflowModel
  {
    #region Consts

    private const string API_ENDPOINT = "http://ais.sofodev.com/mediasite/services60/edassixonefive.svc";
    private const string PRIVATE_KEY = "uTBisLe83ZgZExYUYcKkVA==";
    private const string PUBLIC_KEY = "EAJos+ME82eh+rCUA+96tA==";
    private const string APPLICATION = "MediaPortal2";

    public const string MODEL_ID_STR = "89A89847-7523-47CB-9276-4EC544B8F19A";
    public static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    /// <summary>
    /// Another localized string resource.
    /// </summary>
    protected const string COMMAND_TRIGGERED_RESOURCE = "[Mediasite.ButtonTextCommandExecuted]";

    #endregion

    #region Protected properties

    /// <summary>
    /// This property holds a string that we will modify in this tutorial.
    /// </summary>
    protected string _requestTicket;
    private EdasClient _client;
    private ItemsList _presentations;
    private ItemsList _slides;

    #endregion

    #region Ctor & maintainance

    /// <summary>
    /// Constructor... this one is called by the WorkflowManager when this model is loaded due to a screen reference.
    /// </summary>
    public MediasitePlugin()
    {

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

    public string CreateAuthTicket(string mediasiteResourceID)
    {
      var aRequest = new CreateAuthTicketRequest { ApplicationName = APPLICATION, Ticket = _requestTicket, TicketSettings = new CreateAuthTicketSettings() { Username = "MediaPortalUser", ResourceId = mediasiteResourceID, MinutesToLive = 10 } };
      return _client.CreateAuthTicket(aRequest).AuthTicketId;
    }

    public ItemsList LoadPresentations()
    {
      _requestTicket = new APIAuthenticator(API_ENDPOINT, PUBLIC_KEY, PRIVATE_KEY).RequestTicket;
      var binding = new BasicHttpBinding
        {
          ReceiveTimeout = new TimeSpan(0, 5, 0),
          SendTimeout = new TimeSpan(0, 5, 0),
          MaxBufferPoolSize = 2147483647,
          MaxBufferSize = 2147483647,
          MaxReceivedMessageSize = 2147483647,
          HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,

        };
      binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
      binding.Security.Mode = API_ENDPOINT.Substring(0, 5) == "https" ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;

      EndpointAddress endpoint = new EndpointAddress(new Uri(API_ENDPOINT, UriKind.Absolute));
      _client = new EdasClient(binding, endpoint);
      var pRequest = new QueryPresentationsByCriteriaRequest
        {
          Ticket = _requestTicket,
          ApplicationName = APPLICATION,
          QueryCriteria = new PresentationQueryCriteria { StartDate = Convert.ToDateTime("01/01/00"), EndDate = DateTime.Now, PermissionMask = ResourcePermissionMask.Read },
          Options = new QueryOptions { BatchSize = 100, StartIndex = 0 }
        };

      var tpresentations = _client.QueryPresentationsByCriteria(pRequest);
      var list = new ItemsList();
      foreach (PresentationDetails presentation in tpresentations.Presentations)
      {
        ListItem item = new ListItem("Name", presentation.Name);
        PresentationDetails localPresentation = presentation; // Keep local variable to avoid changing values in iterations
        item.Command = new MethodDelegateCommand(() => LoadSlides(localPresentation));
        // TODO: internal ID and URLs are not needed on Skin level
        //item.SetLabel("ID", presentation.Id);
        //item.SetLabel("URL", presentation.VideoUrl + "?AuthTicket=" + CreateAuthTicket(presentation.Id));
        //item.SetLabel("FileURL", presentation.FileServerUrl);
        list.Add(item);
      }
      return list;
    }

    private void LoadSlides(PresentationDetails presentation)
    {
      var slides = _client.QuerySlides(new QuerySlidesRequest 
      { 
          PresentationId = presentation.Id, 
          Ticket = _requestTicket,
          ApplicationName = APPLICATION,
          StartIndex = 0,
          Count = 10
      });
      var list = new ItemsList();
      var dummySlides = new List<SlideDetails> { new SlideDetails { Title = "Dummy 1" }, new SlideDetails { Title = "Dummy 2" } };
      foreach (SlideDetails slide in slides.Slides /*TODO: slides.Slides */)
      {
        ListItem item = new ListItem("Name", slide.Title);
        item.SetLabel("Time", slide.Time.ToString());
        SlideDetails localSlide = slide; // Keep local variable to avoid changing values in iterations
        item.Command = new MethodDelegateCommand(() => ShowSlide(localSlide));
        list.Add(item);
      }
      _slides = list;
    }

    private void ShowSlide(SlideDetails localSlide)
    {
      // TODO: what to do with a single slide?
    }

    /// <summary>
    /// Refreshes the contents of <see cref="Presentations"/> list.
    /// </summary>
    public void RefreshPresentations()
    {
      _presentations = LoadPresentations();
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
      RefreshPresentations();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
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
