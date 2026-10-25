using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
namespace WpfApplicationTest
{
	public partial class MainWindow: Window
	{
		public MainWindow ()
		{
			InitializeComponent ();
			FreezeResource ("ImgNormal");
			FreezeResource ("ImgHover");
			FreezeResource ("ImgActive");
			FreezeResource ("ImgDisabled");
			FreezeResource ("ImgFocused");
		}
		// 把 XAML 里的 BitmapImage 资源冻结，保证跨线程安全、渲染开销最低
		private void FreezeResource (string key)
		{
			var bmp = FindResource (key) as BitmapSource;
			if (bmp != null && !bmp.IsFrozen && bmp.CanFreeze) bmp.Freeze ();
		}
		private BitmapSource Get (string key) { return (BitmapSource)FindResource (key); }
		// 把 To 设为目标图（null 表示淡出到透明）
		private void SetTo (BitmapSource img) { HackImageTransitions.SetTo (Demo, img); }
		private void Log (string msg) { LogText.Text = DateTime.Now.ToString ("HH:mm:ss.fff") + "  " + msg; }
		private void OnSetNormal (object sender, RoutedEventArgs e) { SetTo (Get ("ImgNormal")); Log ("To = 普通"); }
		private void OnSetHover (object sender, RoutedEventArgs e) { SetTo (Get ("ImgHover")); Log ("To = Hover"); }
		private void OnSetActive (object sender, RoutedEventArgs e) { SetTo (Get ("ImgActive")); Log ("To = Active"); }
		private void OnSetDisabled (object sender, RoutedEventArgs e) { SetTo (Get ("ImgDisabled")); Log ("To = Disabled"); }
		private void OnSetFocused (object sender, RoutedEventArgs e) { SetTo (Get ("ImgFocused")); Log ("To = Focus"); }
		// 淡出到透明：null 被当作透明图，触发 opacity 1→0 的过渡
		private void OnFadeOut (object sender, RoutedEventArgs e) { SetTo (null); Log ("To = null（淡出）"); }
		// 从透明淡入：Source 已为 null，触发 opacity 0→1 的过渡
		private void OnFadeIn (object sender, RoutedEventArgs e) { SetTo (Get ("ImgNormal")); Log ("To = A（淡入）"); }
		// 快速打断：第一次触发 A→Hover 过渡，第二次立刻中断并反向 Hover→A
		private void OnRapidToggle (object sender, RoutedEventArgs e)
		{
			SetTo (Get ("ImgHover"));
			SetTo (Get ("ImgNormal"));
			Log ("打断测试：A→Hover 立即 Hover→A");
		}
		private void OnDirectClick (object sender, RoutedEventArgs e) { Log ("版本 A 按钮点击"); }
		private void OnSmoothClick (object sender, RoutedEventArgs e) { Log ("版本 B 按钮点击"); }
	}
}