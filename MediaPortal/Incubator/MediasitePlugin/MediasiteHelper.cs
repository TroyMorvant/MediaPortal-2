using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using MediasitePlugin.Items;
using www.sonicfoundry.com.Mediasite.Services60.Messages;
using MediasiteAPIConnector;
using MediaPortal.Common.Commands;
using System.Xml;
using System.Xml.XPath;

namespace MediasitePlugin
{
  public class MediasiteHelper
  {
    protected string _requestTicket;
    EdasClient _client;
    SiteProperties _siteProperties;
    string _applicationName;
    string _apiEndpoint;
    string _privateKey;
    string _publicKey;
    string _sofoSite;

    //PresentationDetails[] _presentations;
    SlideContentDetails[] _slides;
    List<CategoryCollection> _categories = new List<CategoryCollection>();

    public MediasiteHelper(string APIEndpoint, string PrivateKey, string Publickey, string ApplicationName, string SOFOSite)
    {
      _apiEndpoint = APIEndpoint;
      _privateKey = PrivateKey;
      _publicKey = Publickey;
      _applicationName = ApplicationName;
      _sofoSite = SOFOSite;

      InitClient();
      LoadSiteProperties();
      //LoadPresentations();
      LoadCategoryCollection();
    }

    /// <summary>
    /// returns an array of PresentationDetails
    /// </summary>
    public List<CategoryCollection> LectureCollection
    {
      get
      {
        return _categories;
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
    /*
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
     */

    /// <summary>
    /// Populates object with array of presentations where the value of the key "CategoryName" = value
    /// </summary>
    public void LoadCategoryCollection()
    {
      XPathDocument document = new XPathDocument(@"Plugins\mediasitePlugin\CategoryDefinition.xml");
      XPathNavigator navigator = document.CreateNavigator();
      XPathNodeIterator nodes = navigator.Select("/Categories/Category");

      foreach (XPathNavigator item in nodes)
      {
        var _name = item.GetAttribute("Name", "");
        var _iconPath = item.GetAttribute("IconPath", "");
        var _bannerPath = item.GetAttribute("BannerPath", "");

        CategoryCollection _collection = new CategoryCollection();
        _collection.CategoryName = _name;
        _collection.IconPath = _iconPath;
        _collection.BannerPath = _bannerPath;

        var _pResponse = _client.QueryMediasiteKeyValuesByCriteria(new QueryMediasiteKeyValuesByCriteriaRequest() { Key = "CategoryName", Value = _collection.CategoryName, Ticket = _requestTicket, ApplicationName = _applicationName });

        List<PresentationDetails> _tPresList = new List<PresentationDetails>();
        if (_pResponse.KeyValues.Length > 0)
        {
          string[] pIDList = new string[_pResponse.KeyValues.Length];

          for (int i = 0; i < _pResponse.KeyValues.Length; i++)
          {
            pIDList[i] = _pResponse.KeyValues[i].Id;
          }
          var tpresentations = _client.QueryPresentationsById(new QueryPresentationsByIdRequest() { PresentationIdList = pIDList, ApplicationName = _applicationName, Ticket = _requestTicket, IncludeKeyValues = true });
          if (tpresentations.Presentations != null)
          {
            foreach (PresentationDetails _pres in tpresentations.Presentations)
            {
              _pres.VideoUrl = _pres.VideoUrl.Replace("$$NAME$$", GetMP4Content(_pres.Content).FileNameWithExtension).Replace("$$PBT$$", CreateAuthTicket(_pres.Id)).Replace("$$SITE$$", _sofoSite);
            }

            _collection.Presentations = tpresentations.Presentations;
            _categories.Add(_collection);
          }

        }
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

