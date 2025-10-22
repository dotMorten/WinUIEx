﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinUIEx;

namespace WinUIExSample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if !DISABLE_XAML_GENERATED_MAIN // With custom main, we'd rather want to do this code in main
            if (WebAuthenticator.CheckOAuthRedirectionActivation())
                return;
            fss = SimpleSplashScreen.ShowDefaultSplashScreen();
#endif
            this.InitializeComponent();
#if UNPACKAGED
            // Use file-based persistence since we can't rely on default storage for window persistence when unpackaged
            WinUIEx.WindowManager.PersistenceStorage = new FilePersistence("WinUIExPersistence.json");
#endif
        }

        internal SimpleSplashScreen? fss { get; set; }
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var window = new MainWindow();
            var splash = new SplashScreen(window);
            splash.Completed += (s, e) =>
            {
                m_window = e as WindowEx;
            };
            splash.Activated += Splash_Activated;
        }

        private void Splash_Activated(object sender, WindowActivatedEventArgs args)
        {
            ((Window)sender).Activated -= Splash_Activated;
            fss?.Hide(TimeSpan.FromSeconds(1));
            fss = null;
        }

        private WindowEx? m_window;

        public WindowEx? MainWindow => m_window;

#if UNPACKAGED
        private class FilePersistence : IDictionary<string, object>
        {
            private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
            private readonly string _file;

            public FilePersistence(string filename)
            {
                _file = filename;
                try
                {
                    if (File.Exists(filename))
                    {
                        var jo = System.Text.Json.Nodes.JsonObject.Parse(File.ReadAllText(filename)) as JsonObject;
                        foreach(var node in jo)
                        {
                            if (node.Value is JsonValue jvalue && jvalue.TryGetValue<string>(out string value))
                                _data[node.Key] = value;
                        }
                    }
                }
                catch { }
            }
            private void Save()
            {
                JsonObject jo = new JsonObject();
                foreach(var item in _data)
                {
                    if (item.Value is string s) // In this case we only need string support. TODO: Support other types
                        jo.Add(item.Key, s);
                }
                File.WriteAllText(_file, jo.ToJsonString());
            }
            public object this[string key] { get => _data[key]; set { _data[key] = value; Save();} }

            public ICollection<string> Keys => _data.Keys;

            public ICollection<object> Values => _data.Values;

            public int Count => _data.Count;

            public bool IsReadOnly => false;

            public void Add(string key, object value)
            {
                _data.Add(key, value); Save();
            }

            public void Add(KeyValuePair<string, object> item)
            {
                _data.Add(item.Key, item.Value); Save();
            }

            public void Clear()
            {
                _data.Clear(); Save();
            }

            public bool Contains(KeyValuePair<string, object> item) => _data.Contains(item);

            public bool ContainsKey(string key) => _data.ContainsKey(key);

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotImplementedException(); // TODO

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => throw new NotImplementedException(); // TODO

            public bool Remove(string key) => throw new NotImplementedException(); // TODO

            public bool Remove(KeyValuePair<string, object> item) => throw new NotImplementedException(); // TODO

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => throw new NotImplementedException(); // TODO

            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException(); // TODO
        }
#endif
    }

#if DISABLE_XAML_GENERATED_MAIN
    /// <summary>
    /// Program class
    /// </summary>
    public static class Program
    {
        [global::System.STAThreadAttribute]
        static void Main(string[] args)
        {
#pragma warning disable CS0618 // Type or member is obsolete - We keep this here for testing the obsolete API
            if (WebAuthenticator.CheckOAuthRedirectionActivation(true))
                return;
#pragma warning restore CS0618 // Type or member is obsolete
#if UNPACKAGED
            var fss = SimpleSplashScreen.ShowSplashScreenImage("Assets\\SplashScreen.scale-100.png");
#else
            var fss = SimpleSplashScreen.ShowDefaultSplashScreen();
#endif
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            global::Microsoft.UI.Xaml.Application.Start((p) => {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App() { fss = fss };
            });
        }
    }
#endif
}
