using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace hlrsTheme
{
	public class HlrsComboBox : ComboBox
	{
		public static readonly DependencyProperty ValidateValueProperty =
			DependencyProperty.Register("ValidateValue", typeof(bool), typeof(HlrsComboBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty ValidProperty =
			DependencyProperty.Register("Valid", typeof(bool), typeof(HlrsComboBox), new UIPropertyMetadata(OnValidChanged));

		//public static readonly DependencyProperty PlaceholderProperty =
		//	DependencyProperty.Register("Placeholder", typeof(string), typeof(HlrsComboBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty ValidationBrushProperty =
					DependencyProperty.Register("ValidationBrush", typeof(Brush), typeof(HlrsComboBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty ShowValidationTextProperty =
			DependencyProperty.Register("ShowValidationText", typeof(bool), typeof(HlrsComboBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty ValidationTextProperty =
			DependencyProperty.Register("ValidationText", typeof(string), typeof(HlrsComboBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty LabelTextProperty =
				DependencyProperty.Register("LabelText", typeof(string), typeof(HlrsComboBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty ShowLabelProperty =
					DependencyProperty.Register("ShowLabel", typeof(bool), typeof(HlrsComboBox), new UIPropertyMetadata(null));


		private readonly Brush _standardBorderBrush;

		public HlrsComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ComboBox),
				new FrameworkPropertyMetadata(typeof(ComboBox)));
			//DefaultStyleKey = typeof(HlrsComboBox);
			GotFocus += OnGotFocus;
			LostFocus += OnLostFocus;

			_standardBorderBrush = BorderBrush;

		}

		private void OnLostFocus(object sender, RoutedEventArgs e)
		{
			//if (string.IsNullOrEmpty(Text))
			//{
			//	if (GetTemplateChild("Placeholder") is TextBlock placeHolder) placeHolder.Visibility = Visibility.Visible;
			//	//ShowLabel = false;
			//}

			//Validate();
		}

		private void OnGotFocus(object sender, RoutedEventArgs e)
		{
			//if (GetTemplateChild("Placeholder") is TextBlock placeHolder && placeHolder.Visibility == Visibility.Visible)
			//{
			//	ShowLabel = true;
			//	placeHolder.Visibility = Visibility.Collapsed;
			//}
		}


		public bool ValidateValue
		{
			get => (bool)GetValue(ValidateValueProperty);
			set => SetValue(ValidateValueProperty, value);
		}

		public bool Valid
		{
			get => (bool)GetValue(ValidProperty);
			set
			{
				SetValue(ValidProperty, value);

				Validate();
			}
		}

		private static void OnValidChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			HlrsComboBox cp = obj as HlrsComboBox;
			cp.Validate();
		}


		private void Validate()
		{
			if (ValidateValue && Valid == false)
			{
				//_standardBorderBrush = BorderBrush;
				// Show Validation
				BorderBrush = ValidationBrush;
				ShowValidationText = true;
			}

			if (ValidateValue && Valid)
			{
				BorderBrush = _standardBorderBrush;
				ShowValidationText = false;
			}
		}

		//public string Placeholder
		//{
		//	get => (string)GetValue(PlaceholderProperty);
		//	set => SetValue(PlaceholderProperty, value);
		//}

		public Brush ValidationBrush
		{
			get => (Brush)GetValue(ValidationBrushProperty);
			set => SetValue(ValidationBrushProperty, value);
		}

		public bool ShowValidationText
		{
			get => (bool)GetValue(ShowValidationTextProperty);
			set => SetValue(ShowValidationTextProperty, value);
		}

		public string ValidationText
		{
			get => (string)GetValue(ValidationTextProperty);
			set => SetValue(ValidationTextProperty, value);
		}

		public string LabelText
		{
			get => (string)GetValue(LabelTextProperty);
			set => SetValue(LabelTextProperty, value);
		}
		public bool ShowLabel
		{
			get => (bool)GetValue(ShowLabelProperty);
			set => SetValue(ShowLabelProperty, value);
		}


	}
}