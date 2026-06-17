#region MIT LICENSE

// Copyright 2023 Brandon Thetford
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// See https://opensource.org/license/MIT/

#endregion

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SnapsInAZfs.Interop.Zfs.ZfsTypes;

namespace SnapsInAZfs.Interop.Zfs.ZfsCommandRunner;

/// <summary>
///     Class representing all of the string values of a ZFS object and its properties
/// </summary>
public sealed class RawZfsObject
{
    private static readonly Logger          Logger                      = LogManager.GetCurrentClassLogger( );
    private static readonly HashSet<string> MandatorySnapshotProperties = [ .. IZfsProperty.KnownSnapshotProperties.Union ( [ "type", "used" ], StringComparer.OrdinalIgnoreCase ) ];

    private static readonly HashSet<string> MandatoryZfsRecordProperties = [ .. IZfsProperty.KnownDatasetProperties.Union ( [ "type", "used", "available" ], StringComparer.OrdinalIgnoreCase ) ];

    /// <summary>
    ///     Creates a new instance of a <see cref="RawZfsObject" />
    /// </summary>
    /// <param name="kind">The string corresponding to the 'type' attribute of a ZFS object</param>
    public RawZfsObject( string kind )
    {
        Kind = kind;
    }

    public bool HasAllMandatorySnapshotProperties  => !MandatorySnapshotProperties.Except ( Properties.Keys ).Any ( );
    public bool HasAllMandatoryZfsRecordProperties => !MandatoryZfsRecordProperties.Except ( Properties.Keys ).Any ( );

    /// <summary>The string corresponding to the 'type' attribute of a ZFS object</summary>
    public string Kind { get; }

    /// <summary>
    ///     A collection of <see cref="RawProperty" /> values for a ZFS object, indexed and sorted by their 'property' name values
    /// </summary>
    public SortedList<string, RawProperty> Properties { get; } = new( );

    public void AddRawProperty( in string propertyName, in string propertyValue, in string propertySource )
    {
        Properties.Add( propertyName, new( propertyName, propertyValue, propertySource ) );
    }

    /// <exception cref="InvalidOperationException">Cannot convert to ZfsRecord - Missing one or more properties</exception>
    /// <exception cref="ArgumentNullException">Cannot convert to ZfsRecord - Missing name <paramref name="dsName" /></exception>
    public bool ConvertToDatasetAndAddToCollection( string dsName, ConcurrentDictionary<string, ZfsRecord> datasets )
    {
        Logger.Trace( "Parsing property values for {0} {1}", Kind, dsName );
        if ( string.IsNullOrWhiteSpace( dsName ) )
        {
            throw new ArgumentNullException( nameof( dsName ), "Cannot convert to ZfsRecord - Missing name" );
        }

        if ( !HasAllMandatoryZfsRecordProperties )
        {
            throw new InvalidOperationException( "Cannot convert to ZfsRecord - Missing one or more properties" );
        }

        // Note the type parameter doesn't matter for this static method call - it just has to be something
        if ( !TryParseDatasetPropertiesFromRawZfsObject( dsName,
                                                         this,
                                                         out ZfsProperty<bool>? enabled,
                                                         out ZfsProperty<bool>? takeSnapshots,
                                                         out ZfsProperty<bool>? pruneSnapshots,
                                                         out ZfsProperty<DateTimeOffset>? lastFrequentSnapshotTimestamp,
                                                         out ZfsProperty<DateTimeOffset>? lastHourlySnapshotTimestamp,
                                                         out ZfsProperty<DateTimeOffset>? lastDailySnapshotTimestamp,
                                                         out ZfsProperty<DateTimeOffset>? lastWeeklySnapshotTimestamp,
                                                         out ZfsProperty<DateTimeOffset>? lastMonthlySnapshotTimestamp,
                                                         out ZfsProperty<DateTimeOffset>? lastYearlySnapshotTimestamp,
                                                         out ZfsProperty<string>? recursion,
                                                         out ZfsProperty<string>? template,
                                                         out ZfsProperty<int>? retentionFrequent,
                                                         out ZfsProperty<int>? retentionHourly,
                                                         out ZfsProperty<int>? retentionDaily,
                                                         out ZfsProperty<int>? retentionWeekly,
                                                         out ZfsProperty<int>? retentionMonthly,
                                                         out ZfsProperty<int>? retentionYearly,
                                                         out ZfsProperty<int>? retentionPruneDeferral,
                                                         out ZfsProperty<string>? sourceSystem,
                                                         out long bytesAvailable,
                                                         out long bytesUsed ) )
        {
            Logger.Error( "Failed parsing object {0} from ZFS", dsName );
            return false;
        }

        string parentName = dsName.GetZfsPathParent( );
        bool isRootDs = dsName == parentName;
        ZfsRecord newDs = ZfsRecord.CreateInstanceFromAllProperties( dsName,
                                                                     Kind,
                                                                     enabled.Value,
                                                                     takeSnapshots.Value,
                                                                     pruneSnapshots.Value,
                                                                     lastFrequentSnapshotTimestamp.Value,
                                                                     lastHourlySnapshotTimestamp.Value,
                                                                     lastDailySnapshotTimestamp.Value,
                                                                     lastWeeklySnapshotTimestamp.Value,
                                                                     lastMonthlySnapshotTimestamp.Value,
                                                                     lastYearlySnapshotTimestamp.Value,
                                                                     recursion.Value,
                                                                     template.Value,
                                                                     retentionFrequent.Value,
                                                                     retentionHourly.Value,
                                                                     retentionDaily.Value,
                                                                     retentionWeekly.Value,
                                                                     retentionMonthly.Value,
                                                                     retentionYearly.Value,
                                                                     retentionPruneDeferral.Value,
                                                                     sourceSystem.Value,
                                                                     bytesAvailable,
                                                                     bytesUsed,
                                                                     isRootDs ? null : datasets[ parentName ] );
        datasets[ dsName ] = newDs;
        if ( !isRootDs )
        {
            datasets[ parentName ].AddDataset( newDs );
        }

        return true;
    }

    /// <summary>
    ///     Attempts to parse all the raw string properties in this <see cref="RawZfsObject" /> as <see cref="ZfsProperty{T}" />s, create
    ///     a <see cref="Snapshot" /> from those properties, and add it to the <paramref name="snapshots" /> collection, as well as its
    ///     parent <see cref="ZfsRecord" /> in <paramref name="datasets" />
    /// </summary>
    /// <param name="snapName">The ZFS identifier (path) of the resulting parsed <see cref="Snapshot" /></param>
    /// <param name="datasets">
    ///     The collection containing the parent <see cref="ZfsRecord" /> of the resulting parsed <see cref="Snapshot" />
    /// </param>
    /// <param name="snapshots">The collection to add the resulting parsed <see cref="Snapshot" /> to</param>
    public bool ConvertToSnapshotAndAddToCollections( string snapName, ConcurrentDictionary<string, ZfsRecord> datasets, ConcurrentDictionary<string, Snapshot> snapshots )
    {
        Logger.Trace( "Parsing property values for snapshot {0}", snapName );

        if ( string.IsNullOrWhiteSpace( snapName ) )
        {
            throw new ArgumentNullException( nameof( snapName ), "Cannot convert to Snapshot - Missing name" );
        }

        if ( !HasAllMandatorySnapshotProperties )
        {
            throw new InvalidOperationException( "Cannot convert to Snapshot - Missing one or more properties" );
        }

        if ( !TryParseDatasetPropertiesFromRawZfsObject( snapName, this, out ZfsProperty<bool>? enabled, out ZfsProperty<bool>? takeSnapshots, out ZfsProperty<bool>? pruneSnapshots, out ZfsProperty<DateTimeOffset>? lastFrequentSnapshotTimestamp, out ZfsProperty<DateTimeOffset>? lastHourlySnapshotTimestamp, out ZfsProperty<DateTimeOffset>? lastDailySnapshotTimestamp, out ZfsProperty<DateTimeOffset>? lastWeeklySnapshotTimestamp, out ZfsProperty<DateTimeOffset>? lastMonthlySnapshotTimestamp, out ZfsProperty<DateTimeOffset>? lastYearlySnapshotTimestamp, out ZfsProperty<string>? recursion, out ZfsProperty<string>? template, out ZfsProperty<int>? retentionFrequent, out ZfsProperty<int>? retentionHourly, out ZfsProperty<int>? retentionDaily, out ZfsProperty<int>? retentionWeekly, out ZfsProperty<int>? retentionMonthly, out ZfsProperty<int>? retentionYearly, out ZfsProperty<int>? retentionPruneDeferral, out ZfsProperty<string>? sourceSystem, out _, out _ ) )
        {
            Logger.Warn( "Failed parsing snapshot {0} - Possibly not a SIAZ snapshot - Skipping object", snapName );
            return false;
        }

        if ( !DateTimeOffset.TryParse( Properties[ ZfsPropertyNames.SnapshotTimestampPropertyName ].Value, out DateTimeOffset snapshotTimestamp ) )
        {
            Logger.Debug( "{0} value {1} not valid for snapshot {2} - Skipping object", ZfsPropertyNames.SnapshotTimestampPropertyName, Properties[ ZfsPropertyNames.SnapshotTimestampPropertyName ].Value, snapName );
            return false;
        }

        string parentName = snapName.GetZfsPathParent( );
        ZfsRecord dataset = datasets[ parentName ];
        snapshots[ snapName ] = dataset.CreateAndAddSnapshot( snapName,
                                                              enabled.Value,
                                                              takeSnapshots.Value,
                                                              pruneSnapshots.Value,
                                                              lastFrequentSnapshotTimestamp.Value,
                                                              lastHourlySnapshotTimestamp.Value,
                                                              lastDailySnapshotTimestamp.Value,
                                                              lastWeeklySnapshotTimestamp.Value,
                                                              lastMonthlySnapshotTimestamp.Value,
                                                              lastYearlySnapshotTimestamp.Value,
                                                              recursion.Value,
                                                              template.Value,
                                                              retentionFrequent.Value,
                                                              retentionHourly.Value,
                                                              retentionDaily.Value,
                                                              retentionWeekly.Value,
                                                              retentionMonthly.Value,
                                                              retentionYearly.Value,
                                                              retentionPruneDeferral.Value,
                                                              Properties[ ZfsPropertyNames.SnapshotPeriodPropertyName ].Value,
                                                              sourceSystem.Value,
                                                              in snapshotTimestamp,
                                                              dataset );
        Logger.Trace( "Snapshot object {0} added to {1} collection and parent {2}", snapName, nameof( snapshots ), parentName );
        return true;
    }

    private static bool TryParseDatasetPropertiesFromRawZfsObject (
      string dsName,
      RawZfsObject rawZfsObject,
      [NotNullWhen ( true )] out ZfsProperty<bool>? enabled,
      [NotNullWhen ( true )] out ZfsProperty<bool>? takeSnapshots,
      [NotNullWhen ( true )] out ZfsProperty<bool>? pruneSnapshots,
      [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? lastFrequentSnapshotTimestamp,
      [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? lastHourlySnapshotTimestamp,
      [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? lastDailySnapshotTimestamp,
      [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? lastWeeklySnapshotTimestamp,
      [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? lastMonthlySnapshotTimestamp,
      [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? lastYearlySnapshotTimestamp,
      [NotNullWhen ( true )] out ZfsProperty<string>? recursion,
      [NotNullWhen ( true )] out ZfsProperty<string>? template,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionFrequent,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionHourly,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionDaily,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionWeekly,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionMonthly,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionYearly,
      [NotNullWhen ( true )] out ZfsProperty<int>? retentionPruneDeferral,
      [NotNullWhen ( true )] out ZfsProperty<string>? sourceSystem,
      out long bytesAvailable,
      out long bytesUsed
    )
    {
      bytesAvailable = 0;
      bytesUsed      = 0;
      Unsafe.SkipInit ( out enabled );
      Unsafe.SkipInit ( out takeSnapshots );
      Unsafe.SkipInit ( out pruneSnapshots );
      Unsafe.SkipInit ( out lastFrequentSnapshotTimestamp );
      Unsafe.SkipInit ( out lastHourlySnapshotTimestamp );
      Unsafe.SkipInit ( out lastDailySnapshotTimestamp );
      Unsafe.SkipInit ( out lastWeeklySnapshotTimestamp );
      Unsafe.SkipInit ( out lastMonthlySnapshotTimestamp );
      Unsafe.SkipInit ( out lastYearlySnapshotTimestamp );
      Unsafe.SkipInit ( out recursion );
      Unsafe.SkipInit ( out template );
      Unsafe.SkipInit ( out retentionFrequent );
      Unsafe.SkipInit ( out retentionHourly );
      Unsafe.SkipInit ( out retentionDaily );
      Unsafe.SkipInit ( out retentionWeekly );
      Unsafe.SkipInit ( out retentionMonthly );
      Unsafe.SkipInit ( out retentionYearly );
      Unsafe.SkipInit ( out retentionPruneDeferral );
      Unsafe.SkipInit ( out sourceSystem );

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.EnabledPropertyName, rawZfsObject, out enabled ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.TakeSnapshotsPropertyName, rawZfsObject, out takeSnapshots ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.PruneSnapshotsPropertyName, rawZfsObject, out pruneSnapshots ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.DatasetLastFrequentSnapshotTimestampPropertyName, rawZfsObject, out lastFrequentSnapshotTimestamp ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.DatasetLastHourlySnapshotTimestampPropertyName, rawZfsObject, out lastHourlySnapshotTimestamp ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.DatasetLastDailySnapshotTimestampPropertyName, rawZfsObject, out lastDailySnapshotTimestamp ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.DatasetLastWeeklySnapshotTimestampPropertyName, rawZfsObject, out lastWeeklySnapshotTimestamp ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.DatasetLastMonthlySnapshotTimestampPropertyName, rawZfsObject, out lastMonthlySnapshotTimestamp ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.DatasetLastYearlySnapshotTimestampPropertyName, rawZfsObject, out lastYearlySnapshotTimestamp ) )
      {
        return false;
      }

      recursion    = ZfsProperty<string>.CreateWithoutParent ( ZfsPropertyNames.RecursionPropertyName, rawZfsObject.Properties [ ZfsPropertyNames.RecursionPropertyName ].Value, rawZfsObject.Properties [ ZfsPropertyNames.RecursionPropertyName ].Source == ZfsPropertySourceConstants.Local );
      sourceSystem = ZfsProperty<string>.CreateWithoutParent ( ZfsPropertyNames.SourceSystem, rawZfsObject.Properties [ ZfsPropertyNames.SourceSystem ].Value, rawZfsObject.Properties [ ZfsPropertyNames.SourceSystem ].Source == ZfsPropertySourceConstants.Local );
      template     = ZfsProperty<string>.CreateWithoutParent ( ZfsPropertyNames.TemplatePropertyName, rawZfsObject.Properties [ ZfsPropertyNames.TemplatePropertyName ].Value, rawZfsObject.Properties [ ZfsPropertyNames.TemplatePropertyName ].Source == ZfsPropertySourceConstants.Local );

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionFrequentPropertyName, rawZfsObject, out retentionFrequent ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionHourlyPropertyName, rawZfsObject, out retentionHourly ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionDailyPropertyName, rawZfsObject, out retentionDaily ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionWeeklyPropertyName, rawZfsObject, out retentionWeekly ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionMonthlyPropertyName, rawZfsObject, out retentionMonthly ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionYearlyPropertyName, rawZfsObject, out retentionYearly ) )
      {
        return false;
      }

      if ( !TryParsePropertyByName ( dsName, ZfsPropertyNames.SnapshotRetentionPruneDeferralPropertyName, rawZfsObject, out retentionPruneDeferral ) )
      {
        return false;
      }

      if ( rawZfsObject.Kind != ZfsPropertyValueConstants.Snapshot && !long.TryParse ( rawZfsObject.Properties [ ZfsNativePropertyNames.Available ].Value, out bytesAvailable ) )
      {
        Logger.Debug ( "{0} value {1} not valid for {2} {3} - skipping object", ZfsNativePropertyNames.Available, rawZfsObject.Properties [ ZfsNativePropertyNames.Available ].Value, rawZfsObject.Kind, dsName );

        bytesAvailable = 0;

        return false;
      }

      // Keeping this one as-is for consistency with the rest of the method
      // ReSharper disable once InvertIf
      if ( rawZfsObject.Kind != ZfsPropertyValueConstants.Snapshot && !long.TryParse ( rawZfsObject.Properties [ ZfsNativePropertyNames.Used ].Value, out bytesUsed ) )
      {
        Logger.Debug ( "{0} value {1} not valid for {2} {3} - skipping object", ZfsNativePropertyNames.Used, rawZfsObject.Properties [ ZfsNativePropertyNames.Used ].Value, rawZfsObject.Kind, dsName );

        bytesUsed = 0;

        return false;
      }

      return true;
    }

    private static bool TryParsePropertyByName ( string objectName, string propertyName, RawZfsObject rawZfsObject, [NotNullWhen ( true )] out ZfsProperty<bool>? parsedProperty )
    {
      Unsafe.SkipInit ( out parsedProperty );

      if ( rawZfsObject.Properties.TryGetValue ( propertyName, out RawProperty rawProp ) )
      {
        if ( ZfsProperty<bool>.TryParse ( rawProp, out parsedProperty ) )
        {
          return true;
        }

        Logger.Debug ( "{0} value {1} not valid for {2} {3} - skipping object", propertyName, rawProp.Value, rawZfsObject.Kind, objectName );
      }

      Logger.Debug ( "Property {0} does not exist for {1} {2} - skipping object", propertyName, rawZfsObject.Kind, objectName );

      return false;
    }

    private static bool TryParsePropertyByName ( string objectName, string propertyName, RawZfsObject rawZfsObject, [NotNullWhen ( true )] out ZfsProperty<DateTimeOffset>? parsedProperty )
    {
      Unsafe.SkipInit ( out parsedProperty );

      if ( rawZfsObject.Properties.TryGetValue ( propertyName, out RawProperty rawProp ) )
      {
        if ( ZfsProperty<DateTimeOffset>.TryParse ( rawProp, out parsedProperty ) )
        {
          return true;
        }

        Logger.Debug ( "{0} value {1} not valid for {2} {3} - skipping object", propertyName, rawProp.Value, rawZfsObject.Kind, objectName );
      }

      Logger.Debug ( "Property {0} does not exist for {1} {2} - skipping object", propertyName, rawZfsObject.Kind, objectName );

      return false;
    }

    private static bool TryParsePropertyByName ( string objectName, string propertyName, RawZfsObject rawZfsObject, [NotNullWhen ( true )] out ZfsProperty<int>? parsedProperty )
    {
      Unsafe.SkipInit ( out parsedProperty );

      if ( rawZfsObject.Properties.TryGetValue ( propertyName, out RawProperty rawProp ) )
      {
        if ( ZfsProperty<int>.TryParse ( rawProp, out parsedProperty ) )
        {
          return true;
        }

        Logger.Debug ( "{0} value {1} not valid for {2} {3} - skipping object", propertyName, rawProp.Value, rawZfsObject.Kind, objectName );
      }

      Logger.Debug ( "Property {0} does not exist for {1} {2} - skipping object", propertyName, rawZfsObject.Kind, objectName );

      return false;
    }
}
