namespace AssetPackage
{
    using System;

    /// <summary>
    /// A tracker asset settings.
    /// </summary>
    public class TrackerAssetSettings : BaseSettings
    {
        /// <summary>
        /// Initializes a new instance of the AssetPackage.TrackerAssetSettings
        /// class.
        /// </summary>
        public TrackerAssetSettings() : base()
        {
            // Apply 'Factory' defaults.
            // 
            Port = 3000;
            Secure = false;
            BatchSize = 2;
            LogFile = "TrackerAsset.log";
        }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        ///
        /// <value>
        /// The host.
        /// </value>
        public String Host
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        ///
        /// <value>
        /// The host.
        /// </value>
        public int Port
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use http or https.
        /// </summary>
        ///
        /// <value>
        /// true if secure, false if not.
        /// </value>
        public bool Secure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full pathname of the base file.
        /// </summary>
        ///
        /// <remarks>
        /// Should either be empty or else start with a /. Should not include a
        /// trailing /.
        /// </remarks>
        ///
        /// <value>
        /// The full pathname of the base file.
        /// </value>
        public String BasePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user Authentication token.
        /// </summary>
        ///
        /// <value>
        /// The user token.
        /// </value>
        public String UserToken
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the game tracking code.
        /// </summary>
        ///
        /// <value>
        /// The tracking code.
        /// </value>
        public String TrackingCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the storage.
        /// </summary>
        ///
        /// <value>
        /// The type of the storage.
        /// </value>
        public TrackerAsset.StorageTypes StorageType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the trace format.
        /// </summary>
        ///
        /// <value>
        /// The trace format.
        /// </value>
        public TrackerAsset.TraceFormats TraceFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum size of the batch to be flushed.
        /// </summary>
        ///
        /// <remarks>
        /// A value of 0 results in no limit on the batch size.
        /// </remarks>
        ///
        /// <value>
        /// The maximum size of the batch.
        /// </value>
        public UInt32 BatchSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the log file.
        /// </summary>
        ///
        /// <value>
        /// The log file.
        /// </value>
        public String LogFile
        {
            get;
            set;
        }

    }
}
