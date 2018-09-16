/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Service.QuickSettings;
using Android.Views;
using Java.Lang;
using Java.IO;
using Plugin.Settings;
using Plugin.Settings.Abstractions;
using System;
using Android.Content;

namespace OverscanToggle
{
    [Activity(Label = "OverscanToggle", MainLauncher = true)]
    public class MainActivity : Activity
    {
        public const int DIALOG_TIMEOUT = 15000;
        //public static AlertDialog confirmAlertDialog;
        public static AlertDialog.Builder builder;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // get screen metrics
            var metrics = Resources.DisplayMetrics;
            WindowManager.DefaultDisplay.GetRealMetrics(metrics);   // love the name of this function
                                                                    // "yeah those 'metrics' are cute get me the real metrics"
                                                                    // like to imagine WindowManager.DefaultDisplay as one of those rich criminal guys from crime movies with like 10 bodyguards who kisses his gun every 10 seconds
            int height = metrics.HeightPixels;
            int width = metrics.WidthPixels;


            // modify widgets based on screen metrics

            // the virgin document.getElementById vs the chad
            TextView text_screenres = FindViewById<TextView>(Resource.Id.text_screenres);
            // take that, javascript. lol no generics
            // (i am aware this can be condensed, but S A F E T Y)

            text_screenres.Text = $"Your screen resolution is {width}x{height}";  // TIL Python stole f-strings from C#

            SeekBar sb_left = FindViewById<SeekBar>(Resource.Id.sb_left);
            SeekBar sb_right = FindViewById<SeekBar>(Resource.Id.sb_right);
            SeekBar sb_top = FindViewById<SeekBar>(Resource.Id.sb_top);
            SeekBar sb_bottom = FindViewById<SeekBar>(Resource.Id.sb_bottom);

            sb_left.Max = width;
            sb_right.Max = width;
            sb_top.Max = height;
            sb_bottom.Max = height;

            sb_left.ProgressChanged += ProgressChanged;
            sb_right.ProgressChanged += ProgressChanged;
            sb_top.ProgressChanged += ProgressChanged;
            sb_bottom.ProgressChanged += ProgressChanged;

            sb_left.Progress = OverscanUtil.OverscanLeft;
            sb_right.Progress = OverscanUtil.OverscanRight;
            sb_top.Progress = OverscanUtil.OverscanTop;
            sb_bottom.Progress = OverscanUtil.OverscanBottom;

            // Set up toggle
            Switch sw_toggle = FindViewById<Switch>(Resource.Id.sw_toggle);
            sw_toggle.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                if (e.IsChecked)
                    ShowAlertAndEnableOverscan();
                else
                    OverscanUtil.DisableOverscan();
            };

            // Set up apply button
            Button apply_button = FindViewById<Button>(Resource.Id.btn_apply);
            apply_button.Touch += delegate (object sender, Button.TouchEventArgs e)
            {
                //OverscanUtil.OverscanLeft = sb_left.Progress;
                //OverscanUtil.OverscanRight = sb_right.Progress;
                //OverscanUtil.OverscanTop = sb_top.Progress;
                //OverscanUtil.OverscanBottom = sb_bottom.Progress;
                if (OverscanUtil.OverscanState)
                    ShowAlertAndEnableOverscan();
            };

            // Set up confirmation dialog
            builder = new AlertDialog.Builder(this);
            builder.SetTitle("Does everything look okay?");
            builder.SetMessage("If ignored, your screen will go back to normal in 15 seconds.");
            builder.SetPositiveButton("OK", delegate { });
            builder.SetNegativeButton("Reset", delegate (object sender, DialogClickEventArgs e)
            {
                System.Console.WriteLine("Resetting");
                DisableOverscan();
            });
        }

        public void ShowAlertAndEnableOverscan()
        {
            /**
             * Show a confirmation dialog and enable overscan.
             */
            OverscanUtil.EnableOverscan();
            if (!OverscanUtil.DialogShown)
            {
                System.Console.WriteLine("Showing alert dialog");
                AlertDialog confirmAlertDialog = builder.Create();
                confirmAlertDialog.SetCanceledOnTouchOutside(false);
                Handler mHandler = new Handler((Android.OS.Message msg) =>
                {
                    switch (msg.What)
                    {
                        case 0:
                            if (confirmAlertDialog != null && confirmAlertDialog.IsShowing)
                            {
                                confirmAlertDialog.Cancel();
                                DisableOverscan();
                            }
                            break;

                        default:
                            break;
                    }
                });
                confirmAlertDialog.Show();
                mHandler.SendEmptyMessageDelayed(0, DIALOG_TIMEOUT);

                OverscanUtil.DialogShown = true;
            }
        }

        private void DisableOverscan()
        {
            /**
             * Disables overscan and resets some widgets
             */
            OverscanUtil.DisableOverscan();
            Switch sw_toggle = FindViewById<Switch>(Resource.Id.sw_toggle);
            sw_toggle.Checked = false;
        }

        private void ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            /**
             * Update labels when the sliders have changed
             */
            SeekBar seek_bar = e.SeekBar;
            TextView text_view;
            if (seek_bar.Id == Resource.Id.sb_left) {
                text_view = FindViewById<TextView>(Resource.Id.text_left);
                text_view.Text = $"Left: {e.Progress}px";
                OverscanUtil.OverscanLeft = e.Progress;
            }
            else if (seek_bar.Id == Resource.Id.sb_right) {
                text_view = FindViewById<TextView>(Resource.Id.text_right);
                text_view.Text = $"Right: {e.Progress}px";
                OverscanUtil.OverscanRight = e.Progress;
            }
            else if (seek_bar.Id == Resource.Id.sb_top) {
                text_view = FindViewById<TextView>(Resource.Id.text_top);
                text_view.Text = $"Top: {e.Progress}px";
                OverscanUtil.OverscanTop = e.Progress;
            }
            else if (seek_bar.Id == Resource.Id.sb_bottom) {
                text_view = FindViewById<TextView>(Resource.Id.text_bottom);
                text_view.Text = $"Bottom: {e.Progress}px";
                OverscanUtil.OverscanBottom = e.Progress;
            }

            // since the dimensions were changed, enable the dialog again
            OverscanUtil.DialogShown = false;
        }
    }

    [Service(Name = "OverscanToggle.OverscanToggle.OverscanTileService",
             Permission = Android.Manifest.Permission.BindQuickSettingsTile,
             Label = "Overscan",
             Icon = "@drawable/crop")]
    [IntentFilter(new[] { ActionQsTile })]
    public class OverscanTileService : TileService
    {
        /**
         * Quick settings tile service for the quick settings tile.
         * Fun Fact: for like a month this app was literally nothing but the QS tile
         */
        public override void OnStartListening()
        {
            base.OnStartListening();

            Tile tile = QsTile;
            tile.State = (OverscanUtil.OverscanState? TileState.Active : TileState.Inactive);
        }

        public override void OnClick()
        {
            base.OnClick();

            Tile tile = QsTile;
            if (tile.State == TileState.Active)
            {
                // update visual tile state
                tile.State = TileState.Inactive;
                tile.UpdateTile();

                OverscanUtil.DisableOverscan();
            }
            else if (tile.State == TileState.Inactive)
            {
                // update visual tile state
                tile.State = TileState.Active;
                tile.UpdateTile();

                // todo: show warning dialog for qs tile
                // not a common scenario but if you press the qs tile without having tested it before
                // you're on your own
                OverscanUtil.EnableOverscan();
            }
        }
    }
}

