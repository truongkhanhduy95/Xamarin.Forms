using System;
using System.ComponentModel;
using Android.Content;
using Android.Support.V7.Widget;
using AView = Android.Views.View;
using Android.Views;
using Xamarin.Forms.Internals;
using AColor = Android.Graphics.Color;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Android.Graphics.Drawables;
using Android.Graphics;
using Xamarin.Forms.Platform.Android.FastRenderers;
using Android.OS;
using Android.Widget;
using Android.Content.Res;
using Android.Support.V4.Widget;
using AAttribute = Android.Resource.Attribute;

namespace Xamarin.Forms.Platform.Android
{
	public class CheckBoxRendererBase :
		AppCompatCheckBox,
		IVisualElementRenderer,
		AView.IOnFocusChangeListener,
		CompoundButton.IOnCheckedChangeListener
	{
		bool _disposed;
		bool _skipInvalidate;
		int? _defaultLabelFor;
		VisualElementTracker _tracker;
		VisualElementRenderer _visualElementRenderer;
		IPlatformElementConfiguration<PlatformConfiguration.Android, CheckBox> _platformElementConfiguration;
		CheckBox _checkBox;

		static int[][] _checkedStates = new int[][]
					{
						new int[] { AAttribute.StateEnabled, AAttribute.StateChecked },
						new int[] { AAttribute.StateEnabled, -AAttribute.StateChecked },
						new int[] { -AAttribute.StateEnabled, AAttribute.StateChecked },
						new int[] { -AAttribute.StateEnabled, -AAttribute.StatePressed },
					};

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;
		public event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;

		public CheckBoxRendererBase(Context context) : base(context) => Init();

		public CheckBoxRendererBase(Context context, int defStyleAttr) : base(context, null, defStyleAttr) => Init();

		void Init()
		{
			SoundEffectsEnabled = false;
			SetOnCheckedChangeListener(this);
			Tag = this;			
			OnFocusChangeListener = this;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;
				_tracker?.Dispose();
				_tracker = null;
				SetOnCheckedChangeListener(null);
				OnFocusChangeListener = null;

				if (Element != null)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;

					if (Android.Platform.GetRenderer(Element) == this)
					{
						Element.ClearValue(Android.Platform.RendererProperty);
					}

					Element = null;
				}
			}

			base.Dispose(disposing);
		}

		public override void Invalidate()
		{
			if (_skipInvalidate)
			{
				_skipInvalidate = false;
				return;
			}

			base.Invalidate();
		}

		Size MinimumSize()
		{
			return new Size();
		}

		SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			if (_disposed)
			{
				return new SizeRequest();
			}
			Measure(widthConstraint, heightConstraint);
			return new SizeRequest(new Size(MeasuredWidth, MeasuredHeight), MinimumSize());
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{

			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			if (!(element is CheckBox checkBox))
			{
				throw new ArgumentException("Element is not of type " + typeof(CheckBox), nameof(element));
			}

			CheckBox oldElement = Element;
			Element = checkBox;

			Performance.Start(out string reference);

			if (oldElement != null)
			{
				oldElement.PropertyChanged -= OnElementPropertyChanged;
			}

			element.PropertyChanged += OnElementPropertyChanged;

			if (_tracker == null)
			{
				_tracker = new VisualElementTracker(this);
			}

			if (_visualElementRenderer == null)
			{
				_visualElementRenderer = new VisualElementRenderer(this);
			}

			OnElementChanged(new ElementChangedEventArgs<CheckBox>(oldElement as CheckBox, Element));
			Element?.SendViewInitialized(Control);
			Performance.Stop(reference);
		}

		protected virtual void OnElementChanged(ElementChangedEventArgs<CheckBox> e)
		{
			if (e.NewElement != null && !_disposed)
			{
				this.EnsureId();

				UpdateOnColor();
				UpdateIsChecked();
				UpdateBackgroundColor();
			}

			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == CheckBox.ColorProperty.PropertyName)
			{
				UpdateOnColor();
			}
			else if (e.PropertyName == CheckBox.IsCheckedProperty.PropertyName)
			{
				UpdateIsChecked();
			}
			else if (e.PropertyName == CheckBox.BackgroundColorProperty.PropertyName)
			{
				UpdateBackgroundColor();
			}

			ElementPropertyChanged?.Invoke(this, e);
		}

		void IOnCheckedChangeListener.OnCheckedChanged(CompoundButton buttonView, bool isChecked)
		{
			((IElementController)Element).SetValueFromRenderer(CheckBox.IsCheckedProperty, isChecked);
		}

		void UpdateIsChecked()
		{
			if (Element == null || Control == null)
				return;

			Checked = Element.IsChecked;
		}


		protected virtual ColorStateList GetColorStateList()
		{
			var tintColor = Element.Color == Color.Default ? Color.Accent.ToAndroid() : Element.Color.ToAndroid();

			var list = new ColorStateList(
					_checkedStates,
					new int[]
					{
						tintColor,
						tintColor,
						tintColor,
						tintColor
					});

			return list;
		}

		void UpdateBackgroundColor()
		{
			if (Element.BackgroundColor == Color.Default)
				SetBackgroundColor(AColor.Transparent);
			else
				SetBackgroundColor(Element.BackgroundColor.ToAndroid());
		}

		void UpdateOnColor()
		{
			if (Element == null || Control == null)
				return;

			var mode = PorterDuff.Mode.SrcIn;

			CompoundButtonCompat.SetButtonTintList(Control, GetColorStateList());
			CompoundButtonCompat.SetButtonTintMode(Control, mode);
		}

		void IOnFocusChangeListener.OnFocusChange(AView v, bool hasFocus)
		{
			((IElementController)Element).SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, hasFocus);
		}

		IPlatformElementConfiguration<PlatformConfiguration.Android, CheckBox> OnThisPlatform()
		{
			if (_platformElementConfiguration == null)
				_platformElementConfiguration = Element.OnThisPlatform();

			return _platformElementConfiguration;
		}

		public void SetLabelFor(int? id)
		{
			if (_defaultLabelFor == null)
				_defaultLabelFor = LabelFor;

			LabelFor = (int)(id ?? _defaultLabelFor);
		}

		void IVisualElementRenderer.UpdateLayout() => _tracker?.UpdateLayout();
		VisualElement IVisualElementRenderer.Element => Element;
		AView IVisualElementRenderer.View => this;
		ViewGroup IVisualElementRenderer.ViewGroup => null;
		VisualElementTracker IVisualElementRenderer.Tracker => _tracker;

		protected CheckBox Element
		{
			get => _checkBox;
			private set
			{
				_checkBox = value;
				_platformElementConfiguration = null;
			}
		}

		protected AppCompatCheckBox Control => this;
	}
}