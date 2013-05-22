using MediaPortal.UI.Presentation.DataObjects;
using www.sonicfoundry.com.Mediasite.Services60.Messages;

namespace MediasitePlugin.Items
{
  public class CategoryCollection : ListItem
  {
    public string CategoryName { get; set; }
    public string IconPath { get; set; }
    public string BannerPath { get; set; }
    public PresentationDetails[] Presentations { get; set; }
  }
}