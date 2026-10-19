#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Mindscape.Raygun4Net;

/// <summary>
/// Creates the message builders on first use, not when the Raygun client is created.
/// </summary>
/// <remarks>
/// This prevents a dependency cycle when a message builder depends on ILogger and the Raygun logger is registered.
/// </remarks>
internal sealed class LazyMessageBuilders : IEnumerable<IMessageBuilder>
{
  private readonly object _lock = new();
  private readonly Func<IEnumerable<IMessageBuilder>> _getBuilders;
  private IEnumerable<IMessageBuilder>? _builders;

  public LazyMessageBuilders(Func<IEnumerable<IMessageBuilder>> getBuilders)
  {
    _getBuilders = getBuilders;
  }

  public IEnumerator<IMessageBuilder> GetEnumerator()
  {
    return GetBuilders().GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  private IEnumerable<IMessageBuilder> GetBuilders()
  {
    // Re-entered while creating the message builders, from a constructor that logs an error
    if (Monitor.IsEntered(_lock))
    {
      return Enumerable.Empty<IMessageBuilder>();
    }

    lock (_lock)
    {
      return _builders ??= _getBuilders();
    }
  }
}
