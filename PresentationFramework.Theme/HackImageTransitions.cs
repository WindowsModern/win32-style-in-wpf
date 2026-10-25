using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace System.Windows.Controls
{
	/// <summary>HackImage 的附加属性过渡：To 变化时自动从当前 Source 过渡到新图（null 视为透明）；完成后以 SetCurrentValue 更新 Source（保留绑定），供 DataTrigger / Interaction.Triggers 使用。</summary>
	public static class HackImageTransitions
	{
		public static readonly DependencyProperty ToProperty = DependencyProperty.RegisterAttached ("To", typeof (ImageSource), typeof (HackImageTransitions), new PropertyMetadata (null, OnToChanged));
		public static readonly DependencyProperty DurationProperty = DependencyProperty.RegisterAttached ("Duration", typeof (Duration), typeof (HackImageTransitions), new PropertyMetadata (new Duration (TimeSpan.FromMilliseconds (500))));
		public static readonly DependencyProperty EasingProperty = DependencyProperty.RegisterAttached ("Easing", typeof (IEasingFunction), typeof (HackImageTransitions), new PropertyMetadata (null));
		private static readonly DependencyPropertyKey IsRunningPropertyKey = DependencyProperty.RegisterAttachedReadOnly ("IsRunning", typeof (bool), typeof (HackImageTransitions), new PropertyMetadata (false));
		public static readonly DependencyProperty IsRunningProperty = IsRunningPropertyKey.DependencyProperty;
		private static readonly DependencyProperty TransitionProperty = DependencyProperty.RegisterAttached ("Transition", typeof (HackImageTransition), typeof (HackImageTransitions), new PropertyMetadata (null));
		public static ImageSource GetTo (DependencyObject d) { return (ImageSource)d.GetValue (ToProperty); }
		public static void SetTo (DependencyObject d, ImageSource value) { d.SetValue (ToProperty, value); }
		public static Duration GetDuration (DependencyObject d) { return (Duration)d.GetValue (DurationProperty); }
		public static void SetDuration (DependencyObject d, Duration value) { d.SetValue (DurationProperty, value); }
		public static IEasingFunction GetEasing (DependencyObject d) { return (IEasingFunction)d.GetValue (EasingProperty); }
		public static void SetEasing (DependencyObject d, IEasingFunction value) { d.SetValue (EasingProperty, value); }
		public static bool GetIsRunning (DependencyObject d) { return (bool)d.GetValue (IsRunningProperty); }
		// To 变化：停旧过渡（取当前帧作新起点）→ null→null 短路 → 同引用短路 → 目标未加载兜底 → 启动过渡（null 参与亦走过渡）
		private static void OnToChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var img = d as HackImage;
			if (img == null) return;
			BitmapSource midFrame = null;
			var old = d.GetValue (TransitionProperty) as HackImageTransition;
			if (old != null)
			{
				midFrame = old.GetCurrentFrame ();
				old.Dispose ();
				d.SetValue (TransitionProperty, null);
				d.SetValue (IsRunningPropertyKey, false);
			}
			var to = e.NewValue as BitmapSource;
			var from = midFrame ?? (img.Source as BitmapSource);
			if (from == null && to == null) return;
			if (from != null && ReferenceEquals (from, to)) return;
			if (to != null && (to.PixelWidth == 0 || to.PixelHeight == 0)) { img.SetCurrentValue (HackImage.SourceProperty, to); return; }
			var trans = new HackImageTransition (img, from, to, GetDuration (img).TimeSpan);
			trans.Easing = GetEasing (img);
			trans.Completed += OnCompleted;
			d.SetValue (TransitionProperty, trans);
			img.SetValue (IsRunningPropertyKey, true);
			trans.Start ();
		}
		// 过渡完成：复位 IsRunning、清 Transition、Dispose 释放像素缓冲
		private static void OnCompleted (object sender, EventArgs e)
		{
			var trans = sender as HackImageTransition;
			if (trans == null) return;
			var target = trans.Target;
			target.SetValue (IsRunningPropertyKey, false);
			target.SetValue (TransitionProperty, null);
			trans.Dispose ();
		}
	}
}