// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Android.App;

namespace LeanCloud {
  public partial class AVInstallation : AVObject {
    /// <summary>
    /// The device token of the installation. Typically generated by APNS or GCM.
    /// </summary>
    [AVFieldName("deviceToken")]
    public string DeviceToken {
      get { return GetProperty<string>(); }
      internal set { SetProperty<string>(value); }
    }

    /// <summary>
    /// Returns push type.
    /// </summary>
    [AVFieldName("pushType")]
    internal string PushType {
      get { return GetProperty<string>(); }
    }
  }
}
