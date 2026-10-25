# PresentationFramework.Theme

> This project targets Windows XP only. It was completed by AI (DeepSeek).

English | [简体中文](README.md)

A WPF control theme library that draws directly with **Win32 visual styles (UXTHEME)**.

After merging one `ResourceDictionary`, mapped WPF controls render with the real parts of the
current Windows theme instead of WPF's approximation. The library provides both a **plain** and
a **transition** version.

## Comparison with native Win32

`NativeComparison.exe` is a pure Win32/WinForms comparison window using
`Application.EnableVisualStyles()` + a comctl32 v6 manifest. It creates native Button / CheckBox /
ComboBox / TrackBar / ProgressBar / TabControl / ScrollBar / StatusBar / ToolTip / ContextMenu
controls directly:

```powershell
.\NativeComparison\bin\Release\NativeComparison.exe --out=native.png
```

![WPF vs native](docs/native-vs-wpf.png)

### What is UxTheme and what is not

- **Yes**: background/border pixels are drawn with `OpenThemeData(hwnd, class)` +
  `DrawThemeBackground`; sizes and nine-patch margins come from `GetThemePartSize` /
  `GetThemeMargins`.
- **No**: text is not drawn with `DrawThemeText`; it still uses WPF `ContentPresenter`
  (font, line height, padding are decided by WPF/template). ScrollBar arrows overlay WPF `Path`;
  top-level menus use system-color highlighting; some missing parts fall back to approximate
  margins/layout.
- `HackImage` first renders a theme part to a 64×40 or natural-size bitmap, then stretches it
  as a nine-patch. Native controls call `DrawThemeBackground` directly at the final size, so
  corners, radii, and hairlines can differ slightly.
- The accurate description is therefore: **use the real UxTheme part bitmaps as WPF template
  background assets**, not "the whole control is natively drawn by UxTheme".

## Quick start

Merge the theme dictionary into application (or window) resources; implicit styles apply
automatically:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="pack://application:,,,/PresentationFramework.Theme;component/theme.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

With transitions (cross-fade on state change, 150 ms):

```xml
<ResourceDictionary Source="pack://application:,,,/PresentationFramework.Theme;component/theme.withtransition.xaml" />
```

Reference these assemblies: `PresentationFramework.Theme.dll`, `ThemeExplorer.Core.dll`.

> Merge the dictionary before creating windows/controls. Replacing it at runtime only affects
> controls created afterwards.

## Demo coverage

`ThemeDemo` is a scrollable, screenshot-friendly control gallery covering:

- **Mapped controls**: `Button`, `RepeatButton`, `ToggleButton`, `CheckBox` (three-state),
  `RadioButton`, `TextBox`, `PasswordBox`, `RichTextBox`, `ComboBox`, `Slider`, `ScrollBar`,
  `ProgressBar`, `GroupBox`, `TabControl`/`TabItem`, `StatusBar`/`StatusBarItem`,
  `ContextMenu`/`MenuItem`, `Menu`, `ToolTip`, `Separator`.
- **Complete state strips**: Button part 1's 6 states, CheckBox part 3's 12 states,
  RadioButton part 2's 8 states, Edit part 1's 5 states, rendered state-by-state with
  `HackImage`.
- **Still using built-in WPF styles**: `ListBox`, `ListView`, `TreeView`, `DataGrid`,
  `Calendar`, `DatePicker`, `Expander`, `ToolBar`, `Label`, `Hyperlink`, `Image`.

| Control | Theme mapping | Covered states |
| --- | --- | --- |
| Button / RepeatButton / ToggleButton | ✅ | Normal / Hot / Pressed / Disabled / Default (Toggle Checked) |
| CheckBox | ✅ | Unchecked / Checked / Mixed × Normal / Hot / Pressed / Disabled |
| RadioButton | ✅ | Unchecked / Checked × Normal / Hot / Pressed / Disabled |
| TextBox / PasswordBox / RichTextBox | ✅ | Normal / Hot / Focused / Disabled / ReadOnly |
| ComboBox | ✅ | Normal / Hot / Focused / Disabled; dropdown item Normal / Highlighted |
| ScrollBar | ✅ | Arrow Normal / Hot / Pressed / Disabled; Thumb Normal / Hot / Pressed / Disabled |
| Slider | ✅ | Track / Thumb Normal / Hot / Pressed / Focused / Disabled |
| ProgressBar | ✅ | Bar / Fill (Normal) |
| GroupBox | ✅ | Normal / Disabled |
| TabControl / TabItem | ✅ | Normal / Hot / Selected / Disabled / Focused (top tabs) |
| StatusBar / ContextMenu / Menu / ToolTip / Separator | ✅ | Basic Win32 border/item/separator |

> Note: literally covering **every WPF control and every state** would require complete templates
> for ComboBox, ScrollBar, DataGrid, Calendar, Ribbon, etc. (Microsoft's Aero theme is roughly
> 6000 lines of XAML). This version first completes the most common input/button controls and all
> their theme states; the gallery places the remaining controls in the same window so they can be
> replaced one by one.

## Lazy loading and resource reclamation

- **Merging XAML alone loads no images.** Styles and templates are instantiated lazily by WPF;
  `ThemeImage` attached properties only record class/part/state. Actual drawing happens when the
  element is `Loaded`.
- **Only parts entering the visual tree are loaded.** Each `HackImage` requests its image from
  `ThemeImageProvider` when loaded; images with the same theme, state, and size are shared through
  a **weak-reference cache**.
- **Unload releases immediately.** On `Unloaded`, `HackImage.Source` is set to null, ongoing
  transitions are cancelled, and the host drops its strong reference to `ThemeImageData`; the
  cache only keeps `WeakReference`, with no static strong references.
- **Native resources are created and destroyed immediately.** Each image uses one DIB section +
  memory DC; after drawing, `SelectObject` is restored, then `DeleteObject`, `DeleteDC`; the
  `HTHEME` is closed with `using`.
- **Transition resources are released too.** `HackImageTransition` disposes its 9
  `WriteableBitmap`s and unsubscribes from `CompositionTarget.Rendering` on completion or
  interruption.

Self-test quantifies this:

```text
ThemeDemo.exe --selftest --out=<directory>
ThemeDemo.exe --transition --selftest --out=<directory>
```

Example output (Windows 11 / 125% DPI, gallery instance):

```text
images-created-after-merge=0       # merging dictionary only: 0
images-created-before-show=0       # window constructed but not shown: 0
images-created-after-show=55       # created on demand after showing (all gallery states + new controls)
images-cached-while-open=55        # held by controls
images-cached-after-close=0        # zero after close + GC
```

The transition build also verifies that 50 ms after a state switch the image is still old and at
350 ms it has switched; the plain build switches immediately. Actual logs:
`transition-check=early-changed:False;late-changed:True` (transition) and
`early-changed:True;late-changed:True` (plain). Popup templates are also really opened and
verified: `contextmenu-open=True`, `tooltip-open=True`, `combobox-open=True`.

## Custom templates

Use `ThemeImage` attached properties on any `HackImage`:

```xml
<Window xmlns:pt="clr-namespace:System.Windows.Controls;assembly=PresentationFramework.Theme"
        xmlns:th="clr-namespace:Microsoft.Windows.Themes;assembly=PresentationFramework.Theme">
  <pt:HackImage th:ThemeImage.Class="Button"
                th:ThemeImage.Part="1"
                th:ThemeImage.State="1"
                th:ThemeImage.TransitionDuration="0:0:0.15" />
</Window>
```

- `Class` / `Part` / `State` use Windows SDK `xxx_P_*` / `xxx_S_*` constant values.
- `HackThickness` is set automatically from the theme's `TMT_SIZINGMARGINS`; no manual values.
- Fixed-size glyphs (check box / radio button) can also target `Image`; attached properties
  support both `HackImage` and `Image`.
- Transitions only apply to `HackImage` (`Image` has no intermediate buffer);
  `TransitionDuration` of 0 switches immediately.

## Diagnostics API

```csharp
int created = ThemeImageDiagnostics.CreatedImageCount; // total created
int cached  = ThemeImageDiagnostics.CachedImageCount;  // still alive in weak cache
ThemeImageDiagnostics.ClearCache();                    // clear cache keys (images held by controls stay alive)
```

## Portable test package (TestDemo)

`TestDemo/` is a publish directory that can be copied directly to Windows XP:

```text
ThemeDemo.exe
PresentationFramework.Theme.dll
ThemeExplorer.Core.dll
NativeComparison.exe
Run-All.bat
Run-Plain.bat
Run-Transition.bat
Run-Native.bat
Run-Demo.bat
```

On XP, double-click `Run-All.bat`; it writes under `TestDemo\logs`:

```text
logs\plain\selftest.log
logs\plain\theme-plain.png
logs\transition\selftest.log
logs\transition\theme-withtransition.png
logs\native-win32.png
```

`NativeComparison.exe` is the native Win32 comparison window used to determine the real
appearance of the current XP theme.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release -SelfTest
```

`build.ps1 -SelfTest` runs both plain and transition self-tests and checks the logs.

## Compatibility and limitations

- .NET Framework 4.0, C# 6, AnyCPU; Windows XP and later (WPF 4.0 + UXTHEME).
- Uses `VisualStyle.Open(className)` with a zero-window mode; no `HwndHost`/hidden window needed.
- On XP, classes without `TMT_SIZINGMARGINS` use border width or conservative defaults for
  nine-patch slicing.
- If the theme changes at runtime, already-loaded controls do not automatically re-skin; newly
  loaded controls use the new theme (cache keys include theme file/color scheme/size).
- The current **Win32 mapping** covers the table above; `ListBox`, `ListView`, `TreeView`,
  `DataGrid`, `Calendar`, and `DatePicker` still use built-in WPF styles and are included in the
  gallery for future completion.
- Slider ticks (`TickPlacement`), the full editable ComboBox experience, the smooth ScrollBar
  thumb appearance, left/bottom/right TabControl tabs, and MenuItem check/icon columns are still
  simplified; complex states will be completed iteratively.
- Glyph images are composited against `SystemColors.Control`; on a white/custom background a
  small block of control color may be visible at check-box corners.
- System DPI only; no per-monitor DPI switching (net40 limitation).
- Transitions are image cross-fades, not WPF animated properties; they do not cause layout jitter.

## Directory

```text
PresentationFramework.Theme/   library (HackImage, ThemeImage, DIB rendering, theme dictionaries)
ThemeDemo/                     gallery demo + lazy-loading/reclamation/transition self-tests
WpfApplicationTest/            original HackImage/transition test application
NativeComparison/              pure Win32/WinForms comparison window
build.ps1                      build + both self-test suites
docs/                          screenshots of both themes
```

## References and attribution

- **Theme Explorer**: reference project for inspecting Win32 UxTheme class / part / state and
  properties.
  This project's part IDs, state mapping, and Theme Explorer preview approach are based on it.
  Project URL: https://github.com/mity/theme-explorer
