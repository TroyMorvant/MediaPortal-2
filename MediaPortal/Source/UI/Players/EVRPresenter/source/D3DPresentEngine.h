//////////////////////////////////////////////////////////////////////////
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
//////////////////////////////////////////////////////////////////////////

#include <d3d9.h>
#include <dxva2api.h>

class D3DPresentEngine : public SchedulerCallback
{
public:

  // State of the Direct3D device.
  enum DeviceState
  {
    DeviceOK,
    DeviceReset,    // The device was reset OR re-created.
    DeviceRemoved,  // The device was removed.
  };

  D3DPresentEngine(HRESULT& hr);
  virtual ~D3DPresentEngine();

  // GetService: Returns the IDirect3DDeviceManager9 interface.
  virtual HRESULT GetService(REFGUID guidService, REFIID riid, void** ppv);
  virtual HRESULT CheckFormat(D3DFORMAT format);

  // The presenter's implementation of IMFVideoDisplayControl calls these methods.
  HRESULT SetVideoWindow(HWND hwnd);
  HWND    GetVideoWindow() const { return m_hwnd; }
  HRESULT SetDestinationRect(const RECT& rcDest);
  RECT    GetDestinationRect() const { return m_rcDestRect; };

  HRESULT CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue);
  void    ReleaseResources();

  HRESULT CheckDeviceState(DeviceState *pState);
  HRESULT PresentSample(IMFSample* pSample, LONGLONG llTarget); 

  UINT    RefreshRate() const { return m_DisplayMode.RefreshRate; }

protected:
  HRESULT InitializeD3D();
  HRESULT GetSwapChainPresentParameters(IMFMediaType *pType, D3DPRESENT_PARAMETERS* pPP);
  HRESULT CreateD3DDevice();
  HRESULT CreateD3DSample(IDirect3DSwapChain9 *pSwapChain, IMFSample **ppVideoSample);
  HRESULT UpdateDestRect();

  // A derived class can override these handlers to allocate any additional D3D resources.
  virtual HRESULT OnCreateVideoSamples(D3DPRESENT_PARAMETERS& pp) { return S_OK; }
  virtual void    OnReleaseResources() { }

  virtual HRESULT PresentSwapChain(IDirect3DSwapChain9* pSwapChain, IDirect3DSurface9* pSurface);
  virtual void    PaintFrameWithGDI();

  UINT                        m_DeviceResetToken;     // Reset token for the D3D device manager.

  HWND                        m_hwnd;                 // Application-provided destination window.
  RECT                        m_rcDestRect;           // Destination rectangle.
  D3DDISPLAYMODE              m_DisplayMode;          // Adapter's display mode.

  CritSec                     m_ObjectLock;           // Thread lock for the D3D device.

  // COM interfaces
  IDirect3D9Ex                *m_pD3D9;
  IDirect3DDevice9Ex          *m_pDevice;
  IDirect3DDeviceManager9     *m_pDeviceManager;      // Direct3D device manager.
  IDirect3DSurface9           *m_pSurfaceRepaint;     // Surface for repaint requests.
};

