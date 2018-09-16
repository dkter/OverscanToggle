using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.IO;
using Plugin.Settings.Abstractions;
using Plugin.Settings;

namespace OverscanToggle
{
    static class OverscanUtil
    {
        /**
         * Important utilities for handling overscan
         */
        
        public static ISettings OverscanStateSettings =>
            CrossSettings.Current;

        public static bool OverscanState
        {
            get => OverscanStateSettings.GetValueOrDefault(nameof(OverscanState), false);
            set => OverscanStateSettings.AddOrUpdateValue(nameof(OverscanState), value);
        }

        public static int OverscanLeft
        {
            get => OverscanStateSettings.GetValueOrDefault(nameof(OverscanLeft), 0);
            set => OverscanStateSettings.AddOrUpdateValue(nameof(OverscanLeft), value);
        }

        public static int OverscanRight
        {
            get => OverscanStateSettings.GetValueOrDefault(nameof(OverscanRight), 0);
            set => OverscanStateSettings.AddOrUpdateValue(nameof(OverscanRight), value);
        }

        public static int OverscanTop
        {
            get => OverscanStateSettings.GetValueOrDefault(nameof(OverscanTop), 0);
            set => OverscanStateSettings.AddOrUpdateValue(nameof(OverscanTop), value);
        }

        public static int OverscanBottom
        {
            get => OverscanStateSettings.GetValueOrDefault(nameof(OverscanBottom), 0);
            set => OverscanStateSettings.AddOrUpdateValue(nameof(OverscanBottom), value);
        }

        public static bool DialogShown
        {
            get => OverscanStateSettings.GetValueOrDefault(nameof(DialogShown), false);
            set => OverscanStateSettings.AddOrUpdateValue(nameof(DialogShown), value);
        }

        public static void EnableOverscan()
        {
            // execute command
            Java.Lang.Process su = Runtime.GetRuntime().Exec("su");
            DataOutputStream suStream = new DataOutputStream(su.OutputStream);
            suStream.WriteBytes($"wm overscan {OverscanLeft},{OverscanTop},{OverscanRight},{OverscanBottom}\n");
            suStream.Flush();
            suStream.WriteBytes("exit\n");
            suStream.Flush();
            su.WaitFor();
            System.Console.WriteLine("Enabled overscan");

            // set persistent overscan state
            OverscanState = true;
        }

        public static void DisableOverscan()
        {
            // execute command
            Java.Lang.Process su = Runtime.GetRuntime().Exec("su");
            DataOutputStream suStream = new DataOutputStream(su.OutputStream);
            suStream.WriteBytes("wm overscan reset\n");
            suStream.Flush();
            suStream.WriteBytes("exit\n");
            suStream.Flush();
            su.WaitFor();
            System.Console.WriteLine("Reset overscan");

            // set persistent overscan state
            OverscanState = false;
        }
    }
}