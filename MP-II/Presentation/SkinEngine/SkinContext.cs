﻿#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D9;
using Presentation.SkinEngine.Players;
using Presentation.SkinEngine.Players.Geometry;
using Presentation.SkinEngine.Skin;
using MediaPortal.Core;
using MediaPortal.Presentation.Players;


namespace Presentation.SkinEngine
{                         
  /// <summary>
  /// Holds context variables which are used by the skin controls.
  /// </summary>
  public class SkinContext
  {
    public static bool UseBatching = false;
    public static bool IsValid = false;
    #region variables

    private static string _skinName = "default";
    private static float _skinWidth = 720;
    private static float _skinHeight = 576;
    private static List<ExtendedMatrix> _groupTransforms = new List<ExtendedMatrix>();
    private static List<ExtendedMatrix> _layoutTransforms = new List<ExtendedMatrix>();
    private static List<double> _opacity = new List<double>();
    private static double _finalOpacity = 1.0;
    private static ExtendedMatrix _finalTransform = new ExtendedMatrix();
    private static ExtendedMatrix _finalLayoutTransform = new ExtendedMatrix();
    private static ExtendedMatrix _tempTransform = null;
    private static Form _form;
    private static bool _gradientInUse;
    private static Vector3 _gradientPosition;
    private static Vector2 _gradientSize;
    private static DateTime _mouseTimer;
    private static bool _isRendering = false;
    private static Geometry _geometry = new Geometry();
    private static CropSettings _cropSettings = new CropSettings();
    public static DateTime _now;
    public static bool HandlingInput;
    private static Property _mouseUsedProperty = new Property(false);
    private static Property _timeProperty = new Property(DateTime.Now);
    private static bool _mouseHidden = false;
    private static DateTime _lastAction = DateTime.Now;
    public static uint TimePassed;
    public static System.Drawing.SizeF _skinZoom = new System.Drawing.SizeF(1, 1);
    public static float Z = 0.0f;
    #endregion

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ShowCursor")]
    internal extern static Int32 ShowCursor(bool bShow);

    static public void AddOpacity(double opacity)
    {
      _finalOpacity *= opacity;
      _opacity.Add(_finalOpacity);
    }

    static public void RemoveOpacity()
    {
      _opacity.RemoveAt(_opacity.Count - 1);
      if (_opacity.Count > 0)
      {
        _finalOpacity = _opacity[_opacity.Count - 1];
      }
      else
        _finalOpacity = 1.0;
    }

    static public double Opacity
    {
      get
      {
        return _finalOpacity;
      }
    }

    /// <summary>
    /// Gets or sets the Applications' main windows form.
    /// </summary>
    /// <value>The form.</value>
    public static Form Form
    {
      get { return _form; }
      set
      {
        _form = value;
        Application.Idle += new EventHandler(Application_Idle);
      }
    }

    /// <summary>
    /// Gets or sets the name of the skin currently in use
    /// </summary>
    /// <value>The name of the skin.</value>
    public static string SkinName
    {
      get
      {
        return _skinName;
      }
      set
      {
        _skinName = value;
      }
    }

    /// <summary>
    /// Gets or sets the width of the skin
    /// </summary>
    /// <value>The width.</value>
    public static float Width
    {
      get { return _skinWidth; }
      set { _skinWidth = value; }
    }

    /// <summary>
    /// Gets or sets the height of the skin
    /// </summary>
    /// <value>The height.</value>
    public static float Height
    {
      get { return _skinHeight; }
      set { _skinHeight = value; }
    }

    public static System.Drawing.SizeF Zoom
    {
      get { return _skinZoom; }
      set { _skinZoom = value; }
    }

    public static List<ExtendedMatrix> Transforms
    {
      get
      {
        return _groupTransforms;
      }
      set
      {
        _groupTransforms = value;
      }
    }
    /// <summary>
    /// Adds the transform matrix to the current transform stack
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void AddTransform(ExtendedMatrix matrix)
    {
      if (_groupTransforms.Count > 0)
      {
        _groupTransforms.Add(matrix.Multiply(_groupTransforms[_groupTransforms.Count - 1]));
      }
      else
      {
        _groupTransforms.Add(matrix);
      }
      UpdateFinalTransform(_groupTransforms[_groupTransforms.Count - 1]);
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveTransform()
    {
      if (_groupTransforms.Count > 0)
      {
        _groupTransforms.RemoveAt(_groupTransforms.Count - 1);
      }
      if (_groupTransforms.Count > 0)
      {
        UpdateFinalTransform(_groupTransforms[_groupTransforms.Count - 1]);
      }
      else
      {
        UpdateFinalTransform(new ExtendedMatrix());
      }
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void UpdateFinalTransform(ExtendedMatrix matrix)
    {
      _finalTransform = matrix;
    }

    /// <summary>
    /// Gets or sets the final matrix.
    /// </summary>
    /// <value>The final matrix.</value>
    public static ExtendedMatrix FinalMatrix
    {
      get { return _finalTransform; }
      set { _finalTransform = value; }
    }


    public static void AddLayoutTransform(ExtendedMatrix matrix)
    {
      if (_layoutTransforms.Count > 0)
      {
        _layoutTransforms.Add(matrix.Multiply(_layoutTransforms[_layoutTransforms.Count - 1]));
      }
      else
      {
        _layoutTransforms.Add(matrix);
      }
      UpdateFinalLayoutTransform(_layoutTransforms[_layoutTransforms.Count - 1]);
    }

    /// <summary>
    /// Removes the top transform from the transform stack.
    /// </summary>
    public static void RemoveLayoutTransform()
    {
      if (_layoutTransforms.Count > 0)
      {
        _layoutTransforms.RemoveAt(_layoutTransforms.Count - 1);
      }
      if (_layoutTransforms.Count > 0)
      {
        UpdateFinalLayoutTransform(_layoutTransforms[_layoutTransforms.Count - 1]);
      }
      else
      {
        UpdateFinalLayoutTransform(new ExtendedMatrix());
      }
    }

    /// <summary>
    /// Sets the final transform.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    public static void UpdateFinalLayoutTransform(ExtendedMatrix matrix)
    {
      _finalLayoutTransform = matrix;
    }

    /// <summary>
    /// Gets or sets the final matrix.
    /// </summary>
    /// <value>The final matrix.</value>
    public static ExtendedMatrix FinalLayoutTransform
    {
      get { return _finalLayoutTransform; }
      set { _finalLayoutTransform = value; }
    }

    /// <summary>
    /// Gets or sets the temporary transform.
    /// </summary>
    /// <value>The temporary transform.</value>
    public static ExtendedMatrix TemporaryTransform
    {
      get { return _tempTransform; }
      set { _tempTransform = value; }
    }


    /// <summary>
    /// Gets or sets a value indicating whether mouse was used used the last 5 seconds
    /// </summary>
    /// <value><c>true</c> if [mouse used]; otherwise, <c>false</c>.</value>
    public static bool MouseUsed
    {
      get { return (bool)_mouseUsedProperty.GetValue(); }
      set
      {
        if (value != MouseUsed)
        {
          _mouseUsedProperty.SetValue(value);
          if (value)
          {
            _mouseTimer = DateTime.Now;
            if (_mouseHidden)
            {
              ShowCursor(true);
              _mouseHidden = false;
            }
          }
        }
        else if (value)
        {
          _mouseTimer = DateTime.Now;
          _lastAction = DateTime.Now;
        }
      }
    }

    private static void Application_Idle(object sender, EventArgs e)
    {
      if (MouseUsed)
      {
        TimeSpan ts = DateTime.Now - _mouseTimer;
        if (ts.TotalSeconds >= 2)
        {
          MouseUsed = false;
          if (!_mouseHidden)
          {
            if (ServiceScope.Get<IApplication>().IsFullScreen)
            {
              ShowCursor(false);
              _mouseHidden = true;
            }
          }
        }
      }
    }

    public static Property MouseUsedProperty
    {
      get { return _mouseUsedProperty; }
      set { _mouseUsedProperty = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether application is rendering or not.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is rendering; otherwise, <c>false</c>.
    /// </value>
    public static bool IsRendering
    {
      get { return _isRendering; }
      set { _isRendering = value; }
    }

    /// <summary>
    /// Gets or sets the current geometry.
    /// </summary>
    /// <value>The geometry.</value>
    public static Geometry Geometry
    {
      get { return _geometry; }
      set { _geometry = value; }
    }


    /// <summary>
    /// Gets or sets the crop settings.
    /// </summary>
    /// <value>The crop settings.</value>
    public static CropSettings CropSettings
    {
      get { return _cropSettings; }
      set
      {
        if (value != null)
          _cropSettings = value;
      }
    }

    public static DateTime Now
    {
      get { return _now; }
      set
      {
        _now = value;
        _timeProperty.SetValue(_now);
      }
    }

    public static Property TimeProperty
    {
      get { return _timeProperty; }
      set { _timeProperty = value; }
    }

    public static bool ScreenSaverActive
    {
      get
      {
        TimeSpan ts = DateTime.Now - _lastAction;
        if (ts.TotalSeconds < 30) return false;
        if (!ServiceScope.Get<IApplication>().IsFullScreen) return false;
        PlayerCollection players = ServiceScope.Get<PlayerCollection>();
        if (players.Count > 0)
        {
          if (players[0].IsVideo) return false;
        }
        return true;
      }
      set
      {
        _lastAction = DateTime.Now;
      }
    }

  }
}
