using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Windows;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Numerics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using UnityPlayer;


namespace Map1
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private WinRTBridge.WinRTBridge _bridge;
		
		private SplashScreen splash;
		private Windows.Foundation.Rect splashImageRect;
		private WindowSizeChangedEventHandler onResizeHandler;
		private TypedEventHandler<DisplayInformation, object> onRotationChangedHandler;

        private List<System.Numerics.Vector3> coordinates;
        private List<System.Numerics.Vector3> smileCoordinates;
        private float size = 40;
        private float scale = 1;



        class MyClass
        {
            public static int staticProperty;

            public void m()
            {
                staticProperty = 1;
            }
        }

        public MainPage()
		{
			this.InitializeComponent();

            coordinates = new List<Vector3>();
            smileCoordinates = new List<Vector3>();
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;

			AppCallbacks appCallbacks = AppCallbacks.Instance;
			// Setup scripting bridge
			_bridge = new WinRTBridge.WinRTBridge();
			appCallbacks.SetBridge(_bridge);

			appCallbacks.RenderingStarted += () => { RemoveSplashScreen(); };

#if !UNITY_WP_8_1
			appCallbacks.SetKeyboardTriggerControl(this);
#endif
			appCallbacks.SetSwapChainPanel(GetSwapChainPanel());
			appCallbacks.SetCoreWindowEvents(Window.Current.CoreWindow);
			appCallbacks.InitializeD3DXAML();

			splash = ((App)App.Current).splashScreen;
			GetSplashBackgroundColor();
			OnResize();
			onResizeHandler = new WindowSizeChangedEventHandler((o, e) => OnResize());
			Window.Current.SizeChanged += onResizeHandler;

#if UNITY_WP_8_1
			onRotationChangedHandler = new TypedEventHandler<DisplayInformation, object>((di, o) => { OnRotate(di); });
			ExtendedSplashImage.RenderTransformOrigin = new Point(0.5, 0.5);
			var displayInfo = DisplayInformation.GetForCurrentView();
			displayInfo.OrientationChanged += onRotationChangedHandler;
			OnRotate(displayInfo);

			SetupLocationService();
#endif
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			splash = (SplashScreen)e.Parameter;
			OnResize();
		}

		private void OnResize()
		{
			if (splash != null)
			{
				splashImageRect = splash.ImageLocation;
				PositionImage();
			}
		}

#if UNITY_WP_8_1
		private void OnRotate(DisplayInformation di)
		{
			// system splash screen doesn't rotate, so keep extended one rotated in the same manner all the time
			int angle = 0;
			switch (di.CurrentOrientation)
			{
			case DisplayOrientations.Landscape:
				angle = -90;
				break;
			case DisplayOrientations.LandscapeFlipped:
				angle = 90;
				break;
			case DisplayOrientations.Portrait:
				angle = 0;
				break;
			case DisplayOrientations.PortraitFlipped:
				angle = 180;
				break;
			}
			var rotate = new RotateTransform();
			rotate.Angle = angle;
			ExtendedSplashImage.RenderTransform = rotate;
		}
#endif

		private void PositionImage()
		{
			var inverseScaleX = 1.0f;
			var inverseScaleY = 1.0f;
#if UNITY_WP_8_1
			inverseScaleX = inverseScaleX / DXSwapChainPanel.CompositionScaleX;
			inverseScaleY = inverseScaleY / DXSwapChainPanel.CompositionScaleY;
#endif

			ExtendedSplashImage.SetValue(Windows.UI.Xaml.Controls.Canvas.LeftProperty, splashImageRect.X * inverseScaleX);
			ExtendedSplashImage.SetValue(Windows.UI.Xaml.Controls.Canvas.TopProperty, splashImageRect.Y * inverseScaleY);
			ExtendedSplashImage.Height = splashImageRect.Height * inverseScaleY;
			ExtendedSplashImage.Width = splashImageRect.Width * inverseScaleX;
		}

		private async void GetSplashBackgroundColor()
		{
			try
			{
				StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///AppxManifest.xml"));
				string manifest = await FileIO.ReadTextAsync(file);
				int idx = manifest.IndexOf("SplashScreen");
				manifest = manifest.Substring(idx);
				idx = manifest.IndexOf("BackgroundColor");
				if (idx < 0)  // background is optional
					return;
				manifest = manifest.Substring(idx);
				idx = manifest.IndexOf("\"");
				manifest = manifest.Substring(idx + 1);
				idx = manifest.IndexOf("\"");
				manifest = manifest.Substring(0, idx);
				int value = 0;
				bool transparent = false;
				if (manifest.Equals("transparent"))
					transparent = true;
				else if (manifest[0] == '#') // color value starts with #
					value = Convert.ToInt32(manifest.Substring(1), 16) & 0x00FFFFFF;
				else
					return; // at this point the value is 'red', 'blue' or similar, Unity does not set such, so it's up to user to fix here as well
				byte r = (byte)(value >> 16);
				byte g = (byte)((value & 0x0000FF00) >> 8);
				byte b = (byte)(value & 0x000000FF);

				await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate()
				{
					byte a = (byte)(transparent ? 0x00 : 0xFF);
					ExtendedSplashGrid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
				});
			}
			catch (Exception)
			{ }
		}

		public SwapChainPanel GetSwapChainPanel()
		{
			return DXSwapChainPanel;
		}

		public void RemoveSplashScreen()
		{
			DXSwapChainPanel.Children.Remove(ExtendedSplashGrid);
			if (onResizeHandler != null)
			{
				Window.Current.SizeChanged -= onResizeHandler;
				onResizeHandler = null;

#if UNITY_WP_8_1
				DisplayInformation.GetForCurrentView().OrientationChanged -= onRotationChangedHandler;
				onRotationChangedHandler = null;
#endif
			}
		}

#if !UNITY_WP_8_1
		protected override Windows.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		{
			return new UnityPlayer.XamlPageAutomationPeer(this);
		}

        private Vector3 toVector3(UnityEngine.Vector3 vec)
        {
            Vector3 vector = new Vector3();
            vector.X = vec.x;
            vector.Y = vec.y;
            vector.Z = vec.z;
            return vector;
        }

        UnityEngine.GameObject nearestTable;

        private void ContentGrid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            //if (e.Cumulative.Scale != 1)
            //{
            //    AppCallbacks.Instance.TryInvokeOnAppThread(() =>
            //    {
            //        if (nearestTable != null)
            //        {
            //            //UnityEngine.GameObject.FindGameObjectWithTag("Sphere1").GetComponent<moveSphere>().targetPosition = nearestTable.transform.position;
            //            UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().target = nearestTable.transform;
            //        }

            //        if (size == maxSize)
            //        {

            //            UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().target = null;
            //        }
            //    }, false);

            //}

            dX = dY = 0;
        }
        float maxSize = 40;
        float minSize = 15;
       
        float dX = 0;
        float dY = 0;
        private void ContentGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //float delta = Math.Abs(e.Delta.Scale - 1);
            //if (delta <= 0.005 )
            //{
            //    AppCallbacks.Instance.TryInvokeOnAppThread(() =>
            //    {
            //        UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().delta = 1;
            //    }, false);
            //    return;
            //}

            float newScale = (e.Delta.Scale - 1) * 0.5f + 1;
            size /= (newScale);

            if (size > maxSize)
            {
                size = maxSize;
            }
            if (size < minSize)
            {
                size = minSize;
            }
            if(size > minSize + 5)
            {
                Panel.SlideInBegin();
            }

            StartPoint = e.Position;
            Vector3 position = new Vector3();
            position.X = (float)StartPoint.X;
            position.Y = (float)(this.ActualHeight - StartPoint.Y);
            position.Z = 0;
            float minDist = float.MaxValue;

            AppCallbacks.Instance.TryInvokeOnAppThread(() =>
            {

                var tables = UnityEngine.GameObject.FindGameObjectsWithTag("Table");
                if (coordinates.Count == 0)
                {
                    tables = UnityEngine.GameObject.FindGameObjectsWithTag("Table");
                    foreach (var table in tables)
                    {
                        coordinates.Add(toVector3(table.GetComponent<ClickingScript>().ScreenCoordinates));
                    }
                    //UnityEngine.Debug.Log("added " + coordinates.Count + " tables");
                }

                tables = UnityEngine.GameObject.FindGameObjectsWithTag("Table");
                nearestTable = tables[0];
                //UnityEngine.Debug.Log("founded " + tables.Length + " tables");
                for (int i = 0; i < tables.Length; i++)
                {
                    //UnityEngine.Debug.Log(i);
                    if (i > coordinates.Count)
                    {
                        coordinates.Add(toVector3(tables[i].GetComponent<ClickingScript>().ScreenCoordinates));
                    }
                    coordinates[i] = toVector3(tables[i].GetComponent<ClickingScript>().ScreenCoordinates);
                    coordinates[i] = new System.Numerics.Vector3(coordinates[i].X, coordinates[i].Y, 0);
                    //UnityEngine.Debug.Log(coordinates[i]);
                    float tempDist = Vector3.Distance(position, coordinates[i]);
                    //UnityEngine.Debug.Log("temp distance = " + tempDist);
                    if (tempDist < minDist)
                    {
                        minDist = tempDist;
                        nearestTable = tables[i];
                    }

                }
                //UnityEngine.Debug.Log("minimum distane = " + minDist);
                if (nearestTable != null)
                {
                    //UnityEngine.GameObject.FindGameObjectWithTag("Sphere1").GetComponent<moveSphere>().targetPosition = nearestTable.transform.position;
                    UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().target = nearestTable.transform;
                    UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().coord =
                    coord;
                    //UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().delta = delta;
                }
                if (size == maxSize)
                {
                    UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().target = null;
                }
                UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<RotatigScript>().targetSize = size;

            }, false);
            

        }
        //scale *= e.Delta.Scale;
        UnityEngine.Vector3 coord;

        private void ContentGrid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            //Vector2 position = new Vector2((float)e.Position.X, (float)e.Position.Y);
            Vector3 position = new Vector3();
            position.X = (float)e.Position.X;
            position.Y = (float)(this.ActualHeight - e.Position.Y);
            AppCallbacks.Instance.TryInvokeOnAppThread(() =>
            {
                coord =
                   UnityEngine.Camera.main.ScreenToWorldPoint(new UnityEngine.Vector3(position.X, position.Y, 0));

            }, false);
        }

        private Point StartPoint;

        private void ContentGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            
        }

        private void ContentGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Vector3 position = new Vector3();
            position.X = (float)e.GetPosition(this).X;
            position.Y = (float)(this.ActualHeight - e.GetPosition(this).Y);
            position.Z = 0;
            //Panel.SlideOutBegin();
            if (size <= minSize + 5)
            {
                Panel.SlideOutBegin();
                //    if (size <= 100)
                //{
                AppCallbacks.Instance.TryInvokeOnAppThread(() =>
                    {
                        UnityEngine.GameObject nearestSmile;
                        var smiles = UnityEngine.GameObject.FindGameObjectsWithTag("Smile");
                            // UnityEngine.Debug.Log("length = " + smiles.Length);
                            var cam = UnityEngine.GameObject.FindGameObjectWithTag("MainCamera").GetComponent<UnityEngine.Camera>();
                        if (smileCoordinates.Count == 0)
                        {
                                // UnityEngine.Debug.Log("s");
                                foreach (var smile in smiles)
                            {
                                    //   UnityEngine.Debug.Log("s");
                                    smileCoordinates.Add(toVector3(cam.WorldToScreenPoint(smile.transform.position)));
                            }

                        }


                    // UnityEngine.Debug.Log("length = " + smiles.Length);
                    nearestSmile = smiles[0];
                float minDist = float.MaxValue;


                for (int i = 0; i < smiles.Length; i++)
                {
                        //UnityEngine.Debug.Log(i);
                        if (i > smileCoordinates.Count)
                    {
                        smileCoordinates.Add(toVector3(cam.WorldToScreenPoint(smiles[i].transform.position)));
                    }
                    smileCoordinates[i] = toVector3(cam.WorldToScreenPoint(smiles[i].transform.position));
                    smileCoordinates[i] = new System.Numerics.Vector3(smileCoordinates[i].X, smileCoordinates[i].Y, 0);
                    //UnityEngine.Debug.Log("smile + " + smiles[i].transform.position);

                    float tempDist = Vector3.Distance(position, smileCoordinates[i]);
                        //UnityEngine.Debug.Log("temp distance = " + tempDist);

                    if (tempDist < minDist)
                        {
                            minDist = tempDist;
                            nearestSmile = smiles[i];
                        }

                    }
                    //UnityEngine.Debug.Log("minimum distane = " + minDist);
                    if (nearestSmile != null)
                    {
                        //UnityEngine.Debug.Log(nearestSmile.transform.position);
                        //UnityEngine.GameObject.FindGameObjectWithTag("Sign").transform.position =
                        // new UnityEngine.Vector3(nearestSmile.transform.position.x / 10.0f, nearestSmile.transform.position.y / 10.0f + 0.5f, nearestSmile.transform.position.z / 10.0f + 0.1f);

                    var sign = UnityEngine.GameObject.FindGameObjectWithTag("Sign");
                    //UnityEngine.Debug.Log("sign position " + sign.transform.position);

                    sign.transform.position = 
                        new UnityEngine.Vector3(nearestSmile.transform.position.x, nearestSmile.transform.position.y, nearestSmile.transform.position.z+0.1f);

                    //UnityEngine.Debug.Log("sign new position " + sign.transform.position);

                   
                        //new UnityEngine.Vector3(0, 0, 0);
                    }


            }, false);
            }
        }
#else
		// This is the default setup to show location consent message box to the user
		// You can customize it to your needs, but do not remove it completely if your application
		// uses location services, as it is a requirement in Windows Store certification process
		private async void SetupLocationService()
		{
			AppCallbacks appCallbacks = AppCallbacks.Instance;
			if (!appCallbacks.IsLocationCapabilitySet())
			{
				return;
			}

			const string settingName = "LocationContent";
			bool userGaveConsent = false;

			object consent;
			var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
			var userWasAskedBefore = settings.Values.TryGetValue(settingName, out consent);

			if (!userWasAskedBefore)
			{
				var messageDialog = new Windows.UI.Popups.MessageDialog("Can this application use your location?", "Location services");

				var acceptCommand = new Windows.UI.Popups.UICommand("Yes");
				var declineCommand = new Windows.UI.Popups.UICommand("No");

				messageDialog.Commands.Add(acceptCommand);
				messageDialog.Commands.Add(declineCommand);

				userGaveConsent = (await messageDialog.ShowAsync()) == acceptCommand;
				settings.Values.Add(settingName, userGaveConsent);
			}
			else
			{
				userGaveConsent = (bool)consent;
			}

			if (userGaveConsent)
			{	// Must be called from UI thread
				appCallbacks.SetupGeolocator();
			}
		}
#endif
    }

}
