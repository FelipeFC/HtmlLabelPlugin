﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: InternalsVisibleTo("HtmlLabel.Forms.Plugin.Shared.Tests")]
namespace LabelHtml.Forms.Plugin.Abstractions
{
	internal class RendererHelper
	{
		private readonly Label _label;
		private readonly string _text;
		private readonly IList<KeyValuePair<string, string>> _styles;
		private static readonly string[] _supportedProperties = new []
			{
				Label.TextProperty.PropertyName,
				Label.FontAttributesProperty.PropertyName,
				Label.FontFamilyProperty.PropertyName,
				Label.FontSizeProperty.PropertyName,
				Label.HorizontalTextAlignmentProperty.PropertyName,
				Label.TextColorProperty.PropertyName
			};
		private const string _systemFontFamilies = "-apple-system,system-ui,BlinkMacSystemFont,Segoe UI";

		public RendererHelper(Label label, string text)
		{
			_label = label ?? throw new ArgumentNullException(nameof(label));
			_text = text?.Trim();
			_styles = new List<KeyValuePair<string, string>>();
		}

		public void AddFontAttributesStyle(FontAttributes fontAttributes)
		{
			if (fontAttributes == FontAttributes.Bold)
            {
				AddStyle("font-weight", "bold");
			}
			else if (fontAttributes == FontAttributes.Italic)
			{
				AddStyle("font-style", "italic");
			}
		}

		public void AddFontFamilyStyle(string fontFamily)
        {
			var fontFamilyValue = string.IsNullOrWhiteSpace(fontFamily)
				? string.Empty
				: $",{fontFamily}";

			AddStyle("font-family", $"'{_systemFontFamilies}{fontFamilyValue}'");
        }

		public void AddFontSizeStyle(double fontSize)
		{
			AddStyle("font-size", $"{fontSize}px");
		}

		public void AddTextColorStyle(Color color)
		{
			if (color.IsDefault)
            {
                return;
            }

			var red = (int)(color.R * 255);
			var green = (int)(color.G * 255);
			var blue = (int)(color.B * 255);
			var alpha = color.A;
			var hex = $"#{red:X2}{green:X2}{blue:X2}";
			var rgba = $"rgba({red},{green},{blue},{alpha})";
			AddStyle("color", hex);
			AddStyle("color", rgba);
		}

		public void AddHorizontalTextAlignStyle(TextAlignment textAlignment)
		{
			if (textAlignment == TextAlignment.Center)
			{
				AddStyle("text-align", "center");
			}
			else if (textAlignment == TextAlignment.End)
			{
				AddStyle("text-align", "right");
				AddStyle("text-align", "end");
			}
		}

		public override string ToString()
		{
			if (string.IsNullOrWhiteSpace(_text))
			{
				return null;
			}

			AddFontAttributesStyle(_label.FontAttributes);
			AddFontFamilyStyle(_label.FontFamily);
			AddTextColorStyle(_label.TextColor);
			AddHorizontalTextAlignStyle(_label.HorizontalTextAlignment);

			if (_label.FontSize != Device.GetNamedSize(NamedSize.Default, typeof(Label)))
			{
				AddFontSizeStyle(_label.FontSize);
			}

			var style = GetStyle();
			return $"<div style=\"{style}\">{_text}</div>";
		}

		public string GetStyle()
		{
			var builder = new StringBuilder();

			foreach (KeyValuePair<string, string> style in _styles)
			{
				_ = builder.Append($"{style.Key}:{style.Value};");
			}

			var css = builder.ToString();
			if (_styles.Any())
			{
				css = css.Substring(0, css.Length - 1);
			}

			return css;
		}

		public static bool RequireProcess(string propertyName) => _supportedProperties.Contains(propertyName);

		public static async Task HandleUriAsync(HtmlLabel label, string url)
		{
			if (url == null || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
			{
				return;
			}

			var args = new WebNavigatingEventArgs(WebNavigationEvent.NewPage, new UrlWebViewSource { Url = url }, url);

			label.SendNavigating(args);

			if (args.Cancel)
			{
				return;
			}

			var uri = new Uri(url);

			Task<bool> navigatedTask = NeedBrowser(uri)
				? LaunchBrowserAsync(label, uri)
				: LaunchAppAsync(uri);

			var navigated = await navigatedTask.ConfigureAwait(false);
			if (navigated)
			{
				label.SendNavigated(args);
			}			
		}

		private static bool NeedBrowser(Uri uri) =>
			uri.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase) ||
			uri.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase);

		private static async Task<bool> LaunchBrowserAsync(HtmlLabel label, Uri uri)
		{
			if (label.BrowserLaunchOptions == null)
			{
				await Browser.OpenAsync(uri).ConfigureAwait(false);
				return true;
			}

			return await Browser.OpenAsync(uri, label.BrowserLaunchOptions).ConfigureAwait(false);
		}

		private static async Task<bool> LaunchAppAsync(Uri uri)
		{
			var canOpen = await Launcher.CanOpenAsync(uri).ConfigureAwait(false);
			if (canOpen)
			{
				await Launcher.OpenAsync(uri).ConfigureAwait(false);
				return true;
			}
			return false;
		}

		private void AddStyle(string selector, string value)
		{
			_styles.Add(new KeyValuePair<string, string>(selector, value));
		}
	}
}