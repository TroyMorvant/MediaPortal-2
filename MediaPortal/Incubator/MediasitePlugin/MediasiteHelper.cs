using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using www.sonicfoundry.com.Mediasite.Services60.Messages;
using MediasiteAPIConnector;
using MediaPortal.Common.Commands;

namespace MediasitePlugin
{
  public class MediasiteHelper
  {
    protected string _requestTicket;
    private EdasClient _client;
    private SiteProperties _siteProperties;
    private string _applicationName;
    private string _apiEndpoint;
    private string _privateKey;
    private string _publicKey;
    private string _sofoSite;

    private PresentationDetails[] _presentations;
    private SlideContentDetails[] _slides;
   
    public MediasiteHelper(string APIEndpoint, string PrivateKey, string Publickey, string ApplicationName, string SOFOSite)
    {
      _apiEndpoint = APIEndpoint;
      _privateKey = PrivateKey;
      _publicKey = Publickey;
      _applicationName = ApplicationName;
      _sofoSite = SOFOSite;

      InitClient();
      LoadSiteProperties();
      LoadPresentations();
    }

    /// <summary>
    /// returns an array of PresentationDetails
    /// </summary>
    public PresentationDetails[] Presentations
    {
      get
      {
        return _presentations;
      }
    }
    public string SiteName
    {
      get
      {
        return _siteProperties.Name.ToLower();
      }
    }

    /// <summary>
    /// Refreshes Mediasite system properties
    /// </summary>
    public void LoadSiteProperties()
    {
      
      var sitePropertiesResponse = _client.QuerySiteProperties(new QuerySitePropertiesRequest
      {
        Ticket = _requestTicket,
        ApplicationName = _applicationName
      });

      if (sitePropertiesResponse != null)
      {
        _siteProperties = sitePropertiesResponse.Properties;
      }
    }

    /// <summary>
    /// Returns an Authentication Ticket that is used to authorize the user to view the selected resource.
    /// </summary>
    public string CreateAuthTicket(string mediasiteResourceID)
    {
      var aRequest = new CreateAuthTicketRequest { ApplicationName = _applicationName, Ticket = _requestTicket, TicketSettings = new CreateAuthTicketSettings { Username = "MediaPortal2User", ResourceId = mediasiteResourceID, MinutesToLive = 10 } };
      return _client.CreateAuthTicket(aRequest).AuthTicketId;
    }

    /// <summary>
    /// Initializes the Mediasite API Client
    /// </summary>
    public void InitClient()
    {
      _requestTicket = new APIAuthenticator(_apiEndpoint, _publicKey, _privateKey).RequestTicket;
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
      binding.Security.Mode = _apiEndpoint.Substring(0, 5) == "https" ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;

      EndpointAddress endpoint = new EndpointAddress(new Uri(_apiEndpoint, UriKind.Absolute));
      _client = new EdasClient(binding, endpoint);
    }

    /// <summary>
    /// Populates object with presentation array
    /// </summary>
    public void LoadPresentations()
    {
      var pRequest = new QueryPresentationsByCriteriaRequest
      {
        Ticket = _requestTicket,
        ApplicationName = _applicationName,
        QueryCriteria = new PresentationQueryCriteria { StartDate = Convert.ToDateTime("01/01/00"), EndDate = DateTime.Now, PermissionMask = ResourcePermissionMask.Read },
        Options = new QueryOptions { BatchSize = 100, StartIndex = 0 }
      };
      var tpresentations = _client.QueryPresentationsByCriteria(pRequest);

      if (tpresentations.Presentations != null)
      {
        foreach (PresentationDetails _pres in tpresentations.Presentations)
        {
          _pres.VideoUrl = _pres.VideoUrl.Replace("$$NAME$$", GetMP4Content(_pres.Content).FileNameWithExtension).Replace("$$PBT$$", CreateAuthTicket(_pres.Id)).Replace("$$SITE$$", _sofoSite);
        }

        _presentations = tpresentations.Presentations;
      }
    }

    /// <summary>
    /// Returns an array of SlideDetails for a given presentation
    /// </summary>
    /// <param name="presentation"></param>
    /// <returns></returns>
    public SlideDetails[] LoadSlides(PresentationDetails presentation)
    {
      var slides = _client.QuerySlides(new QuerySlidesRequest
      {
        PresentationId = presentation.Id,
        Ticket = _requestTicket,
        ApplicationName = _applicationName,
        StartIndex = 0,
        Count = presentation.SlideCount
      });

      if (slides.Slides != null)
      {
        return slides.Slides;
      }
      return null;
    }

    public PresentationContentDetails GetMP4Content(IEnumerable<PresentationContentDetails> contents)
    {
      return contents.FirstOrDefault(content => content.ContentMimeType == "video/mp4");
    }

    public PresentationContentDetails GetSlideContent(IEnumerable<PresentationContentDetails> contents)
    {
      return contents.FirstOrDefault(content => content.ContentType == PresentationContentTypeDetails.Slides);
    }

    public string GetSlideUrl(PresentationDetails presentation, SlideDetails slide)
    {
      if (presentation == null || slide == null)
        return null;
      return presentation.FileServerUrl.ToLower().Replace(SiteName, "Public") + @"/" +
          presentation.Id + @"/" + String.Format(GetSlideContent(presentation.Content).FileNameWithExtension, slide.Number.ToString("D" + 4));
    }
  }
}
