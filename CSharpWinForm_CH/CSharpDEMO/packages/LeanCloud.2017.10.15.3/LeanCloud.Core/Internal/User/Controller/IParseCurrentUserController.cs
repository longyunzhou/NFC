// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This Source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this Source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Core.Internal {
  public interface IAVCurrentUserController : IAVObjectCurrentController<AVUser> {
    Task<string> GetCurrentSessionTokenAsync(CancellationToken cancellationToken);

    Task LogOutAsync(CancellationToken cancellationToken);
  }
}
