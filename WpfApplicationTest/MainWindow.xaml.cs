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
			ShowSourceInfo ();
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
		// 显示测试图实际尺寸，帮助用户理解触发条件
		private void ShowSourceInfo ()
		{
			var bmp = Get ("ImgNormal");
			if (bmp == null) { SourceInfoText.Text = "ImgNormal 未加载"; return; }
			SourceInfoText.Text = string.Format (
				"测试图（ImgNormal）实际尺寸：{0}×{1} 像素。当前 XAML 默认 HackThickness 为 100/2，若图片 ≥200 像素则不会触发「小于」条件，请点「自动调整 HackThickness」。",
				bmp.PixelWidth, bmp.PixelHeight);
		}
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
		// 根据 ImgNormal 实际尺寸，自动设置三行测试的 HackThickness，保证恰好触发对应条件
		private void OnAutoAdjustThickness (object sender, RoutedEventArgs e)
		{
			var bmp = Get ("ImgNormal");
			if (bmp == null) { Log ("ImgNormal 未加载，无法自动调整"); return; }
			int w = bmp.PixelWidth, h = bmp.PixelHeight;
			// 宽度小于：L+R > w（触发），T+B < h（不触发）
			int wt = Math.Max (0, h / 4), wl = w;
			var thicknessW = new Thickness (wl, wt, wl, wt);
			ShrinkWFalse.HackThickness = thicknessW;
			ShrinkWTrue.HackThickness = thicknessW;
			// 高度小于：T+B > h（触发），L+R < w（不触发）
			int ht = h, hl = Math.Max (0, w / 4);
			var thicknessH = new Thickness (hl, ht, hl, ht);
			ShrinkHFalse.HackThickness = thicknessH;
			ShrinkHTrue.HackThickness = thicknessH;
			// 全部小于：L+R > w 且 T+B > h
			int m = Math.Max (w, h);
			var thicknessAll = new Thickness (m, m, m, m);
			ShrinkAllFalse.HackThickness = thicknessAll;
			ShrinkAllTrue.HackThickness = thicknessAll;
			LabelW.Text = string.Format ("L+R={0} > 图宽 {1}，T+B={2} < 图高 {3}", wl * 2, w, wt * 2, h);
			LabelH.Text = string.Format ("T+B={0} > 图高 {1}，L+R={2} < 图宽 {3}", ht * 2, h, hl * 2, w);
			LabelAll.Text = string.Format ("L+R={0} > 图宽 {1}，T+B={2} > 图高 {3}", m * 2, w, m * 2, h);
			Log (string.Format ("已根据图尺寸 {0}×{1} 自动设置 HackThickness", w, h));
		}
	}
}