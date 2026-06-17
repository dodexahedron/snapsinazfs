#region MIT LICENSE

// Copyright 2026 Brandon Thetford
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// See https://opensource.org/license/MIT/

#endregion

using System.Diagnostics.CodeAnalysis;
using SnapsInAZfs.Interop.Zfs.ZfsCommandRunner;

namespace SnapsInAZfs.Interop.Zfs.ZfsTypes;

/// <summary>Extensions for <see cref="ZfsProperty{T}" /></summary>
/// <remarks>
///   These may seem strange to have as extensions, but they avoid duplicating the explicitly closed static generic methods for each
///   closed <see cref="ZfsProperty{T}" />.<br />
///   It also makes overload resolution simpler, since each T only gets one overload.
/// </remarks>
public static class ZfsPropertyExtensions
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

  extension ( ZfsProperty<bool> )
  {
    /// <summary>
    ///   Attempts to parse a <see cref="RawProperty" /> as its <see cref="ZfsProperty{T}" /> (<see langword="bool" />) equivalent
    /// </summary>
    /// <param name="input">The <see cref="RawProperty" /> to parse</param>
    /// <param name="property">
    ///   The parsed <see cref="ZfsProperty{T}" /> (<see langword="bool" />), if successful
    /// </param>
    /// <returns>
    ///   <see langword="true" /> if <paramref name="input" /> was parsed successfully; otherwise <see langword="false" />
    /// </returns>
    /// <remarks>
    ///   <paramref name="property" /> is never null when this method returns <see langword="true" />; otherwise,
    ///   <paramref name="property" /> is always <see langword="null" />
    /// </remarks>
    public static bool TryParse ( RawProperty input, [NotNullWhen ( true )] out ZfsProperty<bool>? property )
    {
      property = null;

      // ReSharper disable once InvertIf
      if ( bool.TryParse ( input.Value, out bool result ) )
      {
        property = ZfsProperty<bool>.CreateWithoutParent ( input.Name, result, input.Source == ZfsPropertySourceConstants.Local );

        return true;
      }

      return false;
    }
    public static ZfsProperty<bool> CreateWithoutParent ( string name, in bool value, bool isLocal = true )
    {
      Logger.Trace ( "Creating ZfsProperty<bool> {0} without parent dataset", name );

      return new ( name, in value, isLocal );
    }
  }

  extension ( ZfsProperty<int> )
  {
    public static ZfsProperty<int> CreateWithoutParent ( string name, in int value, bool isLocal = true )
    {
      Logger.Trace ( "Creating ZfsProperty<int> {0} without parent dataset", name );

      return new ( name, in value, isLocal );
    }
    /// <summary>
    ///   Attempts to parse a <see cref="RawProperty" /> as its <see cref="ZfsProperty{T}" /> (<see langword="int" />) equivalent
    /// </summary>
    /// <param name="input">The <see cref="RawProperty" /> to parse</param>
    /// <param name="property">
    ///   The parsed <see cref="ZfsProperty{T}" /> (<see langword="int" />), if successful
    /// </param>
    /// <returns>
    ///   <see langword="true" /> if <paramref name="input" /> was parsed successfully; otherwise <see langword="false" />
    /// </returns>
    /// <remarks>
    ///   <paramref name="property" /> is never null when this method returns <see langword="true" />; otherwise,
    ///   <paramref name="property" /> is always <see langword="null" />
    /// </remarks>
    public static bool TryParse ( RawProperty input, [NotNullWhen ( true )] out ZfsProperty<int>? property )
    {
      if ( int.TryParse ( input.Value, out int result ) )
      {
        property = ZfsProperty<int>.CreateWithoutParent ( input.Name, result, input.Source == ZfsPropertySourceConstants.Local );

        return true;
      }

      property = null;

      return false;
    }
  }

  extension ( ZfsProperty<DateTimeOffset> )
  {
  public static ZfsProperty<DateTimeOffset> CreateWithoutParent ( string name, in DateTimeOffset value, bool isLocal = true )
  {
    Logger.Trace ( "Creating ZfsProperty<DateTimeOffset> {0} without parent dataset", name );

    return new ( name, in value, isLocal );
  }
    /// <summary>
    ///   Attempts to parse a <see cref="RawProperty" /> as its <see cref="ZfsProperty{T}" /> (<see cref="DateTimeOffset" />)
    ///   equivalent
    /// </summary>
    /// <param name="input">The <see cref="RawProperty" /> to parse</param>
    /// <param name="property">
    ///   The parsed <see cref="ZfsProperty{T}" /> (<see cref="DateTimeOffset" />), if successful
    /// </param>
    /// <returns>
    ///   <see langword="true" /> if <paramref name="input" /> was parsed successfully; otherwise <see langword="false" />
    /// </returns>
    /// <remarks>
    ///   <paramref name="property" /> is never null when this method returns <see langword="true" />; otherwise,
    ///   <paramref name="property" /> is always <see langword="null" />
    /// </remarks>
    public static bool TryParse ( RawProperty input, [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? property )
    {
      if ( DateTimeOffset.TryParse ( input.Value, out DateTimeOffset result ) )
      {
        property = ZfsProperty<DateTimeOffset>.CreateWithoutParent ( input.Name, result, input.Source == ZfsPropertySourceConstants.Local );

        return true;
      }

      property = null;

      return false;
    }
  }

  extension ( ZfsProperty<string> )
  {
    public static ZfsProperty<string> CreateWithoutParent ( string name, string value, bool isLocal = true )
    {
      Logger.Trace ( "Creating ZfsProperty<string> {0} without parent dataset", name );

      return new ( name, in value, isLocal );
    }
  }
}
