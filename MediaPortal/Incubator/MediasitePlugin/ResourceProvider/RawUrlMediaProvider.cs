using System;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.MediaManagement;

namespace MediasitePlugin.ResourceProvider
{
  public class RawUrlMediaProvider : IBaseResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the local filesystem media provider.
    /// </summary>
    protected const string RAW_URL_MEDIA_PROVIDER_ID_STR = "{0B67EA75-8EB8-41B0-ADBD-6B61C5D65531}";

    /// <summary>
    /// raw url media provider GUID.
    /// </summary>
    public static Guid RAW_URL_MEDIA_PROVIDER_ID = new Guid(RAW_URL_MEDIA_PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public RawUrlMediaProvider()
    {
      _metadata = new ResourceProviderMetadata(RAW_URL_MEDIA_PROVIDER_ID, "MediaSite Url mediaprovider", "Provides Access to Raw Uri", true, true);
    }

    #endregion

    #region IBaseResourceProvider Member

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      result = null;
      if (!IsResource(path))
        return false;
      
      result = new RawUrlResourceAccessor(path);
      return true;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (IsResource(pathStr))
        return new ResourcePath(new[] { new ProviderPathSegment(_metadata.ResourceProviderId, pathStr, true) });
      return null;
    }

    public bool IsResource(string url)
    {
      Uri uri;
      if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out uri))
        return !uri.IsFile;
      return false;
    }

    #endregion

    #region IResourceProvider Member

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    public static ResourcePath ToProviderResourcePath(string path)
    {
      return ResourcePath.BuildBaseProviderPath(RAW_URL_MEDIA_PROVIDER_ID, path);
    }
  }
}
