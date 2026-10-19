#nullable enable

using System;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the user provider on the first GetUser call, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// This prevents a dependency cycle when the user provider depends on ILogger and the Raygun logger is registered.
/// </remarks>
internal sealed class LazyRaygunUserProvider : IRaygunUserProvider
{
  private readonly object _lock = new();
  private readonly Func<IRaygunUserProvider?> _getProvider;
  private IRaygunUserProvider? _provider;
  private bool _isCreated;

  public LazyRaygunUserProvider(Func<IRaygunUserProvider?> getProvider)
  {
    _getProvider = getProvider;
  }

  public RaygunIdentifierMessage? GetUser()
  {
    return GetProvider()?.GetUser();
  }

  private IRaygunUserProvider? GetProvider()
  {
    // Re-entered while creating the user provider, from a constructor that logs an error
    if (Monitor.IsEntered(_lock))
    {
      return null;
    }

    lock (_lock)
    {
      // Null is a valid provider, so a flag records that it was created
      if (!_isCreated)
      {
        _provider = _getProvider();
        _isCreated = true;
      }

      return _provider;
    }
  }
}
