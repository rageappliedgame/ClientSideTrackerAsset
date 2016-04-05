namespace UCM_Tracker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;

    using AssetManagerPackage;

    using AssetPackage;

    public partial class Form1 : Form
    {
        #region Fields

        TextBoxTraceListener textWriter = null;

        #endregion Fields

        #region Constructors

        public Form1()
        {
            InitializeComponent();

            textWriter = new TextBoxTraceListener(textBox1);

            AssetManager.Instance.Bridge = new Bridge();

            (TrackerAsset.Instance.Settings as TrackerAssetSettings).TraceFormat = TrackerAsset.TraceFormats.xapi;

            // Setup defaults to xApi / net (UCM tracker).
            // 

            // Setup for local storage.
            //(TrackerAsset.Instance.Settings as TrackerAssetSettings).StorageType = TrackerAsset.StorageTypes.local;
            //(TrackerAsset.Instance.Settings as TrackerAssetSettings).TraceFormat = TrackerAsset.TraceFormats.json;

            // The timer is needed to enable the buttons when allowed (due to the async nature of REST requests).
            //
            timer1.Start();

            // In case of net we can do a health check of the server.
            //
            if ((TrackerAsset.Instance.Settings as TrackerAssetSettings).StorageType == TrackerAsset.StorageTypes.net)
            {
                TrackerAsset.Instance.CheckHealth();
            }

            // Catch debugging output.
            // 
            Debug.Listeners.Add(textWriter);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Event handler. Called by button1 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button1_Click(object sender, EventArgs e)
        {
            // Login with username and password.
            TrackerAsset.Instance.Login(userName.Text, password.Text);
        }

        /// <summary>
        /// Event handler. Called by button2 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button2_Click(object sender, EventArgs e)
        {
            // To be used after button1 (user-name/password login).
            TrackerAsset.Instance.Start(trackingCode.Text);

            token.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).UserToken;
        }

        /// <summary>
        /// Event handler. Called by button3 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button3_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Screen("start");

            TrackerAsset.Instance.Var("score", 42);

            TrackerAsset.Instance.Click(128, 256, "Button1");

            TrackerAsset.Instance.Flush();
        }

        /// <summary>
        /// Event handler. Called by button4 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button4_Click(object sender, EventArgs e)
        {
            // Anonymous usage
            TrackerAsset.Instance.Start("a:", trackingCode.Text);
        }

        /// <summary>
        /// Event handler. Called by button5 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button5_Click(object sender, EventArgs e)
        {
            // First parameter is token obtained by login.
            TrackerAsset.Instance.Start(token.Text, trackingCode.Text);

            token.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).UserToken;
        }

        /// <summary>
        /// Event handler. Called by button6 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button6_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Flush();
        }

        /// <summary>
        /// Event handler. Called by clearToolStripMenuItem for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        /// <summary>
        /// Event handler. Called by saveToolStripMenuItem for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog1.FileName, textBox1.Text);
            }
        }

        /// <summary>
        /// Event handler. Called by timer1 for tick events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void timer1_Tick(object sender, EventArgs e)
        {
#warning Could be replaced by PubSubz Event Subscription.

            if (TrackerAsset.Instance.Connected)
            {
                button2.Enabled = true;
            }
            else
            {
                button2.Enabled = false;
                button3.Enabled = false;
                button6.Enabled = false;
            }

            if (TrackerAsset.Instance.Active)
            {
                button3.Enabled = true;
                button6.Enabled = true;
            }
            else
            {
                button3.Enabled = false;
                button6.Enabled = false;
            }

            if (!authToken.Text.Equals((TrackerAsset.Instance.Settings as TrackerAssetSettings).UserToken))
            {
                authToken.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).UserToken;
            }
        }

        /// <summary>
        /// Event handler. Called by trackerServer for text changed events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void trackerServer_TextChanged(object sender, EventArgs e)
        {
            (TrackerAsset.Instance.Settings as TrackerAssetSettings).Host = trackerServer.Text;
        }

        /// <summary>
        /// Event handler. Called by button8 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button8_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.SaveSettings("settings.xml");
        }

        /// <summary>
        /// Event handler. Called by button9 for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void button9_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.LoadSettings("settings.xml");

            trackerServer.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).Host;
            trackingCode.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).TrackingCode;
            token.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).UserToken;
        }

        /// <summary>
        /// Event handler. Called by Form1 for load events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void Form1_Load(object sender, EventArgs e)
        {
            trackerServer.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).Host;
            trackingCode.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).TrackingCode;
            token.Text = (TrackerAsset.Instance.Settings as TrackerAssetSettings).UserToken;
        }

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// See http://www.codeproject.com/KB/trace/TextBoxTraceListener.aspx.
        /// </summary>
        public class TextBoxTraceListener : TraceListener
        {
            #region Fields

            private StringSendDelegate fInvokeWrite;

            /// <summary>
            /// Target for the.
            /// </summary>
            private TextBox fTarget;

            #endregion Fields

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the Swiss.DebugForm.NextGridTraceListener class.
            /// </summary>
            ///
            /// <param name="target"> Target for the. </param>
            public TextBoxTraceListener(TextBox target)
            {
                fTarget = target;
                fInvokeWrite = new StringSendDelegate(SendString);
            }

            #endregion Constructors

            #region Delegates

            /// <summary>
            /// String send delegate.
            /// </summary>
            ///
            /// <param name="message"> The message. </param>
            private delegate void StringSendDelegate(string message);

            #endregion Delegates

            #region Methods

            /// <summary>
            /// When overridden in a derived class, writes the specified message to the listener you create
            /// in the derived class.
            /// </summary>
            ///
            /// <param name="message"> A message to write. </param>
            public override void Write(string message)
            {
                fTarget.Invoke(fInvokeWrite, new object[] { message });
            }

            /// <summary>
            /// When overridden in a derived class, writes a message to the listener you create in the
            /// derived class, followed by a line terminator.
            /// </summary>
            ///
            /// <param name="message"> A message to write. </param>
            public override void WriteLine(string message)
            {
                fTarget.Invoke(fInvokeWrite, new object[] { message + Environment.NewLine });
            }

            /// <summary>
            /// Sends a string.
            /// </summary>
            ///
            /// <param name="message"> A message to write. </param>
            private void SendString(string message)
            {
                // No need to lock text box as this function will only
                // ever be executed from the UI thread!
                fTarget.AppendText(message);
            }

            #endregion Methods
        }

        #endregion Nested Types
    }
}