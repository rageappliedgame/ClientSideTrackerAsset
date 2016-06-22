namespace AssetPackage
{
    using System;

    public static class TrackerExtensions
    {
        ///// <summary>
        ///// Values that represent events.
        ///// </summary>
        //public enum Events
        //{
        //    /// <summary>
        //    /// An enum constant representing the choice option.
        //    /// </summary>
        //    choice,
        //    /// <summary>
        //    /// An enum constant representing the click option.
        //    /// </summary>
        //    click,
        //    /// <summary>
        //    /// An enum constant representing the screen option.
        //    /// </summary>
        //    screen,
        //    /// <summary>
        //    /// An enum constant representing the variable option.
        //    /// </summary>
        //    var,
        //    /// <summary>
        //    /// An enum constant representing the zone option.
        //    /// </summary>
        //    zone,
        //}

        /// <summary>
        /// A choice.
        /// </summary>
        public const string CHOICE = "choice";

        /// <summary>
        /// A click.
        /// </summary>
        public const string CLICK = "click";

        /// <summary>
        /// A screen.
        /// </summary>
        public const string SCREEN = "screen";

        /// <summary>
        /// A variable.
        /// </summary>
        public const string VAR = "var";

        /// <summary>
        /// The zone.
        /// </summary>
        public const string ZONE = "zone";

        /// <summary>
        /// Player selected an option in a presented choice
        /// </summary>
        /// <param name="choiceId">Choice identifier.</param>
        /// <param name="optionId">Option identifier.</param>
        public static void Choice(this TrackerAsset asset, string choiceId, string optionId)
        {
            asset.Trace(new TrackerAsset.TrackerEvent()
            {
                Event = CHOICE,
                Target = choiceId,
                Value = optionId
            });
        }

        /// <summary>
        /// Clicks.
        /// </summary>
        ///
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        public static void Click(this TrackerAsset asset, float x, float y)
        {
            asset.Click(x, y, String.Empty);
        }

        /// <summary>
        /// Clicks.
        /// </summary>
        ///
        /// <param name="x">      The x coordinate. </param>
        /// <param name="y">      The y coordinate. </param>
        /// <param name="target"> Target for the. </param>
        public static void Click(this TrackerAsset asset, float x, float y, string target)
        {
            asset.Trace(new TrackerAsset.TrackerEvent()
            {
                Event = CLICK,
                Target = target,
                Value = String.Format("{0}x{1}", x, y)
            });
        }

        /// <summary>
        /// Screens.
        /// </summary>
        ///
        /// <param name="screenId"> Identifier for the screen. </param>
        public static void Screen(this TrackerAsset asset, string screenId)
        {
            asset.Trace(new TrackerAsset.TrackerEvent()
            {
                Event = SCREEN,
                Target = screenId
            });
        }

        /// <summary>
        /// A meaningful variable was updated in the game.
        /// </summary>
        /// <param name="varName">Variable name.</param>
        /// <param name="value">New value for the variable.</param>
        public static void Var(this TrackerAsset asset, string varName, System.Object value)
        {
            asset.Trace(new TrackerAsset.TrackerEvent()
            {
                Event = VAR,
                Target = varName,
                Value = value
            });
        }

        /// <summary>
        /// Zones.
        /// </summary>
        ///
        /// <param name="zoneId"> Identifier for the zone. </param>
        public static void Zone(this TrackerAsset asset, string zoneId)
        {
            asset.Trace(new TrackerAsset.TrackerEvent()
            {
                Event = ZONE,
                Target = zoneId
            });
        }


    }
}
